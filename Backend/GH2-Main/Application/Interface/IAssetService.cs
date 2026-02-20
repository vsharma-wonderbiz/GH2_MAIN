using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOS;
using Domain.Entities


namespace Application.Interface
{
    public interface IAssetService
    {
        Task<Assets?> GetAssetById(int id);

        Task CreateAssetAsync(CreateAssetDto);

    }
}
