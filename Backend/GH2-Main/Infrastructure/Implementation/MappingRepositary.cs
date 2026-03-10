using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interface;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Implementation
{
    public class MappingRepositary :  IMappingRepositary
    {
        private readonly ApplicationDbContext _context;

        public MappingRepositary(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<List<int>> GetDependentTagMAppingId(List<int> assetIds, List<int> tagIds)
        {

            var MappingIds = await _context.Mappings
                            .Where(m => assetIds.Contains(m.AssetId) && tagIds.Contains(m.TagId))
                            .Select(m => m.MappingId).ToListAsync();

            return MappingIds;

        }

        public async Task<List<MappingTable>> GetMappingsByAssetIdsAndTagIds(
         List<int> assetIds, List<int> tagIds)
        {

            return await _context.Mappings
                .Include(m => m.Asset)
                .Include(m => m.Tag)
                .Where(m => assetIds.Contains(m.AssetId) && tagIds.Contains(m.TagId))
                .ToListAsync();

        }


        public async Task<List<MappingTable>> GetAllMappingWithConfigs()
        {
            return await _context.Mappings
                 .Where(m=>m.Tag.IsDerived==false)
                .Include(m => m.Asset)
                .Include(m => m.Tag)
                .ToListAsync();
        }

        public async Task<ProtocolConfig> GetModbusConfigFromMapppingId(int mappingId)
        {
            return await _context.ProtocolConfig.FirstOrDefaultAsync(a => a.MappingId == mappingId);
        }
    }
}
