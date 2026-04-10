using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOS
{
    public  class modbusConfig
    {
        public List<StackConfig> stacks {  get; set; }
        public List<SignalConfig> plant { get; set; }
    }

    public class StackConfig
    {
        public string stack { get; set; }
        public List<SignalConfig> signals { get; set; }
    }

    public class SignalConfig
    {
        public string name { get; set; }
        public List<int> registers { get; set; }
        public float min {  get; set; }
        public float max { get; set; }
        public string unit { get; set; }
    }

}
