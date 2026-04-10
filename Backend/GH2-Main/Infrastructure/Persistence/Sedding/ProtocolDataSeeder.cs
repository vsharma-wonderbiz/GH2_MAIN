using System;
using System.Text.Json;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IO;

using Application.DTOS;
using System.Diagnostics.CodeAnalysis;
using AutoMapper;
using Application.Interface;

namespace Infrastructure.Persistence.Sedding
{
    public class ProtocolDataSeeder
    {
        private readonly IConfiguration _configuration;
        private readonly IMappingRepositary _mapRepo;

        public ProtocolDataSeeder(IConfiguration configuration,IMappingRepositary mapRepo)
        {
            _configuration = configuration;
            _mapRepo = mapRepo;
        }
        public  async Task SeedAsync(ApplicationDbContext context)
        {
            string relativePath = _configuration["ModbusConfig:FilePath"];
            string fileContent = string.Empty;

            try
            {
                fileContent = File.ReadAllText(relativePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading file: {ex.Message}");
                return; // stop execution if file can't be read
            }

            var modbusConfig = JsonSerializer.Deserialize<modbusConfig>(fileContent);
            string debugJson = JsonSerializer.Serialize(modbusConfig, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(debugJson);


            if (await context.ProtocolConfig.AnyAsync())
               return;

            if (modbusConfig == null)
            {
                throw new Exception("no config found");
            }
                

            foreach(var config in modbusConfig.stacks)
            {
                foreach (var signal in config.signals)
                {
                    var mappingid = await _mapRepo.GetMappingIdFromAssetandTag(config.stack, signal.name);
                    int register = signal.registers[0] + 40001;

                    var newprotocol = new ProtocolConfig(mappingid, register, 2, 3, 1);

                    await context.ProtocolConfig.AddAsync(newprotocol);
                }
            }


            foreach (var config in modbusConfig.plant)
            {
                Console.WriteLine($"these is the config name {config.name}");
                  var mappingid = await _mapRepo.GetMappingIdFromAssetandTag("Plant_1", config.name);

                Console.WriteLine($"the is plant mapping id  {mappingid}");
                
                    int register = config.registers[0] + 40001;

                    var newprotocol = new ProtocolConfig(mappingid, register, 2, 3, 1);

                    await context.ProtocolConfig.AddAsync(newprotocol);
               
            }




            await context.SaveChangesAsync();
        }

        
    }
}