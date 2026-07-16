import atexit
import json
import os
import random
import signal
import struct
import sys
import threading
import time

from flask import Flask, request, jsonify
from pymodbus.server.sync import StartTcpServer
from pymodbus.datastore import ModbusSequentialDataBlock
from pymodbus.datastore import ModbusSlaveContext
from pymodbus.datastore import ModbusServerContext

IP = "172.21.224.1"
STATE_FILE = "Signal_State.json"

app = Flask(__name__)
_signals = []

# Lock guarding writes to Signal_State.json (multiple threads may trigger a save)
_state_save_lock = threading.Lock()

# Names of signals that are "stateful" (i.e. not randomly simulated).
# "lifetime" decays linearly. "h2flow" decays along an efficiency curve
# that is a function of how much lifetime has been consumed - same formula
# used on the .NET backfill side, so historical + live data stay consistent.
STATEFUL_SIGNAL_NAMES = {"lifetime", "h2flow"}

# --- h2flow decay model (must mirror Infrastructure/Services/BackfillSensorDataService.cs) ---
RATED_LIFETIME_HOURS = 40000.0          # matches RATED_LIFETIME_HOURS on the .NET side
H2_EFFICIENCY_START_PERCENT = 80.0      # efficiency at BOL (0 elapsed hours)
H2_EFFICIENCY_END_PERCENT = 60.0        # efficiency at end of rated life
H2_THEORETICAL_NM3_PER_HOUR = 0.000418 * 42 * 28  # same constant as .NET's H2TheoreticalNm3PerHour


def compute_h2flow_value(elapsed_operating_hours):
    """
    Mirrors BackfillSensorDataService.ComputeH2FlowValue() on the .NET side:
    efficiency glides from H2_EFFICIENCY_START_PERCENT down to
    H2_EFFICIENCY_END_PERCENT as elapsed_operating_hours goes from 0 to
    RATED_LIFETIME_HOURS, and h2flow is derived from that efficiency.
    """
    lifetime_fraction = min(elapsed_operating_hours / RATED_LIFETIME_HOURS, 1.0)

    efficiency_percent = H2_EFFICIENCY_START_PERCENT - (
        (H2_EFFICIENCY_START_PERCENT - H2_EFFICIENCY_END_PERCENT) * lifetime_fraction
    )

    h2_actual_nm3_per_hour = (efficiency_percent / 100.0) * H2_THEORETICAL_NM3_PER_HOUR

    return round(h2_actual_nm3_per_hour * 1000.0, 3)


