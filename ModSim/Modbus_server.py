# import json
# import random
# import struct
# import threading
# import time

# from pymodbus.server.sync  import StartTcpServer
# from pymodbus.datastore import ModbusSequentialDataBlock
# from pymodbus.datastore import ModbusSlaveContext
# from pymodbus.datastore import ModbusServerContext



# class Signal:
#     def __init__(self, stack_id, config):
#         self.stack_id = stack_id
#         self.name = config["name"]
#         self.registers = config["registers"]
#         self.min = config["min"]
#         self.max = config["max"]
#         self.unit = config["unit"]

#         self.value = random.uniform(self.min, self.max)

#     def update(self):
#         if self.unit == "bool":
#             self.value = float(random.choice([0, 1]))
#         else:
#             delta = random.uniform(-0.02, 0.02) * (self.max - self.min)
#             self.value += delta
#             self.value =round( max(self.min, min(self.value, self.max)),2)

#         return self.value



# def load_json(path):
#     try:
#         with open(path, 'r') as f:
#             return json.load(f)
#     except Exception as e:
#         print(f"Error loading {path}: {e}")
#         return None


# def float_to_registers(value):
#     packed = struct.pack(">f", float(value))  # Big endian float32
#     return struct.unpack(">HH", packed)



# def allocate_registers(plant_config, stack_template):
#     registers_per_signal = int(plant_config.get('registers_per_signal', 2))
#     signals_per_stack = int(plant_config.get('signals_per_stack', 22))
#     stack_block_size = int(
#         plant_config.get('stack_block_size',
#                          signals_per_stack * registers_per_signal)
#     )
#     stack_start_address = int(plant_config.get('stack_start_address', 0))
#     active_stacks = int(plant_config.get('active_stacks', 1))

#     template_signals = stack_template.get('signals', [])

#     allocation = []

#     for s in range(active_stacks):
#         stack_number = s + 1
#         base_address = stack_start_address + s * stack_block_size

#         addr = base_address
#         mapped = []

#         for sig in template_signals:
#             regs = list(range(addr, addr + registers_per_signal))

#             mapped.append({
#                 'name': sig.get('name'),
#                 'registers': regs,
#                 'unit': sig.get('unit'),
#                 'min': sig.get('min'),
#                 'max': sig.get('max')
#             })

#             addr += registers_per_signal

#         allocation.append({
#             'stack': stack_number,
#             'base_address': base_address,
#             'signals_allocated': len(mapped),
#             'signals': mapped
#         })

#     return allocation



# def simulation_loop(context, signals):
#     while True:
#         print(signals.count)
#         for sig in signals:
#             value = sig.update()
#             r1, r2 = float_to_registers(value)

#             context[0].setValues(
#                 3,  # Holding register
#                 sig.registers[0],
#                 [r1, r2]
#             )
#             print(f"Stack_id:{sig.stack_id}, Signal_name:{sig.name}, Value:{sig.value}")

#         time.sleep(1)



# if __name__ == '__main__':

#     plant = load_json('Plant_Config.json')
#     stack_template = load_json('stack_template.json')

#     if not plant or not stack_template:
#         print("Configuration files missing.")
#         exit()

#     allocation = allocate_registers(plant, stack_template)

 
#     with open("Stack_register_mapping.json", "w") as file:
#         json.dump(allocation, file, indent=4)

#     print("Register mapping created.")


#     all_signals = []

#     for stack in allocation:
#         stack_id = stack["stack"]
#         for sig in stack["signals"]:
#             all_signals.append(Signal(stack_id, sig))


#     max_register = 0
#     for sig in all_signals:
#         max_register = max(max_register,max(sig.registers))

#     store = ModbusSlaveContext(
#         hr=ModbusSequentialDataBlock(0, [0] * (max_register + 20))
#     )

#     context = ModbusServerContext(slaves=store, single=True)


#     sim_thread = threading.Thread(
#         target=simulation_loop,
#         args=(context, all_signals),
#         daemon=True
#     )
#     sim_thread.start()

#     print("Modbus TCP Server running on 0.0.0.0:5020")

#     StartTcpServer(context, address=("0.0.0.0", 5020))


import json
import random
import struct
import threading
import time

