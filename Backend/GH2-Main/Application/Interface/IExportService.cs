using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOS;
using Domain.Entities;

namespace Application.Interface
{
    public interface IExportService
    {
        Task PublishExportRequest(ExportRequestDto request,string user);
        Task<DownloadFileDto> DownloadExportFile(int ExportJobId);

        Task<PagedResult<ExportRequest>> GetPagedExports(
      int pageNumber,
      int pageSize);
    }
}
