from datetime import datetime, timezone

from opcua import Client
import logging
from Connection.PostgresSqlConnection import PostgresSqlConnection
from Interface.ITelemetryRepository import ITelemetryRepository
from Service.PostgresRepositoryService import PostgresRepositoryService
import threading
import time
import pika
import json 

URL = "opc.tcp://10.10.10.193:4840"
RABBITMQ_HOST='localhost'
CHANGE_THRESHOLD = 0.01  # change threshold


logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s - %(levelname)s - %(message)s",
)
logging.getLogger("opcua").setLevel(logging.WARNING)
logging.getLogger("asyncio").setLevel(logging.WARNING)
            
class SubscriptionHandler:
    """
    Handles OPC UA data change notifications and DB updates.
    
    Maps OPC NodeIds to MappingIds and updates DB on value changes.
    """

    def __init__(self, repo: ITelemetryRepository, mapping_id_map: dict, threshold: float,alaram_manager):
        self.repo = repo
        self.mapping_id_map = mapping_id_map  # Maps OpcNodeId string -> MappingId (integer)
        self.threshold = threshold
        self.last_known_values = repo.get_all_last_known_values() if repo else {}
        self.lock = threading.Lock()
        self.alarm_manager=alaram_manager

    def datachange_notification(self, node, val, data):
        """Called when a monitored node value changes."""
        try:
            with self.lock:
                opc_node_id = node.nodeid.to_string()
                signal_name=self._extract_signal_name(opc_node_id)
                asset_name=self._extract_asset_name(opc_node_id)
                # print(signal_name)
                mapping_id = self.mapping_id_map.get(opc_node_id)
                
                if not mapping_id:
                    logging.debug(f"OPC Node {opc_node_id} not in mapping, skipping")
                    return

                # Extract source timestamp
                try:
                    source_time = data.monitored_item.Value.SourceTimestamp.strftime("%Y-%m-%d %H:%M:%S")
                except Exception:
                    from datetime import datetime
                    source_time = datetime.now().strftime("%Y-%m-%d %H:%M:%S")

                last_value = self.last_known_values.get(mapping_id)

                # Check if value changed by threshold
                if self._values_are_different(last_value, val):
                    self.repo.insert_transaction_record(mapping_id, opc_node_id, val, source_time)
                    self.repo.update_last_known_value(mapping_id, opc_node_id, val, source_time)
                    self.last_known_values[mapping_id] = val
                    logging.info(f"Updated MappingId {mapping_id} (OPC: {opc_node_id}): {val}")
                    
                    self.alarm_manager.check_alarm(asset_name,signal_name,mapping_id,val)

        except Exception as e:
            logging.error(f"Subscription handler error: {e}")

    def _values_are_different(self, old_value, new_value) -> bool:
        """Check if values differ beyond threshold."""
        if old_value is None:
            return True

        try:
            old_float = float(old_value)
            new_float = float(new_value)
            return abs(old_float - new_float) > self.threshold

        except (ValueError, TypeError):
            return old_value != new_value
        
    def _extract_signal_name(self,opc_id: str):
      try:
        # Step 1: remove prefix "ns=2;s="
        clean = opc_id.split('=')[-1]

        # Step 2: take last part after "."
        return clean.split('.')[-1].lower()

      except Exception:
        return None
    
    def _extract_asset_name(self,opc_id:str):
        try:
            clean=opc_id.split('=')[-1]
            
            parts=clean.split('.')
            
            if len(parts)>2:
                return parts[1]
            else:
                return parts[0]
        
        except Exception:
            return None
            
            


