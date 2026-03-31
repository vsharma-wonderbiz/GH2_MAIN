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
        private readonly KpiQueryService _kpiQueryService;

        public AnalyticsController(IAnalyticsService analyticsService,KpiQueryService kpiQueryService)
        {
            _analyticsService = analyticsService;
            _kpiQueryService = kpiQueryService;
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

        //[HttpPost("kpi")]
        //public async Task<IActionResult> BuildingKpiService(KpiRequestDto dto)
        //{
        //    var result = await _kpiCalulationService.CalculateKpi(dto);
        //    return Ok(result);
        //}

        [HttpPost("Kpi")]
        public async Task<IActionResult> GetKpi([FromBody] KpiQueryRequestDto request)
        {
            Console.WriteLine($"TagId: {request.TagId}, TimeRange: {request.TimeRange}");
            try
            {
                if (request.TagId <= 0)
                    return BadRequest("TagId is required and must be greater than 0.");

                if (request.TimeRange == KpiTimeRange.Custom)
                {
                    if (request.CustomStart == null || request.CustomEnd == null)
                        return BadRequest("CustomStart and CustomEnd are required for Custom time range.");

                    if (request.CustomStart >= request.CustomEnd)
                        return BadRequest("CustomStart must be before CustomEnd.");
                }

                var result = await _kpiQueryService.GetKpiAsync(request);

                if (result == null || !result.Assets.Any())
                    return NotFound($"No KPI data found for TagId {request.TagId}.");

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }


        //[HttpGet("latest/{stackName}")]
        //public async Task<IActionResult> GetLatestKpis(string stackName)
        //{
        //    var result = await _kpiQueryService.GetLatestKpisAsync(stackName);

        //    if (result == null)
        //        return NotFound("No KPI data found");

        //    return Ok(result);
        //}

        [HttpPost("PlantKpis")]
        public async Task<IActionResult> GetLatestPlantKpiOnWeeks(PlantKpiRequestDto requestDto)
        {
           var result= await _kpiQueryService.GetPlantKpiBased(requestDto);
            return Ok(result);
        }
    }
}