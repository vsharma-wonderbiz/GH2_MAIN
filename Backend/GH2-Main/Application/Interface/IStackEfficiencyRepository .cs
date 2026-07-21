using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interface
{
    public interface IStackEfficiencyRepository
    {
        Task<List<StackEfficiencyRecord>> GetDownSampledRecords(string assetman, int NumberOfRecords);

        Task<StackEfficiencyRecord> GetLatestEfficiency(string stackName);
    }
}
