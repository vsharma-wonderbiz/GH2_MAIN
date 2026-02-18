from Database.Interface.ITelemetryRepository import ITelemetryRepository
from Database.Connection.SqlLiteConnection import SqlLiteConnection
import threading

class SqlLiteRepositoryService(ITelemetryRepository):
    def __init__(self):
        db_connection = SqlLiteConnection()
        self.lock = threading.Lock()
        self.conn = db_connection.connect()
        self.cursor = self.conn.cursor()

    def add_nodes_to_master(self, nodes):
        with self.lock:
            inserted_count = 0
            for node in nodes:
                self.cursor.execute("""
                    INSERT OR IGNORE INTO node_master (mixer_name, signal_name, opc_node_id)
                    VALUES (?, ?, ?)
                """, (node["mixer_name"], node["signal_name"], node["opc_node_id"]))
                if self.cursor.rowcount > 0:
                    inserted_count += 1
            self.conn.commit()
            return inserted_count


    def get_node_id_mapping(self):
        with self.lock:
            self.cursor.execute("SELECT node_id, opc_node_id FROM node_master")
            return {row[1]: row[0] for row in self.cursor.fetchall()}



    def get_all_last_known_values(self):
        with self.lock:
            self.cursor.execute("SELECT node_id, value FROM node_last_value")
            return {row[0]: row[1] for row in self.cursor.fetchall()}



    def update_last_known_value(self, node_id, value, timestamp):
        with self.lock:
            self.cursor.execute("SELECT id FROM node_last_value WHERE node_id = ?", (node_id,))
            exists = self.cursor.fetchone()
            if exists:
                self.cursor.execute("""
                    UPDATE node_last_value
                    SET value = ?, timestamp = ?
                    WHERE node_id = ?
                """, (value, timestamp, node_id))
            else:
                self.cursor.execute("""
                    INSERT INTO node_last_value (node_id, value, timestamp)
                    VALUES (?, ?, ?)
                """, (node_id, value, timestamp))
            self.conn.commit()



    def insert_telemetry_record(self, node_id, value, timestamp):
        with self.lock:
            self.cursor.execute("""
                INSERT INTO telemetry (node_id, value, timestamp)
                VALUES (?, ?, ?)
            """, (node_id, value, timestamp))
            self.conn.commit()

    def get_mixer_signal_data(self):
        with self.lock:
            self.cursor.execute("""
                SELECT nm.mixer_name, nm.signal_name, lkv.value
                FROM node_master nm
                JOIN node_last_value lkv ON nm.node_id = lkv.node_id
            """)
            mixer_data = {}
            for mixer_name, signal_name, value in self.cursor.fetchall():
                if mixer_name not in mixer_data:
                    mixer_data[mixer_name] = {}
                mixer_data[mixer_name][signal_name] = value
            return mixer_data

    def create_machine_snapshots(self, timestamp):
       
        mixer_data = self.get_mixer_signal_data()  
        
        print(f"DEBUG: Retrieved mixer_data: {mixer_data}") 
        
        if not mixer_data:
            print("WARNING: No mixer data found in node_last_value table!")
            return
        
        
        with self.lock:
            for mixer_name, signals in mixer_data.items():
                print(f"DEBUG: Inserting snapshot for {mixer_name}: {signals}") 
                self.cursor.execute("""
                    INSERT INTO machine_snapshot 
                    (timestamp, machine_id, speed, torque, volume, temp, power, current)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?)
                """, (
                    timestamp,
                    mixer_name,
                    signals.get("speed"),
                    signals.get("torque"),
                    signals.get("volume"),
                    signals.get("temp"),
                    signals.get("power"),
                    signals.get("current")
                ))
            self.conn.commit()
            print(f" Successfully created {len(mixer_data)} snapshots")  