class AlarmManager:
    def __init__(self, repo, rabbit_mq):
        self.repo = repo
        self.signal_limits = {}
        # State: None = no alarm, "min" = min breached, "max" = max breached
        self.active_alarms = {}
        self.mq = rabbit_mq

    def get_all_signal_limits(self):
        limits = self.repo.get_signal_limits()
        self.signal_limits = {
            signal.strip().lower(): {"min": min_val, "max": max_val}
            for signal, min_val, max_val in limits
        }
        logging.info(f"Signal limits loaded: {self.signal_limits}")

    def check_alarm(self,asset_name, signal_name, mapping_id, current_value):
        if not signal_name:
            logging.debug(f"Skipping alarm check: signal_name is None for mapping_id {mapping_id}")
            return

        signal_key = signal_name.strip().lower()
        limits = self.signal_limits.get(signal_key)
        if not limits:
            logging.debug(f"No limits configured for signal {signal_name}")
            return

        try:
            val = float(current_value)
            min_val = float(limits["min"])
            max_val = float(limits["max"])
        except (ValueError, TypeError) as e:
            logging.warning(f"Alarm check failed for {signal_name}: {e}")
            return

        current_state = self.active_alarms.get(mapping_id)  # None, "min", or "max"

        if val < min_val:
            if current_state != "min":  # Only fire on first state change
                self.active_alarms[mapping_id] = "min"
                self._publish_alarm("ALARM_TRIGGERED",asset_name, signal_name, mapping_id, "min", val, min_val)
                logging.warning(f"ALARM [MIN] {signal_name}: value={val}, limit={min_val}")

        elif val > max_val:
            if current_state != "max":  # Only fire on first state change
                self.active_alarms[mapping_id] = "max"
                self._publish_alarm("ALARM_TRIGGERED",asset_name,signal_name, mapping_id, "max", val, max_val)
                logging.warning(f"ALARM [MAX] {signal_name}: value={val}, limit={max_val}")

        else:
            if current_state is not None:  # Only clear if there WAS an active alarm
                prev_state = current_state
                self.active_alarms.pop(mapping_id, None)
                self._publish_clear(asset_name,signal_name, mapping_id, prev_state, val,max_val)
                logging.info(f"ALARM CLEARED {signal_name}: value={val}")

    def _publish_alarm(self, event,asset_name, signal_name, mapping_id, alarm_type, current_value, limit_value):
        message = json.dumps({
            "event": event,
            "mapping_id": mapping_id,
            "asset":asset_name,
            "signal": signal_name,
            "alarm_type": alarm_type,
            "current_value": current_value,
            "limit_breached": limit_value,
            "timestamp": datetime.now(timezone.utc).isoformat()
        })
        self.mq.publish("alarm_queue", message)

    def _publish_clear(self,asset_name, signal_name, mapping_id, previous_alarm_type, current_value,limit_value):
        message = json.dumps({
            "event": "ALARM_CLEARED",
            "mapping_id": mapping_id,
            "asset":asset_name,
            "signal": signal_name,
            "alarm_type": previous_alarm_type,
            "current_value": current_value,
            "limit_breached": limit_value,
            "timestamp": datetime.now(timezone.utc).isoformat()
        })
        self.mq.publish("alarm_queue", message)
        

class RabbitMQPublisher:
    def __init__(self, host=RABBITMQ_HOST):
        self.host = host
        self.connection = None
        self.channel = None
        self._connect()

    def _connect(self):
        """Establish connection and declare queue."""
        try:
            self.connection = pika.BlockingConnection(pika.ConnectionParameters(host=self.host,heartbeat=300))
            self.channel = self.connection.channel()
            self.channel.queue_declare(queue="alarm_queue", durable=True)
            logging.info(f"RabbitMQ connected at {self.host}")
        except Exception as e:
            logging.error(f"RabbitMQ connection failed: {e}")
            raise

    def _ensure_connection(self):
        """Reconnect if connection is lost."""
        try:
            if self.connection is None or self.connection.is_closed:
                logging.warning("RabbitMQ connection lost, reconnecting...")
                self._connect()
        except Exception as e:
            logging.error(f"RabbitMQ reconnect failed: {e}")
            raise

    def publish(self, queue_name, message):
        """Publish a message to the given queue."""
        try:
            self._ensure_connection()
            self.channel.basic_publish(
                exchange='',
                routing_key=queue_name,
                body=message,
                properties=pika.BasicProperties(delivery_mode=2)  # persistent message
            )
            logging.info(f"Message published to '{queue_name}': {message}")

        except Exception as e:
            logging.error(f"Failed to publish message to '{queue_name}': {e}")
            raise

    def close(self):
        """Gracefully close the RabbitMQ connection."""
        try:
            if self.connection and self.connection.is_open:
                self.connection.close()
                logging.info("RabbitMQ connection closed")
        except Exception as e:
            logging.warning(f"Error closing RabbitMQ connection: {e}")


