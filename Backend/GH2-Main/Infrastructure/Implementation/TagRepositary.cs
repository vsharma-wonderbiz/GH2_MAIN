using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interface;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Implementation
{
    public class TagRepositary : ITagRepositary
    {
        private readonly ApplicationDbContext _context;

        public TagRepositary(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Tag?> GetTagNameById(int tagId)
        {
            return await _context.Tags
                        .Include(t => t.TagType)
                        .FirstOrDefaultAsync(t => t.TagId == tagId);
        }

        public async Task<List<Tag>> GetTagsByNames(List<string> tagNames)
        {
            return await _context.Tags
                .Where(t => tagNames.Contains(t.TagName))
                .ToListAsync();
        }

        public async Task<List<Tag>> GetAllKpiTags()
        {
            return await _context.Tags
                .Include(t => t.TagType)
                .Where(t => t.IsDerived == true)  // KPI tags are derived tags
                .ToListAsync();
        }

    


    }
}
