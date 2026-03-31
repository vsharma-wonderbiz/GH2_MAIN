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
        private readonly ITagRepositary _tagRepositary;

        public AssetController(IAssetService assetService, ILogger<AssetController> logger,ITagRepositary tagRepositary)
        {
            _assetService = assetService;
            _logger = logger;
            _tagRepositary = tagRepositary;
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

        [HttpGet("{parentAssetId}/Stacks")]
        public async Task<IActionResult> GetChildAssets(int parentAssetId)
        {
            try
            {
                var childAssets = await _assetService.GetChildAssetsAsync(parentAssetId);

                return Ok(childAssets);
            }
            catch (InvalidOperationException ex)
            {
                // Business rule violation (e.g., no child assets found)
                return NotFound(new { message = ex.Message });
            }
            catch (ApplicationException ex)
            {
                // Wrapped exception from service
                return StatusCode(500, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Unexpected error
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }


        [HttpGet("plants")]
        public async Task<IActionResult> GetAllPlants()
        {
            try
            {
                var plants = await _assetService.GetAllPlantsAsync();
                return Ok(plants);
            }
            catch (InvalidOperationException ex)
            {
                // Business rule violation (no plants found)
                return NotFound(new { message = ex.Message });
            }
            catch (ApplicationException ex)
            {
                // Wrapped exception from service
                return StatusCode(500, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Unexpected error
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        [HttpGet("{stackId}/mappings")]
        public async Task<IActionResult> GetAllMappingsOnStack(int stackId)
        {
            try
            {
                var mappings = await _assetService.GetAllMappingsOnStackAsync(stackId);
                return Ok(mappings);
            }
            catch (InvalidOperationException ex)
            {
                
                return NotFound(new { message = ex.Message });
            }
            catch (ApplicationException ex)
            {
                
                return StatusCode(500, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        [HttpGet("PlantKpis")]
        public async Task<IActionResult> GetAllPlantKpis()
        {
            try
            {
                var Kpi = await _tagRepositary.GetAllPlantKpiTags();

                var result = Kpi.Select(k => new
                {
                    TagId=k.TagId,
                    TagName=k.TagName
                });
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Unexpected error
                return StatusCode(500, new { message = "An unexpected error occurred.", detail = ex.Message });
            }
        }


        
    }
}