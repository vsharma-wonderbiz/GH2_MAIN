using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOS;
using Domain.Entities;

namespace Application.Interface
{
    public interface IAnalyticsRepository
    {
        Task<AnalyticsResponseDto> GetAggregatedSensorData(string assetname,string tagname, DateTime startTime,
    DateTime endTime, int time_bucket);

        Task<WeeklyAggregatedData?> GetByAssetMappingAndWeekAsync(
        int assetId,
        int mappingId,
        DateTime weekStart);


        Task<WeeklyAvgResposeFromDb> GetWeeklyAggregateAsync(
        int mappingId,
        DateTime weekStart,
        DateTime weekEnd);

        Task AddAsync(WeeklyAggregatedData entity);

        Task UpdateAsync(WeeklyAggregatedData entity);

        Task<bool> IsWeekAvgDataPresent(int mappingId, DateTime StartDate, DateTime EndDate);
        Task SaveChangesAsync();

    }
}
    