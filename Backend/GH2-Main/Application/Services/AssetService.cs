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
        private readonly IMappingRepositary _mapRepo;

        public AssetService(IAssetRepository assetRepo, IMappingRepositary maopRepo)
        {
            _assetRepo = assetRepo;
            _mapRepo = maopRepo;
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

        public async Task<List<Assets>> GetChildAssetsAsync(int parentassetID)
        {
            var childAssets = await _assetRepo.GetChildAssets(parentassetID);

            if (childAssets == null || childAssets.Count == 0)
                throw new InvalidOperationException(
                    $"No child assets found for parent asset ID {parentassetID}."
                );

            return childAssets;
        }

        public async Task<List<Assets>> GetAllPlantsAsync()
        {
           
                var plants = await _assetRepo.GetAllPlants();

                if (plants == null || plants.Count==0)
                    throw new InvalidOperationException("No plants found in the system.");

            return plants;
        }

        public async Task<List<MappingDto>> GetAllMappingsOnStackAsync(int stackId)
        {
           
                var mappings = await _mapRepo.GetAllMappingsOnStack(stackId);

                if (mappings == null || mappings.Count==0)
                    throw new InvalidOperationException($"No mappings found for stack ID {stackId}.");

                return mappings.Select(m => new MappingDto
                {
                    MappingId = m.MappingId,
                    AssetName = m.Asset?.Name ?? "Unknown Asset",
                    TagName = m.Tag?.TagName ?? "Unknown Tag"
                }).ToList();

        }



    }
}
