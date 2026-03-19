using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class AlarmInfo
    {
        public int Id { get; private set; }

        public int MappingId { get; private set; }

        public string SignalName { get; private set; }

        public float Value { get; private set; }

        public string AlarmType { get; private set; }   

        public string Status { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime? ResolvedAt { get; private set; }
    }
}
