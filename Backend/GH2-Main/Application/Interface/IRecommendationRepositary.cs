using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interface
{
    public interface IRecommendationRepositary:IRepository<RecommendationInfo>
    {
        Task<RecommendationInfo?> GetActiveRecommendation(int mappingId, string name);

        Task<List<RecommendationInfo>> GetAllLatestRecommendation();
    }
}
