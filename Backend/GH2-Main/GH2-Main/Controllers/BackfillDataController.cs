using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace GH2_Main.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BackfillDataController : ControllerBase
    {
        private readonly BackfillSensorDataService _backfillService;
        private readonly ILogger<BackfillDataController> _logger;

        public BackfillDataController(
            BackfillSensorDataService backfillService,
            ILogger<BackfillDataController> logger)
        {
            _backfillService = backfillService;
            _logger = logger;
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

        // -------------------------------------------------------
        // BACKFILL CUSTOM RANGE
        // -------------------------------------------------------

        //[HttpPost("backfill/asset/{assetName}/range")]
        //public IActionResult BackfillAssetRange(
        //    string assetName,
        //    [FromQuery] DateTime startDate,
        //    [FromQuery] DateTime endDate)
        //{
        //    if (startDate >= endDate)
        //        return BadRequest("Start date must be before end date");

        //    if ((endDate - startDate).TotalDays > 90)
        //        return BadRequest("Maximum allowed range is 90 days");

        //    Task.Run(async () =>
        //    {
        //        try
        //        {
        //            _logger.LogInformation($"Backfill started for {assetName} | {startDate} - {endDate}");
        //            await _backfillService.BackfillAssetAsync(assetName, startDate, endDate);
        //            _logger.LogInformation($"Backfill completed for {assetName}");
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, $"Backfill failed for {assetName}");
        //        }
        //    });

        //    return Accepted(new
        //    {
        //        message = $"Backfill started in background for asset: {assetName}"
        //    });
        //}

        //// -------------------------------------------------------
        //// STATS
        //// -------------------------------------------------------

        //[HttpGet("stats")]
        //public async Task<IActionResult> GetStats()
        //{
        //    var stats = await _backfillService.GetStatsAsync();
        //    return Ok(stats);
        //}

        //// -------------------------------------------------------
        //// CLEAR ALL DATA
        //// -------------------------------------------------------

        //[HttpDelete("clear/all")]
        //public async Task<IActionResult> ClearAllData()
        //{
        //    await _backfillService.ClearSensorDataAsync();
        //    return Ok(new { message = "All sensor data cleared" });
        //}

        //// -------------------------------------------------------
        //// CLEAR ASSET DATA
        //// -------------------------------------------------------

        //[HttpDelete("clear/asset/{assetName}")]
        //public async Task<IActionResult> ClearAssetData(string assetName)
        //{
        //    await _backfillService.ClearSensorDataForAssetAsync(assetName);
        //    return Ok(new { message = $"Sensor data cleared for asset: {assetName}" });
        //}
    }
}