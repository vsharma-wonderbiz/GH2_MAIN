import json
import random
import struct
import threading
import time

from flask import Flask, request, jsonify
from pymodbus.server.sync import StartTcpServer
from pymodbus.datastore import ModbusSequentialDataBlock
from pymodbus.datastore import ModbusSlaveContext
from pymodbus.datastore import ModbusServerContext

IP = "10.10.10.122"

app = Flask(__name__)
_signals = []


class Signal:
    def __init__(self, group, config):
        self.group = group
        self.name = config["name"]
        self.registers = config["registers"]
        self.min = config["min"]
        self.max = config["max"]
        self.unit = config["unit"]
        self.value = random.uniform(self.min, self.max)

        self.trigger_active = False
        self.trigger_target = None
        self.trigger_step = (self.max - self.min) * 0.05
        self._lock = threading.Lock()

    def trigger_spike(self, percent=0.8, absolute=None):
     with self._lock:
        if absolute is not None:
            self.trigger_target = absolute      # ✅ Use exact value you specify
        else:
            self.trigger_target = self.max * 1.2  # 20% beyond max by default
        self.trigger_active = True
        self.spike_phase = "rise"
        self.spike_hold_ticks = 5
        print(f"[TRIGGER] {self.group}/{self.name} → target={self.trigger_target:.2f}")

    def update(self):
        with self._lock:
            if self.trigger_active and self.unit != "bool":
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
            "trigger_active": s.trigger_active
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
            sig.spike_hold_ticks = hold_ticks
            sig.trigger_spike(percent=percent, absolute=float(absolute) if absolute else None)
            return jsonify({
                "status": "ok",
                "message": f"Triggered {group}/{signal_name}",
                "normal_range": f"{sig.min} – {sig.max}",
                "spike_target": round(sig.trigger_target, 3)   # ✅ Shows where it will go
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
        if sig.group.upper() == group.upper() and sig.unit != "bool":
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
        stack_id     = s + 1
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
        {"name": "plantdata_power",          "min": 1000, "max": 2000, "unit": "kW"},
        {"name": "plantdata_throughput",     "min": 200,  "max": 450,  "unit": "Nm3/h"},
        {"name": "plantdata_water_flow_tot", "min": 2.0,  "max": 5.0,  "unit": "m3/h"}
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


def simulation_loop(context, signals):
    while True:
        for sig in signals:
            value    = sig.update()
            r1, r2   = float_to_registers(value)
            context[0].setValues(3, sig.registers[0], [r1, r2])
            print(f"[{sig.group}] {sig.name} = {value:.3f}", flush=True)
        time.sleep(1)


def start_flask(signals):
    global _signals
    _signals = signals
    app.run(host="0.0.0.0", port=9000, debug=False, use_reloader=False)


# ─── Main ────────────────────────────────────────────────────────────

if __name__ == "__main__":
    plant          = load_json("Plant_Config.json")
    stack_template = load_json("stack_template.json")

    stack_mapping = allocate_stack_registers(plant, stack_template)
    plant_mapping = allocate_plant_signals(plant)

    merged_config = {"stacks": stack_mapping, "plant": plant_mapping}
    with open("Full_Register_Config.json", "w") as file:
        json.dump(merged_config, file, indent=4)

    all_signals = []
    for stack in stack_mapping:
        for sig in stack["signals"]:
            all_signals.append(Signal(f"STACK_{stack['stack']}", sig))
    for sig in plant_mapping:
        all_signals.append(Signal("PLANT", sig))

    max_register = max(max(sig.registers) for sig in all_signals)
    store   = ModbusSlaveContext(hr=ModbusSequentialDataBlock(0, [0] * (max_register + 10)))
    context = ModbusServerContext(slaves=store, single=True)

    threading.Thread(target=simulation_loop, args=(context, all_signals), daemon=True).start()
    threading.Thread(target=start_flask,     args=(all_signals,),          daemon=True).start()

    print(f"[MODBUS] TCP Server on {IP}:5020")
    print(f"[API]    Flask REST API on http://{IP}:9000")
    StartTcpServer(context, address=(IP, 5020))