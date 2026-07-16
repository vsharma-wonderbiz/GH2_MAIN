import json
import os

STATE_FILE = "Signal_State.json"

with open("Full_Register_Config.json", "r") as f:
    RegisterMapping = json.load(f)

# Create state file only if it doesn't already exist
if not os.path.exists(STATE_FILE):

    state = {"states": []}

    for stack in RegisterMapping["stacks"]:
        for signal in stack["signals"]:

            if signal["name"] == "lifetime":

                state["states"].append({
                    "stack": stack["stack"],
                    "signal": signal["name"],
                    "registers": signal["registers"],
                    "remainingLifetime": signal["max"],   # Initial lifetime
                    "ratedLifetime": signal["max"]
                })

    with open(STATE_FILE, "w") as f:
        json.dump(state, f, indent=4)

    print("State file created successfully!")

else:
    print("State file already exists.")