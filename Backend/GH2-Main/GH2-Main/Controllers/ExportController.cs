using Application.DTOS;
using Application.Interface;
using Microsoft.AspNetCore.Mvc;

namespace GH2_Main.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExportController : Controller
    {
        private readonly IExportService _exportService;

        public ExportController(IExportService exportService)
        {
            _exportService = exportService;
        }

        [HttpPost]
        public async Task<IActionResult> PublishExportRequest([FromBody] ExportRequestDto requestDto)
        {
            try
            {

                var user = User.Identity?.Name ?? "Unkown";
                await _exportService.PublishExportRequest(requestDto,user);

                return Ok(new
                {
                    Success = true,
                    Message = "Export request accepted. Report generation is in progress."
                });
            }catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}/download")]
        public async Task<IActionResult> Download(int id)
        {
            var file = await _exportService.DownloadExportFile(id);

            return File(
                file.FileStream,
                file.ContentType,
                file.FileName);
        }

        [HttpGet]
        public async Task<IActionResult> GetExports([FromQuery] PaginationRequest request)
        {
            var result = await _exportService.GetPagedExports(
                request.PageNumber,
                request.Pagesize);

            return Ok(result);
        }

    }
}
