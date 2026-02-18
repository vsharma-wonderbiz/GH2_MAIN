from abc import ABC, abstractmethod
from datetime import datetime

class ITelemetryRepository(ABC):

    @abstractmethod
    def add_nodes_to_master(self, nodes):
        pass

    @abstractmethod
    def get_node_id_mapping(self):
        """Returns dict {opc_node_id: node_id}"""
        pass

    @abstractmethod
    def get_all_last_known_values(self):
        """Returns dict {node_id: value}"""
        pass

    @abstractmethod
    def update_last_known_value(self, node_id, value, timestamp: datetime):
        pass

    @abstractmethod
    def insert_telemetry_record(self, node_id, value, timestamp: datetime):
        pass

    @abstractmethod
    def get_mixer_signal_data(self):
        """Returns dict {mixer_name: {signal_name: value}}"""
        pass

    @abstractmethod
    def create_machine_snapshots(self, timestamp: datetime):
        pass