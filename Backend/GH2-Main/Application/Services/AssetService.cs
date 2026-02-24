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
        private readonly IAssetRepository _assetRepo;

        public AssetService(IAssetRepository assetRepo)
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

        public async Task<Assets?> GetAssetById(int id)
        {
            var asset = await _assetRepo.GetByIdAsync(id);

            if (asset != null)
                throw new InvalidOperationException("Asset Asset Not Present.");

            return asset;
        }
    }
}
