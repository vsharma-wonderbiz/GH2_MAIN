using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interface
{
    public interface IKpiResultRepository
    {
        Task<List<KpiTable>> GetAllKpisValues();
        Task AddAsync(KpiTable result);
        Task AddRangeAsync(List<KpiTable> results);
        Task<bool> IsAlreadyCalculated(string kpiName, string assetName, DateTime startTime, DateTime endTime);
        Task SaveChangesAsync();
    }
}
