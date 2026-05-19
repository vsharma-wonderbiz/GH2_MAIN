import os
from dotenv import load_dotenv
import psycopg2
from psycopg2.extras import RealDictCursor


load_dotenv()

DB_CONFIG = {
    "host": os.getenv("PG_HOST"),
    "port": int(os.getenv("PG_PORT")),
    "dbname": os.getenv("PG_DB"),
    "user": os.getenv("PG_USER"),
    "password": os.getenv("PG_PASSWORD"),
}


# =========================
# CREATE DATABASE IF NOT EXISTS
# =========================
def create_database_if_not_exists():

    temp_conn = psycopg2.connect(
        host=os.getenv("PG_HOST"),
        port=int(os.getenv("PG_PORT")),
        user=os.getenv("PG_USER"),
        password=os.getenv("PG_PASSWORD"),
        dbname=os.getenv("SYS_DB_NAME")   # existing system DB
    )

    temp_conn.autocommit = True
    cursor = temp_conn.cursor()

    db_name = os.getenv("PG_DB")

    # Check whether DB exists
    cursor.execute(
        "SELECT 1 FROM pg_database WHERE datname = %s",
        (db_name,)
    )

    exists = cursor.fetchone()

    if exists:
        print(f"Database already exists: {db_name}")

    else:
        print(f"Creating database: {db_name}")

        # Create DB
        cursor.execute(f'CREATE DATABASE "{db_name}"')

        print("Database created successfully")

    cursor.close()
    temp_conn.close()


# =========================
# POSTGRES CONNECTION CLASS
# =========================
class PostgresSqlConnection:

    def __init__(self):
        self.conn = None

    # -------------------------
    # Connect to PostgreSQL
    # -------------------------
    def connect(self):

        self.conn = psycopg2.connect(**DB_CONFIG)

        self.conn.autocommit = False

        print("PostgreSQL connected successfully")

        return self.conn

    def init_table(self):

        self.conn = self.connect()

        cursor = self.conn.cursor()


        cursor.execute("""
        CREATE TABLE IF NOT EXISTS node_master (
            node_id SERIAL PRIMARY KEY,
            mixer_name TEXT NOT NULL,
            signal_name TEXT NOT NULL,
            opc_node_id TEXT NOT NULL UNIQUE,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )
        """)

      
        cursor.execute("""
        CREATE TABLE IF NOT EXISTS node_last_value (
            id SERIAL PRIMARY KEY,
            node_id INTEGER NOT NULL UNIQUE,
            value DOUBLE PRECISION NOT NULL,
            timestamp TIMESTAMP NOT NULL,

            CONSTRAINT fk_node
                FOREIGN KEY (node_id)
                REFERENCES node_master(node_id)
                ON DELETE CASCADE
        )
        """)

        cursor.execute("""
        CREATE TABLE IF NOT EXISTS telemetry (
            id SERIAL PRIMARY KEY,
            node_id INTEGER NOT NULL,
            value DOUBLE PRECISION NOT NULL,
            timestamp TIMESTAMP NOT NULL,

            CONSTRAINT fk_node_telemetry
                FOREIGN KEY (node_id)
                REFERENCES node_master(node_id)
                ON DELETE CASCADE
        )
        """)


        cursor.execute("""
        CREATE TABLE IF NOT EXISTS machine_snapshot (
            id SERIAL PRIMARY KEY,
            timestamp TIMESTAMP NOT NULL,
            machine_id TEXT NOT NULL,
            speed DOUBLE PRECISION,
            torque DOUBLE PRECISION,
            volume DOUBLE PRECISION,
            temp DOUBLE PRECISION,
            power DOUBLE PRECISION,
            current DOUBLE PRECISION
        )
        """)

        self.conn.commit()

        print("Tables initialized successfully")

        cursor.close()

   
    def close(self):

        if self.conn:
            self.conn.close()
            print("PostgreSQL connection closed")