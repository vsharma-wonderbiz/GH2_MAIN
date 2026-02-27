using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interface
{
    public interface IMappingRepositary
    {
        Task<List<int>> GetDependentTagMAppingId(List<int> assetIds,List<int> tagIds);
        

    }
}