class Signal:
    def __init__(self, group, config, state_entry=None):
        """
        config      : the per-signal dict from Full_Register_Config.json
                      (name, registers, min, max, unit)
        state_entry : reference to this STACK's shared dict inside
                      Signal_State.json, e.g.
                          {"stack": "Stack_1", "remainingLifetime": 39856,
                           "currentH2Flow": 391.485}
                      Both the "lifetime" Signal and the "h2flow" Signal for
                      the same stack are given the SAME dict object here, so
                      h2flow can read remainingLifetime (the shared "clock")
                      to know how many operating hours have elapsed, without
                      needing its own separately persisted counter. Mutating
                      this dict keeps the in-memory state file representation
                      in sync - no extra lookups needed when we persist to disk.
        """
        self.group = group
        self.name = config["name"]
        self.registers = config["registers"]
        self.min = config["min"]
        self.max = config["max"]
        self.unit = config["unit"]

        # --- Stateful signal support -------------------------------------
        self.is_stateful = self.name.lower() in STATEFUL_SIGNAL_NAMES
        self.state_entry = state_entry
        name_lower = self.name.lower()

        if self.is_stateful and self.state_entry is not None:
            if name_lower == "lifetime":
                # Resume from the persisted remaining value instead of randomizing
                self.state_entry.setdefault("remainingLifetime", RATED_LIFETIME_HOURS)
                self.value = self.state_entry["remainingLifetime"]

            elif name_lower == "h2flow":
                # Resume from the persisted currentH2Flow if present, otherwise
                # derive it from remainingLifetime (the shared clock) at BOL.
                elapsed = RATED_LIFETIME_HOURS - self.state_entry.get(
                    "remainingLifetime", RATED_LIFETIME_HOURS
                )
                self.value = self.state_entry.get(
                    "currentH2Flow", compute_h2flow_value(elapsed)
                )
                self.state_entry["currentH2Flow"] = self.value
        else:
            self.value = random.uniform(self.min, self.max)
        # -------------------------------------------------------------------

        self.trigger_active = False
        self.trigger_target = None
        self.trigger_step = (self.max - self.min) * 0.05
        self._lock = threading.Lock()

    def trigger_spike(self, percent=0.8, absolute=None):
        # Stateful signals are never randomized or spiked - they only decay.
        if self.is_stateful:
            print(f"[TRIGGER] Ignored: {self.group}/{self.name} is a stateful signal")
            return

        with self._lock:
            if absolute is not None:
                self.trigger_target = absolute      # Use exact value you specify
            else:
                self.trigger_target = self.max * 1.2  # 20% beyond max by default
            self.trigger_active = True
            self.spike_phase = "rise"
            self.spike_hold_ticks = 5
            print(f"[TRIGGER] {self.group}/{self.name} -> target={self.trigger_target:.2f}")

    def update(self, interval_seconds=1):
        """
        Advance this signal by one simulation tick.

        interval_seconds: how much wall-clock time this tick represents.
                           Passed in explicitly (rather than hardcoded) so the
                           decay formulas stay correct even if the
                           simulation loop's tick rate is changed later.
        """
        with self._lock:
            if self.is_stateful:
                name_lower = self.name.lower()

                if name_lower == "lifetime":
                    # --- Lifetime-style decay: no randomness, no triggers ---
                    remaining = self.state_entry["remainingLifetime"]
                    remaining -= interval_seconds / 3600.0  # hours consumed this tick
                    remaining = max(self.min, remaining)    # never drop below configured min

                    self.state_entry["remainingLifetime"] = remaining
                    self.value = remaining

                elif name_lower == "h2flow":
                    # --- h2flow decay: derives elapsed operating hours from the
                    # SAME stack's remainingLifetime (shared clock), then
                    # re-computes the flow value from the efficiency curve.
                    # This avoids needing a second persisted counter - as long
                    # as the lifetime signal for this stack is ticking, h2flow
                    # stays in sync with it automatically.
                    remaining_lifetime = self.state_entry.get(
                        "remainingLifetime", RATED_LIFETIME_HOURS
                    )
                    elapsed = RATED_LIFETIME_HOURS - remaining_lifetime
                    elapsed = max(0.0, min(elapsed, RATED_LIFETIME_HOURS))

                    new_value = compute_h2flow_value(elapsed)

                    self.state_entry["currentH2Flow"] = new_value
                    self.value = new_value

            elif self.trigger_active and self.unit != "bool":
                if self.value < self.trigger_target:
                    self.value = min(self.value + self.trigger_step, self.trigger_target)
                else:
                    self.trigger_active = False
            else:
                if self.unit == "bool":
                    self.value = float(random.choice([0, 1]))
                else:
                    delta = random.uniform(-0.02, 0.02) * (self.max - self.min)
                    self.value += delta
                    self.value = round(max(self.min, min(self.value, self.max)), 2)

        return self.value


# ─── Flask Endpoints ────────────────────────────────────────────────

@app.route("/signals", methods=["GET"])
def list_signals():
    """List all signals and their current values."""
    return jsonify([
        {
            "group": s.group,
            "name": s.name,
            "value": round(s.value, 3),
            "min": s.min,
            "max": s.max,
            "unit": s.unit,
            "trigger_active": s.trigger_active,
            "stateful": s.is_stateful
        }
        for s in _signals
    ]), 200


