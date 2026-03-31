using Domain.Entities;
using Infrastructure.Persistence;
using Application.Interface;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Implementation
{
    public class AssetRepository : Repository<Assets>, IAssetRepository
    {
        public AssetRepository(ApplicationDbContext context)
            : base(context)
        {
        }

        public async Task<Assets?> GetByNameAsync(string name)
        {
            return await _dbset
                .FirstOrDefaultAsync(x => x.Name == name);
        }

        public async Task<List<Assets>> GetAssetsByType(string assetType)
        {
            return await _context.Assets
                .Where(a => a.AssetType == assetType)
                .ToListAsync();
        }

        public async Task<List<Assets>> GetChildAssets(int parentAssetId)
        {
            return await _context.Assets
                .Where(a => a.ParentAssetId == parentAssetId)
                .ToListAsync();
        }

        public async Task<List<Assets>> GetAllPlants()
        {
            return await _context.Assets
                .Where(a => a.ParentAssetId == null)
                .ToListAsync();
        }

        //public async Task<List<Assets>> GetChildrenAsync(int parentId)
        //{
        //    return await _dbset
        //        .Where(x => x.ParentAssetId == parentId)
        //        .ToListAsync();
        //}
    }
}