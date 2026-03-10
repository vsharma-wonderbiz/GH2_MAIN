using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOS;
using Application.Interface;

namespace Application.Services
{
    public class MappingService
    {
        private readonly IMappingRepositary _mapRepo;

        public MappingService(IMappingRepositary mapRepo)
        {
            _mapRepo = mapRepo;
        }

        public async Task<List<OpcConfigDto>> BuildTheOpcConfig()
        {
            var mappings = await _mapRepo.GetAllMappingWithConfigs();


            var result=new List<OpcConfigDto>();

            foreach (var mapping in mappings)
            {
                var Config=await _mapRepo.GetModbusConfigFromMapppingId(mapping.MappingId);
                int register = Config.RegisterAddress - 40001;
                var deadBand = Math.Round(mapping.Tag.Deadband, 4);

                result.Add(new OpcConfigDto
                {
                    AssetName = mapping.Asset.Name,
                    TagName = mapping.Tag.TagName,
                    OpcNodeId = mapping.OpcNodeId,
                    SlaveId = Config.SlaveId,
                    RegisterAddress = register,
                    Datatype = mapping.Tag.DataType,
                    RegisterCount = Config.RegisterCount,
                    FunctionCode = Config.FunctionCode,
                    Unit = mapping.Tag.Unit,
                    DisplayName = $"{mapping.Asset.Name}_{mapping.Tag.TagName}",
                    Deadband =deadBand,
                });


                
            }

            return result;
        }
    }
}
