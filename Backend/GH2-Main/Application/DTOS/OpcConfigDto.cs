using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOS
{
    public class OpcConfigDto
    {
        public required string asset_name { get; set; }

        public required string tag_name { get; set; }

        public required string opc_node_id { get; set; }

        public required int slave_id { get; set; }

        public required int register_address { get; set; }

        public required string datatype { get; set; }

        public required int register_count { get; set; }

        public required int function_code { get; set; }

        public required string unit { get; set; }

        public required string display_name { get; set; }

        public double deadband { get; set; }
    }
}