class SnapshotThread(threading.Thread):

    def __init__(self, repo: ITelemetryRepository, interval_seconds=5):
        super().__init__(daemon=True)
        self.repo = repo
        self.interval = interval_seconds
        self.running = True
        self.lock = threading.Lock()

    def run(self):

        logging.info(f"Snapshot thread started (interval: {self.interval}s)")

        while self.running:
            try:
                timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
                self.repo.create_machine_snapshots(timestamp)
                logging.info(f"Snapshot created at {timestamp}")

            except Exception as e:
                logging.error(f"Snapshot creation failed: {e}")

            time.sleep(self.interval)

    def stop(self):
        self.running = False
        logging.info("Snapshot thread stopping...")


class NodeSubscriber:
    """
    Manages OPC UA connection, database connection, node discovery,
    and subscription to node changes.
    """

    def __init__(self, url: str = URL, db=None, repo=None, threshold: float = CHANGE_THRESHOLD):
        """
        Initialize the subscriber.
        
        Args:
            url: OPC UA server URL
            db: Database connection object (defaults to PostgresSqlConnection)                  
            repo: Repository object (defaults to PostgresRepositoryService)
            threshold: Change threshold for notifications
        """
        self.url = url
        self.client = Client(self.url)
        self.db = db or PostgresSqlConnection()
        self.repo = repo or PostgresRepositoryService()
        self.conn = None
        self.threshold = threshold
        self.handler = None
        self.subscription = None
        self.monitored_nodes = {}
        self.mapping_id_map = {}
        self.alarm_manager=AlarmManager(self.repo,RabbitMQPublisher())

    
    
    def connect_opc(self) -> bool:
        """Connect to OPC UA server."""
        try:
            self.client.connect()
            logging.info(f"Connected to OPC UA server at {self.url}")
            return True
        except Exception as e:
            logging.error(f"Unable to connect to OPC server: {e}")
            return False

    
    def disconnect_opc(self) -> None:
        """Disconnect from OPC UA server."""
        try:
            if self.subscription:
                self.client.delete_subscription(self.subscription)
            self.client.disconnect()
            logging.info("Disconnected from OPC UA server")
        except Exception as e:
            logging.warning(f"Error disconnecting OPC client: {e}")

    
    def connect_db(self):
        """Connect to database."""
        if not self.db:
            logging.warning("No DB backend configured")
            return None

        try:
            self.conn = self.db.connect()
            logging.info("DB connection ready")
            return self.conn
        except Exception as e:
            logging.error(f"Unable to connect to DB: {e}")
            return None

    
    def discover_nodes(self) -> list:
        """Discover available OPC UA nodes in namespace 2."""
        discovered_nodes = []
        try:
            root = self.client.get_objects_node()
            self._browse_recursive(root, discovered_nodes)
            logging.info(f"Discovered {len(discovered_nodes)} total nodes")
        except Exception as e:
            logging.error(f"Node discovery failed: {e}")
        return discovered_nodes

    
    def _browse_recursive(self, node, discovered_nodes: list) -> None:
        """Recursively browse OPC UA node tree."""
        try:
            children = node.get_children()

            if not children:
                node_name = node.get_browse_name().Name
                node_id = node.nodeid.to_string()

                if node.nodeid.NamespaceIndex == 2:
                    discovered_nodes.append({
                        "node_name": node_name,
                        "opc_node_id": node_id,
                        "opc_node_obj": node,
                    })
                return

            for child in children:
                self._browse_recursive(child, discovered_nodes)

        except Exception as e:
            logging.warning(f"Failed to browse node {node}: {e}")

    
    def get_mapping_id_map(self) -> dict:
        """Fetch OPC NodeId to MappingId mapping from repository."""
        try:
            self.mapping_id_map = self.repo.get_node_id_mapping()
            logging.info(f"Loaded {len(self.mapping_id_map)} node mappings")
            return self.mapping_id_map
        except Exception as e:
            logging.error(f"Failed to get mapping ID map: {e}")
            return {}

    
    def subscribe_to_nodes(self, nodes: list) -> bool:
        """
        Create subscription and monitor list of nodes.
        
        Args:
            nodes: List of discovered node dicts with 'opc_node_id' and 'opc_node_obj'
            
        Returns:
            True if subscription created successfully
        """
        if not nodes:
            logging.warning("No nodes to subscribe to")
            return False

        try:
            # Create subscription
            self.subscription = self.client.create_subscription(500, self.handler or self._create_handler())
            logging.info("Created OPC UA subscription")

            # Add nodes to subscription
            for node_dict in nodes:
                try:
                    node_obj = node_dict.get("opc_node_obj")
                    opc_node_id = node_dict.get("opc_node_id")
                    
                    if not node_obj:
                        logging.warning(f"No node object for {opc_node_id}")
                        continue

                    # Monitor the node
                    handle = self.subscription.subscribe_data_change(node_obj)
                    self.monitored_nodes[opc_node_id] = handle
                    logging.debug(f"Subscribed to {opc_node_id}")

                except Exception as e:
                    logging.warning(f"Failed to subscribe to node {node_dict}: {e}")

            logging.info(f"Subscribed to {len(self.monitored_nodes)} nodes")
            return True

        except Exception as e:
            logging.error(f"Failed to create subscription: {e}")
            return False

    
    def _create_handler(self) -> SubscriptionHandler:
        """Create subscription handler with current config."""
        self.handler = SubscriptionHandler(
            self.repo,
            self.mapping_id_map,
            self.threshold,
            self.alarm_manager
        )
        return self.handler

    
    def keep_alive(self) -> None:
        """Keep subscription alive (blocks indefinitely)."""
        try:
            logging.info("Subscription active, listening for data changes...")
            while True:
                import time
                time.sleep(1)
        except KeyboardInterrupt:
            logging.info("Interrupted by user")
        except Exception as e:
            logging.error(f"Error during subscription: {e}")
            
            
    def _start_snapsot(self):
        try:
          self.snap_shot=SnapshotThread(self.repo)
          self.snap_shot.start()
          
        except Exception as e:
            logging.warning(f"Something went wrong while updating the snaphsot service {e}")
            
    
    def _stop_snapshot(self):
        try:
            self.snap_shot=SnapshotThread(self.repo)
            self.snap_shot.stop()
        except Exception as e:
            logging.warning(f"Something went wrong while updating the snaphsot service {e}")
        

