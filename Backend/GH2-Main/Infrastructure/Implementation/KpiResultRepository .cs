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
                               x.StartTime.Date == startTime.Date &&   // compare DATE only
                               x.EndTime.Date == endTime.Date);        // compare DATE only
        }
        

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

    //    public async Task<bool> IsAlreadyCalculated(
    //string kpiName, string assetName, DateTime startTime, DateTime endTime)
    //    {
    //        return await _context.KpiTable
    //            .AnyAsync(x => x.KpiName == kpiName &&
    //                           x.AssetName == assetName &&
    //                           x.StartTime.Date == startTime.Date &&   // compare DATE only
    //                           x.EndTime.Date == endTime.Date);        // compare DATE only
    //    }
    }
}
