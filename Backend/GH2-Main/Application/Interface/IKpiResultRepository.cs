using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOS;
using Domain.Entities;

namespace Application.Interface
{
    public interface IKpiResultRepository
    {
        Task<List<KpiTable>> GetAllKpisValues();
        Task AddAsync(KpiTable result);
        Task AddRangeAsync(List<KpiTable> results);
        Task<bool> IsAlreadyCalculated(string kpiName, string assetName, DateTime startTime, DateTime endTime);
        Task<List<KpiTable>> GetByKpiNameAndDateRange(string kpiName, DateTime startTime, DateTime endTime);



        Task<List<KpiTable>> GetLatestWeeksAsync(string kpiName, int noOfWeeks);
        Task<List<KpiTable>> GetCustomizeStackKpi(string Kpiname, int NoOfStacks, int NoOfWeeks);
        Task SaveChangesAsync();


    }
}
