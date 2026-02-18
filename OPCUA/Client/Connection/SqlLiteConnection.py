import sqlite3


DB_FILE="Mixer_data.db"

class SqlLiteConnection:
    def __init__(self):
        self.db_path=DB_FILE
        self.conn=None
        pass
    
    def connect(self):
        self.conn = sqlite3.connect(self.db_path,check_same_thread=False)
        print("SQLite connected")
        return self.conn
        
    
    def init_table(self):
        self.conn=self.connect()
        cursor=self.conn.cursor()
        
        
        cursor.execute("PRAGMA journal_mode=WAL")
        cursor.execute("PRAGMA busy_timeout=30000")
        cursor.execute("PRAGMA foreign_keys = ON")
    
    
        cursor.execute("""
        CREATE TABLE IF NOT EXISTS node_master (
            node_id INTEGER PRIMARY KEY AUTOINCREMENT,
            mixer_name TEXT NOT NULL,
            signal_name TEXT NOT NULL,
            opc_node_id TEXT NOT NULL UNIQUE,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP
        )
    """)
    
  
        cursor.execute("""
        CREATE TABLE IF NOT EXISTS node_last_value (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            node_id INTEGER NOT NULL UNIQUE,
            value REAL NOT NULL,
            timestamp DATETIME NOT NULL,
            FOREIGN KEY (node_id) REFERENCES node_master(node_id)
        )
    """)

        cursor.execute("""
        CREATE TABLE IF NOT EXISTS telemetry (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            node_id INTEGER NOT NULL,
            value REAL NOT NULL,
            timestamp DATETIME NOT NULL,
            FOREIGN KEY (node_id) REFERENCES node_master(node_id)
        )
    """)
    

        cursor.execute("""
        CREATE TABLE IF NOT EXISTS machine_snapshot (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            timestamp DATETIME NOT NULL,
            machine_id TEXT NOT NULL,
            speed REAL,
            torque REAL,
            volume REAL,
            temp REAL,
            power REAL,
            current REAL
        )
    """)
    
        self.conn.commit()