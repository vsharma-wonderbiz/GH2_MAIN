using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace GH2_Main.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MappingController : Controller
    {
        private readonly MappingService _mapService;

        public MappingController(MappingService mapService)
        {
            _mapService = mapService;
        }

        [HttpGet("Config")]
        public async Task<IActionResult> GetOpcConfig()
        {
            try
            {
                var result = await _mapService.BuildTheOpcConfig();
                return Ok(result);
            }catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
