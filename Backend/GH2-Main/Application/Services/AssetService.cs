using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOS;
using Application.Interface;
using Domain.Entities;

namespace Application.Services
{
    public class AssetService : IAssetService
    {
        private readonly IRepository<Assets> _assetRepo;

        public AssetService(IRepository<Assets> assetRepo)
        {
            _assetRepo = assetRepo;
        }

        public async Task CreateAssetAsync(CreateAssetDto dto)
        {
           

            var asset = await _assetRepo.GetByNameAsync(dto.Name);

            if (asset != null)
                throw new InvalidOperationException("Asset with this name already exists.");

            var addAsset = new Assets(dto.Name, dto.ParentAssetId);

            await _assetRepo.AddAsync(addAsset);
            await _assetRepo.SaveChangesAsync();


        }
    }
}
