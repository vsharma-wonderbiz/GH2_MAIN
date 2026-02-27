using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOS;
using Application.Interface;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Implementation
{
    public class AnalyticsRepository : IAnalyticsRepository
    {
        private readonly ApplicationDbContext _context;

        public AnalyticsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AnalyticsResponseDto> GetAggregatedSensorData(
        string assetname,
        string tagname,
        DateTime startTime,
        DateTime endTime,
        int bucketMinutes
    )
        {
            IQueryable<SensorRawData> query = _context.SensorRawDatas
                .Where(x => x.AssetName == assetname &&
                            x.TagName == tagname &&
                            x.TimeStamp >= startTime &&
                            x.TimeStamp < endTime);

            List<ValueDto> values;

            if (bucketMinutes <= 0)
            {
                // No bucket, raw data
                values = await query
                    .OrderBy(x => x.TimeStamp)
                    .Select(x => new ValueDto
                    {
                        TimeStamp = x.TimeStamp,
                        Value = (float)x.Value
                    })
                    .ToListAsync();
            }
            else
            {
                values = await query
                    .GroupBy(x => new DateTime(
                        x.TimeStamp.Year,
                        x.TimeStamp.Month,
                        x.TimeStamp.Day,
                        x.TimeStamp.Hour,
                        (x.TimeStamp.Minute / bucketMinutes) * bucketMinutes,
                        0))
                    .Select(g => new ValueDto
                    {
                        TimeStamp = g.Key,
                        Value = (float)g.Average(x => x.Value)
                    })
                    .OrderBy(x => x.TimeStamp)
                    .ToListAsync();
            }

            return new AnalyticsResponseDto
            {
                AsseName = assetname,
                TagName = tagname,
                Values = values,
                count= values.Count()
            };
        }


        public async Task<WeeklyAggregatedData?> GetByAssetMappingAndWeekAsync(
        int assetId,
        int mappingId,
        DateTime weekStart)
        {
            return await _context.WeeklyAvgData
                .FirstOrDefaultAsync(x =>
                    x.AssetId == assetId &&
                    x.MappingId == mappingId &&
                    x.WeekStartDate == weekStart.Date);
        }

        public async Task AddAsync(WeeklyAggregatedData entity)
        {
            await _context.WeeklyAvgData.AddAsync(entity);
        }

        public Task UpdateAsync(WeeklyAggregatedData entity)
        {
            _context.WeeklyAvgData.Update(entity);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<WeeklyAvgResposeFromDb> GetWeeklyAggregateAsync(
     int mappingId,
     DateTime weekStart,
     DateTime weekEnd)
        {
            var aggregate = await _context.SensorRawDatas
                .Where(x => x.MappingId == mappingId &&
                            x.TimeStamp.Date >= weekStart.Date &&
                            x.TimeStamp.Date <= weekEnd.Date)
                .GroupBy(x => x.MappingId)
                .Select(g => new
                {
                    MappingId = g.Key,
                    Avg = g.Average(x => x.Value),
                    Min = g.Min(x => x.Value),
                    Max = g.Max(x => x.Value),
                    Count = g.Count()
                })
                .FirstOrDefaultAsync();

            if (aggregate == null)
            {
                return new WeeklyAvgResposeFromDb
                {
                    Average = 0f,
                    Min = 0f,
                    Max = 0f,
                    Count = 0
                };
            }

            return new WeeklyAvgResposeFromDb
            {
                Average = aggregate.Avg,
                Min = aggregate.Min,
                Max = aggregate.Max,
                Count = aggregate.Count
            };
        }


       


        public async Task<bool> IsWeekAvgDataPresent(int mappingId, DateTime weekStart, DateTime weekEnd)
        {
            return await _context.WeeklyAvgData
                .AnyAsync(x => x.MappingId == mappingId
                            && x.WeekStartDate.Date == weekStart.Date
                            && x.WeekEndDate.Date == weekEnd.Date);
        }

    }
}
