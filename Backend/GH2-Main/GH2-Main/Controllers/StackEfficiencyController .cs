using Microsoft.AspNetCore.Mvc;
using Application.Services;
using Application.DTOS;
using System.Threading.Tasks;
using Application.Interface;

namespace Application.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StackEfficiencyController : ControllerBase
    {
        private readonly IStackEfficiencyService _efficiencyService;

        public StackEfficiencyController(IStackEfficiencyService efficiencyService)
        {
            _efficiencyService = efficiencyService;
        }

        // GET api/stackefficiency/{assetName}
        [HttpGet("{assetName}")]
        public async Task<ActionResult<EfficiencyGraphDto>> GetEfficiencyData(string assetName)
        {
            var result = await _efficiencyService.GetEfficiencyData(assetName);

            if (result == null || result.Actual == null || result.Actual.Count == 0)
                return NotFound($"No efficiency records found for asset: {assetName}");

            return Ok(result);
        }


        [HttpGet("{stackName}/latest")]
        public async Task<IActionResult> GetLatestEfficiency(string stackName)
        {
            var result = await _efficiencyService.GetLatestEfficiencyAsync(stackName);

            if (result.Status == "No data")
                return NotFound($"No efficiency record found for stack {stackName}");

            return Ok(result);
        }
    }
}