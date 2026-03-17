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
            //all the physcial tags mapping 
            var mappings = await _mapRepo.GetAllMappingWithConfigs();


            var result=new List<OpcConfigDto>();

            foreach (var mapping in mappings)
            {
                bool isExist = await _mapRepo.Isconfig(mapping.MappingId);

                if (isExist)
                {

                    var Config = await _mapRepo.GetModbusConfigFromMapppingId(mapping.MappingId);
                    int register = Config.RegisterAddress - 40001;
                    var deadBand = Math.Round(mapping.Tag.Deadband, 4);

                    result.Add(new OpcConfigDto
                    {
                       asset_name = mapping.Asset.Name,
                        tag_name = mapping.Tag.TagName,
                        opc_node_id = mapping.OpcNodeId,
                        slave_id = Config.SlaveId,
                        register_address = register,
                        datatype = mapping.Tag.DataType,
                        register_count = Config.RegisterCount,
                        function_code = Config.FunctionCode,
                        unit = mapping.Tag.Unit,
                        display_name = $"{mapping.Asset.Name}_{mapping.Tag.TagName}",
                        deadband = deadBand,
                    });

                }


                
            }

            return result;
        }
    }
}
