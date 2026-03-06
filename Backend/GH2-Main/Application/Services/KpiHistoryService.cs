using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOS;
using Application.Interface;
using Domain.Entities;

namespace Application.Services
{
    public class KpiHistoryService
    {
        private readonly KpiCalulationService _calService;
        private readonly ITagRepositary _tagRepo;
        private readonly IKpiResultRepository _resultRepository;

        public KpiHistoryService(
            KpiCalulationService calService,
            ITagRepositary tagRepo,
            IKpiResultRepository kpiResultRepository)
        {
            _calService = calService;
            _tagRepo = tagRepo;
            _resultRepository = kpiResultRepository;
        }

        public async Task Generatepreviousweek(int weeks=3)
        {
            var today = DateTime.UtcNow.Date;

            int lastSinceSunday = (int)today.DayOfWeek;

            var lastSunday = today.AddDays(-lastSinceSunday);

            var allKpiTags = await _tagRepo.GetAllKpiTags();

            var results = new List<KpiTable>();

            for (int week = 1; week <= weeks; week++)
            {
                var endtime = lastSunday.AddDays(-7 * (week - 1));
                var startTime = endtime.AddDays(-7);

                foreach (var tag in allKpiTags)
                {
                    var dto = new KpiRequestDto
                    {
                        tagId = tag.TagId,
                        startTime = startTime,
                        endTime = endtime
                    };

                    var kpiResult = await _calService.CalculateKpi(dto);

                    foreach (var asset in kpiResult.Assets)
                    {
                        if (asset.KpiValue == null)
                            continue;

                        var alreadyExists = await _resultRepository.IsAlreadyCalculated(
                            kpiResult.KpiName,
                            asset.AssetName,
                            startTime,
                            endtime);

                        if (alreadyExists)
                            continue;

                        var kpi = new KpiTable(
                            kpiName: kpiResult.KpiName,
                            assetName: asset.AssetName,
                            level: tag.TagType.TagName,
                            kpiValue: asset.KpiValue.Value,
                            weekNumber:week,
                            startTime: startTime,
                            endTime: endtime
                        );

                        results.Add(kpi);
                    }
                }
            }

            if (results.Any())
            {
                await _resultRepository.AddRangeAsync(results);
                await _resultRepository.SaveChangesAsync();
            }
        }
    }
}