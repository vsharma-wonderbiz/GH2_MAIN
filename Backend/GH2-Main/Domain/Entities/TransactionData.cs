using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class TransactionData
    {
        public int Id { get; set; }
        public int MappingId { get; set; }  
        public string OpcNodeId {  get; set; }  

        public string AssetName { get; set; }

        public string TagName { get; set; }

        public float Value { get; set; }    

        public DateTime TimeStamp { get; set; }

        public MappingTable Mapping { get; set; }
    } 
}
