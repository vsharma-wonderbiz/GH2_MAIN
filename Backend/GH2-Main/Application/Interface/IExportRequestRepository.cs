using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Application.DTOS;

namespace Application.Interface
{
    public interface IExportRequestRepository : IRepository<ExportRequest>
    {
        Task<List<ExportRequest>> GetAllCompletedExports();
        Task<PagedResult<ExportRequest>> GetPagedExport(int pagenumber, int pagesize);
    }
}