@app.route("/trigger", methods=["POST"])
def trigger():
    data        = request.json
    group       = data.get("group", "").strip()
    signal_name = data.get("signal", "").strip()
    percent     = float(data.get("percent", 0.8))
    absolute    = data.get("absolute", None)
    hold_ticks  = int(data.get("hold_ticks", 5))

    if not group or not signal_name:
        return jsonify({"status": "error", "message": "group and signal are required"}), 400

    for sig in _signals:
        if sig.group.upper() == group.upper() and sig.name.lower() == signal_name.lower():
            if sig.is_stateful:
                return jsonify({
                    "status": "error",
                    "message": f"'{signal_name}' is a stateful signal and cannot be triggered"
                }), 400
            sig.spike_hold_ticks = hold_ticks
            sig.trigger_spike(percent=percent, absolute=float(absolute) if absolute else None)
            return jsonify({
                "status": "ok",
                "message": f"Triggered {group}/{signal_name}",
                "normal_range": f"{sig.min} - {sig.max}",
                "spike_target": round(sig.trigger_target, 3)   # Shows where it will go
            }), 200

    return jsonify({"status": "error", "message": f"Signal '{group}/{signal_name}' not found"}), 404


@app.route("/trigger/all", methods=["POST"])
def trigger_all():
    """
    Trigger spikes on ALL signals in a group.
    Body: { "group": "STACK_1", "percent": 0.8 }
    """
    data    = request.json
    group   = data.get("group", "").strip()
    percent = float(data.get("percent", 0.8))

    if not group:
        return jsonify({"status": "error", "message": "group is required"}), 400

    triggered = []
    for sig in _signals:
        if sig.group.upper() == group.upper() and sig.unit != "bool" and not sig.is_stateful:
            sig.trigger_spike(percent=percent)
            triggered.append(sig.name)

    if not triggered:
        return jsonify({"status": "error", "message": f"No signals found for group '{group}'"}), 404

    return jsonify({"status": "ok", "triggered": triggered}), 200


@app.route("/reset", methods=["POST"])
def reset():
    """
    Cancel any active trigger on a signal and resume normal simulation.
    Body: { "group": "STACK_1", "signal": "temperature" }
    """
    data        = request.json
    group       = data.get("group", "").strip()
    signal_name = data.get("signal", "").strip()

    for sig in _signals:
        if sig.group.upper() == group.upper() and sig.name.lower() == signal_name.lower():
            with sig._lock:
                sig.trigger_active = False
                sig.trigger_target = None
            return jsonify({"status": "ok", "message": f"Reset {group}/{signal_name}"}), 200

    return jsonify({"status": "error", "message": "Signal not found"}), 404


@app.route("/lifetime/reset", methods=["POST"])
def reset_lifetime():
    """
    Convenience endpoint: reset a stack's lifetime signal back to its rated value
    (e.g. simulating a stack replacement/overhaul). Also resets that stack's
    h2flow back to BOL efficiency, since a new stack means the hydrogen output
    should recover too.
    Body: { "stack": "Stack_1" }
    """
    stack_name = request.json.get("stack", "").strip()

    lifetime_reset = False
    h2flow_reset = False

    for sig in _signals:
        if not sig.is_stateful or sig.state_entry is None:
            continue
        if sig.state_entry.get("stack") != stack_name:
            continue

        name_lower = sig.name.lower()

        if name_lower == "lifetime":
            with sig._lock:
                sig.state_entry["remainingLifetime"] = RATED_LIFETIME_HOURS
                sig.value = sig.state_entry["remainingLifetime"]
            lifetime_reset = True

        elif name_lower == "h2flow":
            with sig._lock:
                # remainingLifetime is the shared clock - once it's reset above,
                # h2flow just needs to re-derive its value from elapsed=0.
                sig.state_entry["currentH2Flow"] = compute_h2flow_value(0.0)
                sig.value = sig.state_entry["currentH2Flow"]
            h2flow_reset = True

    if lifetime_reset or h2flow_reset:
        # Not writing to disk here either - it'll be captured by the
        # same shutdown-time save as everything else.
        return jsonify({
            "status": "ok",
            "message": f"Reset for {stack_name} (lifetime={lifetime_reset}, h2flow={h2flow_reset}); persisted on shutdown"
        }), 200

    return jsonify({"status": "error", "message": f"No stateful signals found for stack '{stack_name}'"}), 404


