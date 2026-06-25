using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOS;
using Application.Interface;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Implementation
{
    public class ExportRequestRepository : Repository<ExportRequest>, IExportRequestRepository
    {
        public ExportRequestRepository(ApplicationDbContext context)
            : base(context)
        {
        }

        public async Task<List<ExportRequest>> GetAllCompletedExports()
        {
            return await _context.ExportRequest.Where(a => a.Status == "Completed").ToListAsync();
        }

        public async Task<PagedResult<ExportRequest>> GetPagedExport(int pagenumber,int pagesize)
        {
            int totalrecords = await _context.ExportRequest.CountAsync();

            var data = await _context.ExportRequest.
                OrderByDescending(x => x.RequestedAt)
                .Skip((pagenumber - 1) * pagesize)
                .Take(pagesize)
                .ToListAsync();

            return new PagedResult<ExportRequest>
            {
                Items = data,
                totalcounts = totalrecords,
                PageNumber = pagenumber,
                PageSize = pagesize
            };
        }
    }
}
