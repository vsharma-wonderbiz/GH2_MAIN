using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOS;

namespace Application.Interface
{
    public  interface IStackEfficiencyService 
    {
        Task<EfficiencyGraphDto> GetEfficiencyData(string assetname);

        Task<EfficiencyResult> GetLatestEfficiencyAsync(string stackName);
    }
}
