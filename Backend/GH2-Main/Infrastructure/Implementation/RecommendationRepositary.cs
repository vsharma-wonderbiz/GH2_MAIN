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
    public class RecommendationRepositary : Repository<RecommendationInfo>,IRecommendationRepositary
    {
        public RecommendationRepositary(ApplicationDbContext context)
           : base(context)
        {
        }

        public async Task<RecommendationInfo?> GetActiveRecommendation(int mappingId, string name)
        {
            return await _context.Recommendations.FirstOrDefaultAsync(a => a.MappingId == mappingId && a.SignalName == name && a.Status == "Active");
        }

        public async Task<List<RecommendationInfo>> GetAllLatestRecommendation()
        {
            return await _context.Recommendations
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .ToListAsync();
        }
    }
}
