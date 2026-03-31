using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOS;
using Application.Interface;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Implementation
{
    public class KpiResultRepository : IKpiResultRepository
    {
        private readonly ApplicationDbContext _context;

        public KpiResultRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<KpiTable>> GetAllKpisValues()
        {
            return await _context.KpiTable.ToListAsync();
        }

        public async Task AddAsync(KpiTable result)
        {
            await _context.AddAsync(result);
        }

        public async Task AddRangeAsync(List<KpiTable> results)
        {
            await _context.KpiTable.AddRangeAsync(results);
        }

        public async Task<bool> IsAlreadyCalculated(
     string kpiName, string assetName, DateTime startTime, DateTime endTime)
        {
            return await _context.KpiTable
                .AnyAsync(x => x.KpiName == kpiName &&
                               x.AssetName == assetName &&
                               x.StartTime.Date == startTime.Date &&   
                               x.EndTime.Date == endTime.Date);        
        }



        //   public async Task<List<KpiTable>> GetByKpiNameAndDateRange(
        //string kpiName, DateTime startTime, DateTime endTime)
        //   {
        //       // Normalize to midnight so comparison is pure date-based
        //       var startDate = startTime.Date;
        //       var endDate = endTime.Date;

        //       return await _context.KpiTable
        //           .Where(x => x.KpiName == kpiName &&
        //                       x.StartTime >= startDate &&
        //                       x.StartTime < startDate.AddDays(1) &&
        //                       x.EndTime >= endDate &&
        //                       x.EndTime < endDate.AddDays(1))
        //           .ToListAsync();
        //   }

        public async Task<List<KpiTable>> GetByKpiNameAndDateRange(
    string kpiName, DateTime startTime, DateTime endTime)
        {
            return await _context.KpiTable
                .Where(x => x.KpiName == kpiName &&
                            x.StartTime <= endTime &&
                            x.EndTime >= startTime)
                .ToListAsync();
        }


        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        //public async Task<(int Week, List<KpiDto> Data)> getPlantKpiBasedOnWeek(int noofweek,string kpiname)
        //{
        //    var latestWeek = await _context.KpiTable
        //        .Where(k => k.AssetName.ToLower() == stackName.ToLower()
        //                 && k.EndTime <= DateTime.UtcNow)
        //        .OrderByDescending(k => k.EndTime)
        //        .Select(k => (int?)k.WeekNumber)
        //        .Distinct()
        //        .FirstOrDefaultAsync();

        //    if (latestWeek == null)
        //        return (0, new List<KpiDto>());

        //    var data = await _context.KpiTable
        //        .Where(k => k.AssetName.ToLower() == stackName.ToLower()
        //                 && k.WeekNumber == latestWeek)
        //        .Select(k => new KpiDto
        //        {
        //            KpiName = k.KpiName,
        //            KpiValue = k.KpiValue,
        //            Level = k.Level
        //        })
        //        .ToListAsync();

        //    return (latestWeek.Value, data);
        //}

        //    public async Task<bool> IsAlreadyCalculated(
        //string kpiName, string assetName, DateTime startTime, DateTime endTime)
        //    {
        //        return await _context.KpiTable
        //            .AnyAsync(x => x.KpiName == kpiName &&
        //                           x.AssetName == assetName &&
        //                           x.StartTime.Date == startTime.Date &&   // compare DATE only
        //                           x.EndTime.Date == endTime.Date);        // compare DATE only
        //    }

        public async Task<List<KpiTable>> GetLatestWeeksAsync(string kpiName, int noOfWeeks)
        {
                return await _context.KpiTable
        .Where(k => k.KpiName == kpiName)
        .OrderBy(k => k.WeekNumber) 
        .Take(noOfWeeks)              
        .ToListAsync();
        }
    }
}