# Holds a one-element list containing the live state_data dict so Flask
# handlers (like /lifetime/reset above) can trigger a save without needing
# global rebinding tricks.
_state_data_ref = [None]


# ─── Core Functions ─────────────────────────────────────────────────

def load_json(path):
    with open(path, "r") as f:
        return json.load(f)


def float_to_registers(value):
    packed = struct.pack(">f", float(value))
    return struct.unpack(">HH", packed)


def allocate_stack_registers(plant_config, stack_template):
    registers_per_signal = plant_config["registers_per_signal"]
    stack_block_size     = plant_config["stack_block_size"]
    stack_start_address  = plant_config["stack_start_address"]
    active_stacks        = plant_config["active_stacks"]

    allocation = []
    for s in range(active_stacks):
        stack_id     = f"Stack_{s+1}"
        base_address = stack_start_address + s * stack_block_size
        addr         = base_address
        mapped_signals = []

        for sig in stack_template["signals"]:
            regs = list(range(addr, addr + registers_per_signal))
            mapped_signals.append({
                "name": sig["name"], "registers": regs,
                "min": sig["min"], "max": sig["max"], "unit": sig["unit"]
            })
            addr += registers_per_signal

        allocation.append({"stack": stack_id, "signals": mapped_signals})

    return allocation


def allocate_plant_signals(plant_config):
    registers_per_signal = plant_config["registers_per_signal"]
    plant_start          = plant_config["plant_block_start"]

    plant_signals = [
        {"name": "power",          "min": 1000, "max": 2000, "unit": "kW"},
        {"name": "throughput",     "min": 200,  "max": 450,  "unit": "Nm3/h"},
        {"name": "water_flow_tot", "min": 2.0,  "max": 5.0,  "unit": "m3/h"}
    ]

    mapped = []
    addr   = plant_start
    for sig in plant_signals:
        regs = list(range(addr, addr + registers_per_signal))
        mapped.append({
            "name": sig["name"], "registers": regs,
            "min": sig["min"], "max": sig["max"], "unit": sig["unit"]
        })
        addr += registers_per_signal

    return mapped


# ─── Stateful signal persistence (NEW) ────────────────────────────────

def load_or_seed_state(state_path=STATE_FILE):
    """
    Loads Signal_State.json AS-IS and uses it directly to start the server -
    no merging, no register refresh, no archive/restore logic. This matches
    the simpler per-stack format now in use:

        {"stack": "Stack_1", "remainingLifetime": 39856, "currentH2Flow": 391.485}

    one entry per stack, shared by both the "lifetime" and "h2flow" signals
    of that stack.

    - If the file exists and has entries, those exact values are used to
      resume the simulation (e.g. seeded by the .NET backfill).
    - If the file doesn't exist, or is empty/unreadable, falls back to a
      BOL (beginning-of-life) default for any stack so the server can still
      start rather than crashing.

    Returns the full state_data dict: {"states": [ {...}, ... ], "archived_states": []}
    """
    if os.path.exists(state_path):
        try:
            with open(state_path, "r") as f:
                raw = json.load(f)
            if raw.get("states"):
                raw.setdefault("archived_states", [])
                print(f"[STATE] Loaded {len(raw['states'])} stack state(s) from {state_path}.")
                return raw
        except (json.JSONDecodeError, OSError) as ex:
            print(f"[STATE] Could not read {state_path} ({ex}) - falling back to BOL defaults.")

    print(f"[STATE] No usable {state_path} found - starting all stacks at BOL defaults.")
    return {"states": [], "archived_states": []}


