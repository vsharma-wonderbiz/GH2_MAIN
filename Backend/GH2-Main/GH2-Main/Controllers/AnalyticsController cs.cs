using Application.DTOS;
using Application.Interface;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;
        private readonly KpiCalulationService _kpiCalulationService;

        public AnalyticsController(IAnalyticsService analyticsService,KpiCalulationService kpiCalulationService)
        {
            _analyticsService = analyticsService;
            _kpiCalulationService= kpiCalulationService;
        }

        [HttpPost("data")]
        public async Task<IActionResult> GetAnalyticsData([FromBody] AnalyticsRequestDto dto)
        {
            try
            {
                var result = await _analyticsService.GetAnalyticsData(dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Internal server error." });
            }
        }

        [HttpPost("kpi")]
        public async Task<IActionResult> BuildingKpiService(KpiRequestDto dto)
        {
            var result = await _kpiCalulationService.CalculateKpi(dto);
            return Ok(result);
        }
    }
}