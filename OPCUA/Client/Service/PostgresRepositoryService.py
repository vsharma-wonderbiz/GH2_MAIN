from Interface.ITelemetryRepository import ITelemetryRepository
from Connection.PostgresSqlConnection import PostgresSqlConnection
import threading
import logging

class PostgresRepositoryService(ITelemetryRepository):
    def __init__(self):
        db_connection = PostgresSqlConnection()
        self.lock = threading.Lock()
        self.conn = db_connection.connect()
        self.cursor = self.conn.cursor()

    def add_nodes_to_master(self, nodes):
        """Legacy method - not used with current Domain schema."""
        raise NotImplementedError("Use get_node_id_mapping() to fetch mappings instead")


    def get_node_id_mapping(self):
        """Fetch mapping from OpcNodeId to MappingId."""
        with self.lock:
            try:
                self.cursor.execute("""
                  SELECT "OpcNodeId", "MappingId" FROM "Mappings"
                """)
                mappings = {row[0]: row[1] for row in self.cursor.fetchall()}
                logging.info(f"Fetched {len(mappings)} OPC node mappings")
                return mappings
            except Exception as e:
                logging.error(f"Error fetching node mappings: {e}")
                self.conn.rollback()
                return {}

 
    def get_all_last_known_values(self):
        """Fetch all last known values from NodeLastDatas."""
        with self.lock:
            try:
                self.cursor.execute("""
                    SELECT "MappingId", "Value" FROM "NodeLastDatas"
                """)
                return {row[0]: row[1] for row in self.cursor.fetchall()}
            except Exception as e:
                logging.error(f"Error fetching last known values: {e}")
                self.conn.rollback()
                return {}
        

    def update_last_known_value(self, MappingId, OpcNodeId, value, timestamp):
        """Update or insert last known value in NodeLastDatas."""
        with self.lock:
            try:
                var = parse_node_id(OpcNodeId)
                AssetName = var.get("asset", "")
                TagName = var.get("tag", "")
                
                self.cursor.execute("""
                    INSERT INTO "NodeLastDatas" ("MappingId", "OpcNodeId", "AssetName", "TagName", "Value", "TimeStamp")
                    VALUES (%s, %s, %s, %s, %s, %s)
                    ON CONFLICT ("MappingId","OpcNodeId") DO UPDATE SET
                        "Value" = EXCLUDED."Value",
                        "TimeStamp" = EXCLUDED."TimeStamp"
                """, (MappingId, OpcNodeId, AssetName, TagName, value, timestamp))
                
                self.conn.commit()
            except Exception as e:
                logging.error(f"Error updating last known value for MappingId {MappingId}: {e}")
                self.conn.rollback()


    def insert_transaction_record(self, MappingId, OpcNodeId, value, timestamp):
        """Insert transaction record in TransactionDatas."""
        with self.lock:
            try:
                var = parse_node_id(OpcNodeId)
                AssetName = var.get("asset", "")
                TagName = var.get("tag", "")
                
                self.cursor.execute("""
                    INSERT INTO "TransactionData" ("MappingId", "OpcNodeId", "AssetName", "TagName", "Value", "TimeStamp")
                    VALUES (%s, %s, %s, %s, %s, %s)
                """, (MappingId, OpcNodeId, AssetName, TagName, value, timestamp))
                
                self.conn.commit()
            except Exception as e:
                logging.error(f"Error inserting transaction record for MappingId {MappingId}: {e}")
                self.conn.rollback()


    def get_mixer_signal_data(self):
        """Fetch aggregated signal data from NodeLastDatas."""
        with self.lock:
            try:
                self.cursor.execute("""
                   Select * FROM "NodeLastDatas"
                """)
                rows= self.cursor.fetchall() 
                return rows
            except Exception as e:
                logging.error(f"Error fetching mixer signal data: {e}")
                return {}


    def create_machine_snapshots(self, timestamp):
        """use the data fetched from the nodelast value to insert here """
        mixer_data = self.get_mixer_signal_data()

        print(f"DEBUG: Retrieved mixer_data: {mixer_data}")
      
        if not mixer_data:
            print("WARNING: No mixer data found in node_last_value table!")
            return

        
        snapshot_rows = []
        for row in mixer_data:
            mapping_id = row[1]
            opc_node_id = row[2]
            asset_name = row[3]
            tag_name = row[4]
            value = row[5]
                
            snapshot_rows.append((mapping_id, opc_node_id, asset_name, tag_name, value, timestamp))
            
        with self.lock:
            self.cursor.executemany("""
            INSERT INTO "SensorRawDatas" ("MappingId", "OpcNodeId", "AssetName", "TagName", "Value", "TimeStamp")
            VALUES (%s, %s, %s, %s, %s, %s)
            """, snapshot_rows)
            self.conn.commit()



        #     for mixer_name, signals in mixer_data.items():
        #         print(f"DEBUG: Inserting snapshot for {mixer_name}: {signals}")

        #         self.cursor.execute("""
        #             INSERT INTO machine_snapshot
        #             (timestamp, machine_id, speed, torque, volume, temp, power, current)
        #             VALUES (%s, %s, %s, %s, %s, %s, %s, %s)
        #         """, (
        #             timestamp,
        #             mixer_name,
        #             signals.get("speed"),
        #             signals.get("torque"),
        #             signals.get("volume"),
        #             signals.get("temperature"),
        #             signals.get("power"),
        #             signals.get("current")
        #         ))
        #         print(f"Temp:{signals.get("temperature")}")
        #     self.conn.commit()
        #     print(f" Successfully created {len(mixer_data)} snapshots")


def parse_node_id(node_id: str):
    """
    Parse an OPC UA NodeId string like 'ns=2;s=Plant_1.water_flow_tot'
    or 'ns=2;s=Plant_1.Stack_1.voltage' and return asset name + tag name.
    """
    try:
        
        parts = node_id.split(';')
        if len(parts) < 2:
            raise ValueError("Invalid NodeId format")

        identifier = parts[1]  
        if identifier.startswith('s='):
            identifier = identifier[2:]  

      
        segments = identifier.split('.')

        
        if len(segments) == 2:
            asset = segments[0]        
            tag = segments[1]           
        elif len(segments) == 3:
            asset = segments[1]         
            tag = segments[2]           
        else:
            raise ValueError("Unexpected NodeId format")

       
        # if asset.lower().startswith("plant"):
        #     asset_type = "Plant"
        # elif asset.lower().startswith("stack"):
        #     asset_type = "Stack"
        # else:
        #     asset_type = "Unknown"

        return {
            "asset": asset,
            "tag": tag
        }

    except Exception as e:
        return {"error": str(e)}