def create_or_update_state(register_config, state_path=STATE_FILE):
    """
    Kept available for later use, but NOT called from main() right now -
    per current requirement, the server should just start directly from
    whatever is already in Signal_State.json (see load_or_seed_state above),
    without rewriting/merging it on every startup.

    If you do want automatic BOL-seeding for any stack that's missing from
    the state file (e.g. a brand new stack added to the plant config), call
    this instead of load_or_seed_state() - it will create a
    {"stack", "remainingLifetime", "currentH2Flow"} entry for any stack that
    doesn't have one yet, preserve existing entries untouched, and archive
    entries for stacks that disappeared from the config.
    """
    existing = load_or_seed_state(state_path)
    existing_states   = existing["states"]
    existing_archived = existing["archived_states"]

    active_map  = {e["stack"]: e for e in existing_states}
    archive_map = {e["stack"]: e for e in existing_archived}

    merged_states = []
    seen_stacks = set()

    for stack in register_config["stacks"]:
        stack_name = stack["stack"]
        signal_names = {s["name"].lower() for s in stack["signals"]}
        if not (STATEFUL_SIGNAL_NAMES & signal_names):
            continue  # this stack has no stateful signals - nothing to persist

        seen_stacks.add(stack_name)

        if stack_name in active_map:
            merged_states.append(active_map[stack_name])

        elif stack_name in archive_map:
            entry = archive_map[stack_name]
            merged_states.append(entry)
            print(f"[STATE] Restored archived stack '{stack_name}' "
                  f"(remainingLifetime={entry.get('remainingLifetime')}, "
                  f"currentH2Flow={entry.get('currentH2Flow')})")

        else:
            merged_states.append({
                "stack": stack_name,
                "remainingLifetime": RATED_LIFETIME_HOURS,
                "currentH2Flow": compute_h2flow_value(0.0)
            })
            print(f"[STATE] New stack '{stack_name}' detected -> seeded at BOL")

    for stack_name, entry in active_map.items():
        if stack_name not in seen_stacks:
            archive_map[stack_name] = entry
            print(f"[STATE] Stack '{stack_name}' no longer in config -> archiving")

    for stack_name in seen_stacks:
        archive_map.pop(stack_name, None)

    state_data = {
        "states": merged_states,
        "archived_states": list(archive_map.values())
    }

    with open(state_path, "w") as f:
        json.dump(state_data, f, indent=4)

    return state_data


def save_state(state_data, state_path=STATE_FILE):
    """
    Atomically persist the current state_data dict to disk.
    Uses a temp-file + os.replace so a crash mid-write can never leave
    Signal_State.json corrupted/truncated.
    """
    with _state_save_lock:
        tmp_path = state_path + ".tmp"
        with open(tmp_path, "w") as f:
            json.dump(state_data, f, indent=4)
        os.replace(tmp_path, state_path)


def build_state_lookup(state_data):
    """Convenience: {stack_name: state_entry_dict} for Signal() construction.
    Both the lifetime and h2flow Signal for a given stack are handed the
    SAME entry dict from this lookup, since one entry now holds both values."""
    return {e["stack"]: e for e in state_data["states"]}


# ─── Simulation loop ───────────────────────────────────────────────────

def simulation_loop(context, signals, interval_seconds=1):
    """
    NOTE on persistence: stateful values (lifetime, h2flow) are updated and
    kept in memory (inside each Signal's state_entry dict) on every tick, but
    Signal_State.json is deliberately NOT written here. Disk writes every
    second are wasteful, especially with many stacks. Instead, the in-memory
    state_data dict is flushed to disk exactly once, on graceful shutdown -
    see _graceful_shutdown() near the bottom of this file. Until then, the
    loop just updates registers.
    """
    while True:
        for sig in signals:
            value  = sig.update(interval_seconds=interval_seconds)
            r1, r2 = float_to_registers(value)
            context[0].setValues(3, sig.registers[0], [r1, r2])
            print(f"[{sig.group}] {sig.name} = {value:.3f}", flush=True)

        time.sleep(interval_seconds)


