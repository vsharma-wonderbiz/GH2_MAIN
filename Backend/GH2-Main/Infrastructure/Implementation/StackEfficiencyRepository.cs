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
    public class StackEfficiencyRepository : IStackEfficiencyRepository
    {
        private readonly ApplicationDbContext _context;

        public StackEfficiencyRepository(ApplicationDbContext context)
        {
            _context = context;
        }

       public async Task<List<StackEfficiencyRecord>> GetDownSampledRecords(string assetman,int NumberOfRecords)
        {
            int noOfRows = await _context.StackEfficiencyRecords
                      .Where(a => a.AssetName == assetman)
                      .CountAsync();

            if(noOfRows == 0)
                return new List<StackEfficiencyRecord>();

            if (noOfRows <= NumberOfRecords)
            {
                return await _context.StackEfficiencyRecords
                    .Where(r => r.AssetName == assetman)
                    .OrderBy(r => r.OperationalHours)
                    .ToListAsync();
            }

            int nthRow = noOfRows / NumberOfRecords;

            var sql = @"
        WITH numbered AS (
            SELECT *, 
                   ROW_NUMBER() OVER (ORDER BY ""OperationalHours"") AS rn,
                   COUNT(*) OVER () AS total_rows
            FROM ""StackEfficiencyRecords""
            WHERE ""AssetName"" = {0}
        )
        SELECT * FROM numbered 
        WHERE rn % {1} = 1 
           OR rn = total_rows
        ORDER BY ""OperationalHours"";";

            return await _context.StackEfficiencyRecords
                .FromSqlRaw(sql, assetman, nthRow)
                .ToListAsync();
        }

        public async Task<StackEfficiencyRecord> GetLatestEfficiency(string stackName)
        {
            var record = await _context.StackEfficiencyRecords
                       .Where(a => a.AssetName == stackName)
                       .OrderByDescending(a => a.TimeStamp)
                       .FirstOrDefaultAsync();

            return record;
        }
    }
}