def main():
    """Main entry point: connect, discover nodes, setup subscriptions, and listen."""
    logging.info("=" * 80)
    logging.info("OPC UA Client - Subscription Monitor")
    logging.info("=" * 80)
    logging.info(f"\nConnecting to OPC UA server at {URL}...")

    subscriber = NodeSubscriber()

    try:
        # 1. Connect to OPC UA
        if not subscriber.connect_opc():
            logging.error("Failed to connect to OPC UA server")
            return

        # 2. Connect to Database
        if not subscriber.connect_db():
            logging.error("Failed to connect to database")
            return

        # 3. Discover available nodes
        nodes = subscriber.discover_nodes()
        if not nodes:
            logging.warning("No nodes discovered")
            return

        # 4. Load OPC NodeId to MappingId mapping from DB
        mapping_id_map = subscriber.get_mapping_id_map()
        if not mapping_id_map:
            logging.warning("No node mappings found in database")
            # Continue anyway

        # 5. Setup subscription and monitoring
        if not subscriber.subscribe_to_nodes(nodes):
            logging.error("Failed to subscribe to nodes")
            return

        logging.info("Successfully subscribed to nodes")

        # Start snapshot thread (non-blocking)
        subscriber._start_snapsot()
        logging.info("Snapshot service started")

        # Call alarm logic (NOW IT WILL RUN)
        subscriber.alarm_manager.get_all_signal_limits()

        #  Keep alive should be LAST (blocking loop)
        subscriber.keep_alive()

    except Exception as e:
        logging.error(f"Unexpected error: {e}")

    finally:
        try:
            subscriber.disconnect_opc()
            logging.info("Cleanup completed")
        except Exception as e:
            logging.warning(f"Error during cleanup: {e}")

if __name__ == "__main__":
    main()