def start_flask(signals):
    global _signals
    _signals = signals
    app.run(host="0.0.0.0", port=9000, debug=False, use_reloader=False)


# ─── Graceful shutdown persistence (NEW) ──────────────────────────────

def _graceful_shutdown(signum=None, frame=None):
    """
    Called on Ctrl+C (SIGINT), SIGTERM, or normal process exit.
    This is the ONLY place stateful signal state gets written to disk during
    a run - everything else happens in memory to avoid per-tick I/O cost.
    """
    if _state_data_ref[0] is not None:
        print("[SHUTDOWN] Persisting stateful signal state to Signal_State.json ...")
        save_state(_state_data_ref[0], STATE_FILE)
        print("[SHUTDOWN] Done.")
    # If we got here from a signal, exit cleanly (atexit below won't double-save
    # because _state_data_ref[0] save_state is idempotent - it just re-writes
    # the same values that are already in memory).
    if signum is not None:
        sys.exit(0)


# ─── Main ────────────────────────────────────────────────────────────

if __name__ == "__main__":
    plant          = load_json("Plant_Config.json")
    stack_template = load_json("stack_template.json")

    stack_mapping = allocate_stack_registers(plant, stack_template)
    plant_mapping = allocate_plant_signals(plant)

    merged_config = {"stacks": stack_mapping, "plant": plant_mapping}
    with open("Full_Register_Config.json", "w") as file:
        json.dump(merged_config, file, indent=4)

    # --- Load Signal_State.json AS-IS and start directly from it. ---
    # No merge/rewrite here - if you later want auto-seeding for stacks
    # missing from the file, swap this for create_or_update_state(merged_config).
    state_data   = load_or_seed_state(STATE_FILE)
    state_lookup = build_state_lookup(state_data)
    _state_data_ref[0] = state_data  # so the shutdown handler can find it

    # Register the shutdown save: Ctrl+C, `kill`, and normal interpreter exit
    # all end up writing the final stateful values back to Signal_State.json.
    signal.signal(signal.SIGINT, _graceful_shutdown)
    signal.signal(signal.SIGTERM, _graceful_shutdown)
    atexit.register(_graceful_shutdown)

    all_signals = []
    for stack in stack_mapping:
        # Same shared entry (remainingLifetime + currentH2Flow) is handed to
        # BOTH the lifetime and h2flow Signal for this stack.
        stack_state_entry = state_lookup.get(stack["stack"])
        for sig in stack["signals"]:
            state_entry = stack_state_entry if sig["name"].lower() in STATEFUL_SIGNAL_NAMES else None
            all_signals.append(Signal(f"STACK_{stack['stack']}", sig, state_entry=state_entry))
    for sig in plant_mapping:
        # Plant-level signals aren't stateful today, but Signal() will pick up
        # STATEFUL_SIGNAL_NAMES automatically if that ever changes.
        all_signals.append(Signal("PLANT", sig))

    max_register = max(max(sig.registers) for sig in all_signals)
    store   = ModbusSlaveContext(hr=ModbusSequentialDataBlock(0, [0] * (max_register + 10)))
    context = ModbusServerContext(slaves=store, single=True)

    threading.Thread(
        target=simulation_loop,
        args=(context, all_signals),
        kwargs={"interval_seconds": 1},
        daemon=True
    ).start()
    threading.Thread(target=start_flask, args=(all_signals,), daemon=True).start()

    print(f"[MODBUS] TCP Server on {IP}:5020")
    print(f"[API]    Flask REST API on http://{IP}:9000")
    StartTcpServer(context, address=(IP, 5020))