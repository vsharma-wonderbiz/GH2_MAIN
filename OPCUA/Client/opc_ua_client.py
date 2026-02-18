from opcua import Client
import logging

URL="opc.tcp://10.10.10.178:4840"


logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s - %(levelname)s - %(message)s",
)

class NodeSubcriber:
    def __init__(self,host,port):
        self.client=Client(URL)

    def connect_to_opc_server(self)
      try:
          connect=self.client.connect()
          return connect
      except Exception as e:
          logging.error(f"Unable to connect to opc server :{e}")
          
    
    