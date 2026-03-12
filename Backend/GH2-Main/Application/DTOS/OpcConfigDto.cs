using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOS
{
    public class OpcConfigDto
    {
        public string asset_name { get; set; }

        public string tag_name { get; set; }

        public string opc_node_id { get; set; }

        public int slave_id { get; set; }

        public int register_address { get; set; }

        public string datatype { get; set; }

        public int register_count { get; set; }

        public int function_code { get; set; }

        public string unit { get; set; }

        public string display_name { get; set; }

        public double deadband { get; set; }
    }
}
