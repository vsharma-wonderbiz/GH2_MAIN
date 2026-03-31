using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interface;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Implementation
{
    public class AlarmRepository : Repository<AlarmInfo>, IAlarmRepositary
    {
        public AlarmRepository(ApplicationDbContext context)
            : base(context)
        {
        }

        public async Task<AlarmInfo> GetActiveAlarm(int mappingId, string name)
        {
            return await _context.Alarms.FirstOrDefaultAsync(a => a.MappingId == mappingId && a.SignalName == name && a.Status == "Active");
        }
    }
}
