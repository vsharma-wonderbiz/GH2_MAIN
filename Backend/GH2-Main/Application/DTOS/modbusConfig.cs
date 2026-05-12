using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOS
{
    public  class modbusConfig
    {
        public required List<StackConfig> stacks {  get; set; }
        public required List<SignalConfig> plant { get; set; }
    }

    public class StackConfig
    {
        public required string stack { get; set; }
        public required List<SignalConfig> signals { get; set; }
    }

    public class SignalConfig
    {
        public required string name { get; set; }
        public required List<int> registers { get; set; }
        public float min {  get; set; }
        public float max { get; set; }
        public required string unit { get; set; }
    }

}
