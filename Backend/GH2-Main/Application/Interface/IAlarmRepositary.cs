using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interface
{
    public interface IAlarmRepositary : IRepository<AlarmInfo>
    {
        Task<AlarmInfo> GetActiveAlarm(int mappingId,string name);
    }
}


