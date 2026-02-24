using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace GH2_Main.Controllers
{
    /// <summary>
    /// TEMPORARY controller for backfill operations.
    /// This controller is a utility and can be deleted after backfill completes.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class BackfillDataController : ControllerBase
    {
        private readonly BackfillSensorDataService _backfillService;

        public BackfillDataController(BackfillSensorDataService backfillService)
        {
            _backfillService = backfillService;
        }

        /// <summary>
        /// Backfill 1 month of sensor data for a specific asset and all its tags.
        /// Current date: NOW, Range: NOW - 30 days to NOW
        /// 1 entry per second using Modbus-like simulation logic.
        /// </summary>  
        [HttpPost("backfill/asset/{assetName}")]
        public async Task<IActionResult> BackfillAsset(string assetName)
        {
            try
            {
                await _backfillService.BackfillAssetByNameAsync(assetName);
                return Ok(new { message = $"Backfill completed for asset: {assetName}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Backfill sensor data for a specific asset in a custom date range.
        /// </summary>
        [HttpPost("backfill/asset/{assetName}/range")]
        public async Task<IActionResult> BackfillAssetRange(
            string assetName,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                if (startDate >= endDate)
                    return BadRequest("Start date must be before end date");

                await _backfillService.BackfillAssetAsync(assetName, startDate, endDate);
                return Ok(new { message = $"Backfill completed for asset: {assetName}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get backfill statistics.
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var stats = await _backfillService.GetStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Clear all sensor raw data (DANGEROUS - use with caution!).
        /// </summary>
        [HttpDelete("clear/all")]
        public async Task<IActionResult> ClearAllData()
        {
            try
            {
                await _backfillService.ClearSensorDataAsync();
                return Ok(new { message = "All sensor data cleared" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Clear sensor data for a specific asset.
        /// </summary>
        [HttpDelete("clear/asset/{assetName}")]
        public async Task<IActionResult> ClearAssetData(string assetName)
        {
            try
            {
                await _backfillService.ClearSensorDataForAssetAsync(assetName);
                return Ok(new { message = $"Sensor data cleared for asset: {assetName}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
