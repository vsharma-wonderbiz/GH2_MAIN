using Application.Services;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace GH2_Main.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BackfillDataController : ControllerBase
    {
        private readonly BackfillSensorDataService _backfillService;
        private readonly SeedingGate _seedingGate;
        private readonly ILogger<BackfillDataController> _logger;
        private readonly PastWeeksAggregatedData _PastweekServce;
        private readonly KpiHistoryService _kpiHistory;


        public BackfillDataController(
            BackfillSensorDataService backfillService,
            ILogger<BackfillDataController> logger,
            PastWeeksAggregatedData pastweekServce,
            KpiHistoryService kpiHistory,
            SeedingGate seedingGate)
        {
            _backfillService = backfillService;
            _logger = logger;
            _PastweekServce = pastweekServce;
            _kpiHistory = kpiHistory;
            _seedingGate = seedingGate;
        }


        [HttpPost("backfill/asset/{assetName}")]
        public async  Task<IActionResult> BackfillAsset(string assetName)
        {
                try
                {
                    _logger.LogInformation($"Backfill started for asset {assetName}");
                    await _backfillService.BackfillAssetAsync(assetName);
                    _logger.LogInformation($"Backfill completed for asset {assetName}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Backfill failed for asset {assetName}");
                }

            return Accepted(new
            {
                message = $"Backfill started in background for asset: {assetName}"
            });
        }

        [HttpPost("pastAvg")]
        public async Task<IActionResult> PastWeekAverage()
        {
            await _PastweekServce.RunAsync();
            return Ok("Check Console");
        }

        [HttpPost("pastKpi")]
        public async Task<IActionResult> PastWeekKpis()
        {
            await _kpiHistory.Generatepreviousweek();
            return Ok("Check Db for the results");
        }

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok(new { isReady = _seedingGate.IsReady });
        }

        // POST api/seeding/mark-seeding-complete
        [HttpPost("mark-seeding-complete")]
        public IActionResult MarkSeedingComplete()
        {
            _seedingGate.SignalReady();
            return Ok(new { message = "Seeding marked complete. Background processing will now start (if not already running)." });
        }

    }
}