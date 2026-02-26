using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOS;

namespace Application.Interface
{
    public interface IAnalyticsService
    {
        Task<AnalyticsResponseDto> GetAnalyticsData(AnalyticsRequestDto dto);
    }
}