from pymodbus.server.sync import StartTcpServer
from pymodbus.datastore import ModbusSequentialDataBlock
from pymodbus.datastore import ModbusSlaveContext
from pymodbus.datastore import ModbusServerContext

class Signal:
    def __init__(self, group, config):
        self.group = group   # Stack ID or PLANT
        self.name = config["name"]
        self.registers = config["registers"]
        self.min = config["min"]
        self.max = config["max"]
        self.unit = config["unit"]

        self.value = random.uniform(self.min, self.max)

    def update(self):
        if self.unit == "bool":
            self.value = float(random.choice([0, 1]))
        else:
            delta = random.uniform(-0.02, 0.02) * (self.max - self.min)
            self.value += delta
            self.value = round(max(self.min, min(self.value, self.max)), 2)

        return self.value



def load_json(path):
    with open(path, "r") as f:
        return json.load(f)


def float_to_registers(value):
    packed = struct.pack(">f", float(value))
    return struct.unpack(">HH", packed)



def allocate_stack_registers(plant_config, stack_template):
    registers_per_signal = plant_config["registers_per_signal"]
    stack_block_size = plant_config["stack_block_size"]
    stack_start_address = plant_config["stack_start_address"]
    active_stacks = plant_config["active_stacks"]

    allocation = []

    for s in range(active_stacks):
        stack_id = s + 1
        base_address = stack_start_address + s * stack_block_size
        addr = base_address

        mapped_signals = []

        for sig in stack_template["signals"]:
            regs = list(range(addr, addr + registers_per_signal))

            mapped_signals.append({
                "name": sig["name"],
                "registers": regs,
                "min": sig["min"],
                "max": sig["max"],
                "unit": sig["unit"]
            })

            addr += registers_per_signal

        allocation.append({
            "stack": stack_id,
            "signals": mapped_signals
        })

    return allocation


def allocate_plant_signals(plant_config):
    registers_per_signal = plant_config["registers_per_signal"]
    plant_start = plant_config["plant_block_start"]

    # Example plant-level signals (you can move this to JSON later)
    plant_signals = [
        {"name": "plantdata_power", "min": 1000, "max": 2000, "unit": "kW"},
        {"name": "plantdata_throughput", "min": 200, "max": 450, "unit": "Nm3/h"},
        {"name": "plantdata_water_flow_tot", "min": 2.0, "max": 5.0, "unit": "m3/h"}
    ]

    mapped = []
    addr = plant_start

    for sig in plant_signals:
        regs = list(range(addr, addr + registers_per_signal))

        mapped.append({
            "name": sig["name"],
            "registers": regs,
            "min": sig["min"],
            "max": sig["max"],
            "unit": sig["unit"]
        })

        addr += registers_per_signal

    return mapped



def simulation_loop(context, signals):
    while True:
        for sig in signals:
            value = sig.update()
            r1, r2 = float_to_registers(value)

            context[0].setValues(3, sig.registers[0], [r1, r2])

            print(f"[{sig.group}] {sig.name} = {sig.value}")

        time.sleep(1)


if __name__ == "__main__":

    plant = load_json("Plant_Config.json")
    stack_template = load_json("stack_template.json")

    stack_mapping = allocate_stack_registers(plant, stack_template)
    plant_mapping = allocate_plant_signals(plant)
   
    
    merged_config = {
    "stacks": stack_mapping,
    "plant": plant_mapping
     }

    with open("Full_Register_Config.json", "w") as file:
      json.dump(merged_config, file, indent=4)
    
    all_signals = []

    
    for stack in stack_mapping:
        stack_id = stack["stack"]
        for sig in stack["signals"]:
            all_signals.append(Signal(f"STACK_{stack_id}", sig))
            
            
   

    for sig in plant_mapping:
        all_signals.append(Signal("PLANT", sig))

   
    max_register = 0
    for sig in all_signals:
        max_register = max(max_register, max(sig.registers))

    store = ModbusSlaveContext(
        hr=ModbusSequentialDataBlock(0, [0] * (max_register + 10))
    )

    context = ModbusServerContext(slaves=store, single=True)

    sim_thread = threading.Thread(
        target=simulation_loop,
        args=(context, all_signals),
        daemon=True
    )
    sim_thread.start()

    print("Modbus TCP Server running on 10.10.10.19:5020")

    StartTcpServer(context, address=("10.10.10.233", 5020))

