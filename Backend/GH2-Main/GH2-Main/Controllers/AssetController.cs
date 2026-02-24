using Microsoft.AspNetCore.Mvc;
using Application.Interface;
using Application.DTOS;
using System;
using System.Threading.Tasks;

namespace GH2_Main.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AssetController : ControllerBase
    {
        private readonly IAssetService _assetService;
        private readonly ILogger<AssetController> _logger;

        public AssetController(IAssetService assetService, ILogger<AssetController> logger)
        {
            _assetService = assetService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAsset([FromBody] CreateAssetDto dto)
        {
            try
            {
                _logger.LogInformation("Creating asset with name {Name}", dto.Name);

                await _assetService.CreateAssetAsync(dto);

                _logger.LogInformation("Asset {Name} created successfully", dto.Name);

                return Ok("Asset created successfully.");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Asset creation failed due to duplicate name {Name}", dto.Name);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating asset {Name}", dto.Name);
                return StatusCode(500, "Something went wrong.");
            }
        }
    }
}