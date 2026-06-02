import json
import time
import logging
from opcua import Server
from pymodbus.client.sync import ModbusTcpClient
import struct
import requests
import urllib3
from dotenv import load_dotenv
import os

urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

load_dotenv()

IP = os.getenv("IP")
MODBUS_PORT = int(os.getenv("MODBUS_PORT"))
OPC_URL = os.getenv("OPC_URL")
SERVER_NAME = os.getenv("SERVER_NAME")
CONFIG_API_URL = os.getenv("CONFIG_URL")

# ------------------ LOGGING SETUP ------------------
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s - %(levelname)s - %(message)s",
)

# logging.getLogger("opcua").setLevel(logging.WARNING)
# logging.getLogger("asyncio").setLevel(logging.WARNING)

logger = logging.getLogger("gateway")


def fetch_config(api_url, output_file="fetched_config.json"):
    try:
        response = requests.get(api_url, verify=False)

        if response.status_code == 200:
            print("---------------------------- Fetched config successfully, starting server ----------------------------------------")
            config_data = response.json()

            # Write the response to a file for verification
            try:
                with open(output_file, "w", encoding="utf-8") as f:
                    json.dump(config_data, f, indent=4)
                print(f"Config written to {output_file}")
            except Exception as file_error:
                logger.error(f"Failed to write config to file: {file_error}")

            return config_data
        else:
            logger.error(f"API returned status {response.status_code}")
            return None

    except Exception as e:
        logger.error(f"Failed to fetch config: {e}")
        return None


# ------------------ OPC NODES CLASS ------------------
class Nodes:
    def __init__(self, server, config):
        self.server = server
        self.nodes = {}
        self.last_values = {}
        self.modbus_map = {}   # register_address -> tag_name
        self.load_config(config)

    def load_config(self, config):
        objects = self.server.nodes.objects
        plant_obj = {}
        stack_obj = {}

        for tag in config:
            init_val = False if tag["datatype"] == "bool" else 0.0

            node_str = tag["opc_node_id"].split(";s=")[1]
            parts = node_str.split(".")
            plant_name = parts[0]

            if plant_name not in plant_obj:
                plant_obj[plant_name] = objects.add_object(
                    f"ns=2;s={plant_name}", plant_name
                )

            parent_obj = plant_obj[plant_name]

            if len(parts) > 2 and parts[1].lower().startswith("stack"):
                stack_name = parts[1]
                print(stack_name)

                stack_key = f"{plant_name}.{stack_name}"
                print(stack_key)

                if stack_key not in stack_obj:
                    stack_obj[stack_key] = parent_obj.add_object(
                        f"ns=2;s={plant_name}.{stack_name}", stack_name
                    )

                parent_obj = stack_obj[stack_key]

            var_node = parent_obj.add_variable(
                tag["opc_node_id"],
                tag["display_name"],
                init_val,
            )

            var_node.set_writable()

            self.nodes[tag["display_name"]] = {
                "node": var_node,
                "register": tag["register_address"],
                "datatype": tag["datatype"],
                "deadband": tag["deadband"],
                "unit": tag.get("unit", "")
            }

            self.modbus_map[tag["register_address"]] = tag["tag_name"]
            self.last_values[tag["display_name"]] = None

    def update_value(self, tag_name, value):
        if tag_name not in self.nodes:
            logger.warning(f"Tag {tag_name} not found")
            return

        node_info = self.nodes[tag_name]
        node = node_info["node"]
        deadband = node_info["deadband"]
        datatype = node_info["datatype"]

        try:
            # if datatype == "bool":
            #     value = bool(value)
            # else:
            value = round(float(value), 2)

            # Only update if changed
            past_value = self.last_values[tag_name]

            if past_value is None:
                node.set_value(value)
                self.last_values[tag_name] = value
                return

            if past_value != value and abs(past_value - value) > deadband:
                node.set_value(value)
                self.last_values[tag_name] = value
                logger.info(f"Updated {tag_name} -> {value}")

        except Exception as e:
            logger.error(f"Failed to update {tag_name}: {e}")

    def get_register_blocks(self, max_block_size=125):
        ranges = []

        # Step 1: Collect register ranges
        for data in self.nodes.values():
            start = data["register"]
            length = 2 
            
            # if data["datatype"] in ["float32", "int32"] else 1
            end = start + length - 1
            ranges.append((start, end))

        if not ranges:
            return []

        # Step 2: Sort ranges
        ranges.sort()

        # Step 3: Merge continuous ranges
        merged = []
        current_start, current_end = ranges[0]

        for start, end in ranges[1:]:
            if start <= current_end + 1:
                # Continuous
                current_end = max(current_end, end)
            else:
                # Gap found → close current block
                merged.append((current_start, current_end))
                current_start, current_end = start, end

        merged.append((current_start, current_end))

        # Step 4: Split merged ranges into max 125 size blocks
        blocks = []

        for start, end in merged:
            while start <= end:
                block_end = min(start + max_block_size - 1, end)
                blocks.append((start, block_end))
                start = block_end + 1

        return blocks


# ------------------ MODBUS CLASS ------------------
class ModbusReader:
    def __init__(self, host, port=502):
        self.client = ModbusTcpClient(host, port)

    def connect(self):
        try:
            if self.client.connect():
                logger.info("Connected to Modbus server")
                return True
            else:
                logger.error("Failed to connect to Modbus server")
                return False
        except Exception as e:
            logger.error(f"Modbus connection error: {e}")
            return False

    def read_holding(self, start, count):
        try:
            result = self.client.read_holding_registers(start, count)
            if result.isError():
                logger.error("Modbus read error")
                return None
            return result.registers
        except Exception as e:
            logger.error(f"Modbus read exception: {e}")
            return None


def main():
    config = fetch_config(CONFIG_API_URL)

    if not config:
        logger.error("Failed to load config. Exiting.")
        return

    server = Server()
    server.set_endpoint(OPC_URL)
    server.set_server_name(SERVER_NAME)

    uri = "urn:asrock:test"
    server.register_namespace(uri)

    nodes = Nodes(server, config)

    print(nodes.nodes)
    print(nodes.modbus_map)

    modbus = ModbusReader(IP, MODBUS_PORT)
    if not modbus.connect():
        return

    server.start()
    logger.info("OPC Server started")

    register_blocks = nodes.get_register_blocks()
    logger.info(f"Register blocks: {register_blocks}")

    try:
        while True:
            for start, end in register_blocks:
                count = end - start + 1
    
                registers = modbus.read_holding(start, count)
                if not registers:
                    continue
    
                for tag_name, data in nodes.nodes.items():
                
                    tag_start = data["register"]
                    datatype = data["datatype"]
    
                    # Skip if tag not inside this block
                    if not (start <= tag_start <= end):
                        continue
    
                    offset = tag_start - start
    
                    try:
                        if datatype == "float32":
                            high = registers[offset]
                            low = registers[offset + 1]
    
                            raw = struct.pack(">HH", high, low)
                            value = struct.unpack(">f", raw)[0]
    
                        elif datatype == "int32":
                            high = registers[offset]
                            low = registers[offset + 1]
    
                            raw = struct.pack(">HH", high, low)
                            value = struct.unpack(">i", raw)[0]
    
                        elif datatype == "bool":
                            value = bool(registers[offset])
    
                        else:
                            value = registers[offset]
    
                        nodes.update_value(tag_name, value)
    
                    except Exception as e:
                        logger.error(f"Error processing tag {tag_name}: {e}")
    
            time.sleep(1)
    
    finally:
        server.stop()
        logger.info("OPC Server stopped")


if __name__ == "__main__":
    main()  