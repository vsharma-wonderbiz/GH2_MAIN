using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Application.DTOS;
using Application.Interface;
using Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace Application.Services
{
    public class KpiCalulationService
    {
        private readonly IMappingRepositary _mappingRepository;
        private readonly ITagRepositary _tagRepositary;
        private readonly IAssetRepository _assetRepositary;
        private readonly IConfiguration _configuration;
        private readonly IAnalyticsRepository _analyticsRepository;
        private readonly KpiFormulaService _formulaService;

        // Tags that come directly from Plant mappings
        private readonly List<string> _plantDirectTags = new()
        {
            "power", "water_flow_tot", "water_conductivity"
        };

        // Tags that need to be summed across all child Stacks
        private readonly List<string> _stackAggregatedTags = new()
        {
            "h2flow"
        };

        public KpiCalulationService(
            IMappingRepositary mappingRepository,
            ITagRepositary tagRepositary,
            IAssetRepository assetRepositary,
            IConfiguration configuration,
            IAnalyticsRepository analyticsRepository,
            KpiFormulaService formulaService)
        {
            _mappingRepository = mappingRepository;
            _tagRepositary = tagRepositary;
            _assetRepositary = assetRepositary;
            _configuration = configuration;
            _analyticsRepository = analyticsRepository;
            _formulaService = formulaService;
        }

        public async Task<KpiMappingResultDto> CalculateKpi(KpiRequestDto dto)
        {
            // Get KPI tag info
            // Console.WriteLine($"passing to the calculation service{dto.startTime.ToString()}");
            // Console.WriteLine($"passing to the calculation service{dto.endTime.ToString()}");

            var kpiTag = await _tagRepositary.GetTagNameById(dto.tagId);
            var level = kpiTag.TagType.TagName; 
            var kpiName = kpiTag.TagName;

            Console.WriteLine(JsonSerializer.Serialize(kpiTag, new JsonSerializerOptions
            {
                WriteIndented = true // pretty-print with indentation
            }));
            Console.WriteLine($"{level.ToString()}");
            Console.WriteLine($"{kpiName.ToString()}");


            var dependentTagNames = _configuration
                .GetSection($"kpiDependencies:{level}:{kpiName}")
                .Get<List<string>>();

            Console.WriteLine($"these is dependent tag names {string.Join(",", dependentTagNames)}");

            if (dependentTagNames == null || !dependentTagNames.Any())
                return new KpiMappingResultDto { KpiName = kpiName, Assets = new List<AssetMappingDto>() };

            //fetch all the tags object from the tag tbale based on the dependent tags to calculte 
            //giv ethe whoe object the defined in the enetity 
            var dependentTags = await _tagRepositary.GetTagsByNames(dependentTagNames);
            Console.WriteLine(JsonSerializer.Serialize(dependentTags, new JsonSerializerOptions
            {
                WriteIndented = true // pretty-print with indentation
            }));

            // Branch based on level
            if (level == "Plant")
                return await CalculatePlantKpi(kpiName, dependentTags, dto.startTime, dto.endTime);
            else
                return await CalculateStackKpi(kpiName, dependentTags, dto.startTime, dto.endTime);
        }

        // ── PLANT ────────────────────────────────────────────────────────────────
            private async Task<KpiMappingResultDto> CalculatePlantKpi(
                string kpiName,
                List<Tag> dependentTags,
                DateTime startTime,
                DateTime endTime)
            {
                //Console.WriteLine($"these is the calculation service {startTime.ToString()}");
                //Console.WriteLine($"these is the calculation service {endTime.ToString()}");
                bool exists = await _analyticsRepository.DataExist();

                if (!exists)
                {
                    Console.WriteLine("No data available to calculate KPI");


                    return new KpiMappingResultDto
                    {
                        KpiName = kpiName,
                        Assets = new List<AssetMappingDto>()
                    };
                }
                // Fetch ALL plants automatically
                var plants = await _assetRepositary.GetAssetsByType("Plant");
                var resultAssets = new List<AssetMappingDto>();

                foreach (var plant in plants)
                {
                    var tagValues = new Dictionary<string, float>();
                    var allPlantMappingDetails = new List<TagMappingDto>();

                    // Fetch Plant-direct tag values
                    //these is done basically cause in some kpi the calultin is dorectly deoedn upon the plnats tag ids 
                    // but in soe cases we have to remove all the vlaue of the stack to get a vlaue for plnat and calculate so these is for that purpose 
                    var plantDirectTagIds = dependentTags
                        .Where(t => _plantDirectTags.Contains(t.TagName))
                        .Select(t => t.TagId).ToList();

                Console.WriteLine(JsonSerializer.Serialize(plantDirectTagIds, new JsonSerializerOptions
                {
                    WriteIndented = true // pretty-print with indentation
                }));


                if (plantDirectTagIds.Any())
                    {
                        //removing all the mapping from the mapping table
                        var plantMappings = await _mappingRepository
                            .GetMappingsByAssetIdsAndTagIds(
                                new List<int> { plant.AssetId }, plantDirectTagIds);


                    Console.WriteLine(JsonSerializer.Serialize(plantMappings, new JsonSerializerOptions
                    {
                        WriteIndented = true // pretty-print with indentation
                    }));

                    var range = endTime - startTime;
                    var plantAvgValues = new List<MappingAvgValueDto>() ;
                    Console.WriteLine($"these is the range between the startime and endtime {range.ToString()}");

                    if (range.TotalHours <= 1)
                    {
                        plantAvgValues = await _analyticsRepository
                            .GetRawAvgValuesFromMapping(
                            plantMappings.Select(m => m.MappingId).ToList(), startTime, endTime);

                        Console.WriteLine(JsonSerializer.Serialize(plantAvgValues, new JsonSerializerOptions
                        {
                            WriteIndented = true // pretty-print with indentation
                        }));

                    }
                    else
                    {
                         plantAvgValues = await _analyticsRepository
                                .GetAvgValuesForMappings(
                                    plantMappings.Select(m => m.MappingId).ToList(), startTime, endTime);

                        Console.WriteLine(JsonSerializer.Serialize(plantAvgValues, new JsonSerializerOptions
                        {
                            WriteIndented = true // pretty-print with indentation
                        }));
                    }


                    //    //using the mapping to fetch the avg vlaue use to calculate the kpi 
                    //    var plantAvgValues = await _analyticsRepository
                    //            .GetAvgValuesForMappings(
                    //                plantMappings.Select(m => m.MappingId).ToList(), startTime, endTime);

                    //Console.WriteLine(JsonSerializer.Serialize(plantAvgValues, new JsonSerializerOptions
                    //{
                    //    WriteIndented = true // pretty-print with indentation
                    //}));


                    // Fill tagValues dictionary for formula
                    foreach (var mapping in plantMappings)
                        {
                            var tagName = dependentTags.First(t => t.TagId == mapping.TagId).TagName;
                            tagValues[tagName] = plantAvgValues
                                .FirstOrDefault(a => a.MappingId == mapping.MappingId)?.AvgValue ?? 0f;
                        }

                        //Fill mapping details with real IDs
                        foreach (var mapping in plantMappings)
                        {
                            var tagName = dependentTags.First(t => t.TagId == mapping.TagId).TagName;
                            allPlantMappingDetails.Add(new TagMappingDto
                            {
                                MappingId = mapping.MappingId,
                                TagId = mapping.TagId,
                                Tagname = tagName,
                                AvgValue = plantAvgValues.FirstOrDefault(a => a.MappingId == mapping.MappingId)?.AvgValue ?? 0f,
                                MinValue = plantAvgValues.FirstOrDefault(a => a.MappingId == mapping.MappingId)?.MinValue ?? 0f,
                                MaxValue = plantAvgValues.FirstOrDefault(a => a.MappingId == mapping.MappingId)?.MaxValue ?? 0f
                            });
                        }
                    }

                    //SUM stack-aggregated tags across all child stacks
                    var stackAggTagIds = dependentTags
                        .Where(t => _stackAggregatedTags.Contains(t.TagName))
                        .Select(t => t.TagId).ToList();

                    if (stackAggTagIds.Any())
                    {
                        //removed all the child in plant 
                        var childStacks = await _assetRepositary.GetChildAssets(plant.AssetId);
                        //remove all the ids for the childs 
                        var stackAssetIds = childStacks.Select(s => s.AssetId).ToList();
                        //remove all the mapping baaed on asset ids and tag ids 
                        var stackMappings = await _mappingRepository
                            .GetMappingsByAssetIdsAndTagIds(stackAssetIds, stackAggTagIds);

                    var range = endTime - startTime;
                    var stackAvgValues = new List<MappingAvgValueDto>();


                    if (range.TotalHours <= 1)
                    {
                        stackAvgValues = await _analyticsRepository
                            .GetRawAvgValuesFromMapping(
                            stackMappings.Select(m => m.MappingId).ToList(), startTime, endTime);

                        Console.WriteLine(JsonSerializer.Serialize(stackAvgValues, new JsonSerializerOptions
                        {
                            WriteIndented = true // pretty-print with indentation
                        }));

                    }
                    else
                    {
                        stackAvgValues = await _analyticsRepository
                               .GetAvgValuesForMappings(
                                   stackMappings.Select(m => m.MappingId).ToList(), startTime, endTime);

                        Console.WriteLine(JsonSerializer.Serialize(stackAvgValues, new JsonSerializerOptions
                        {
                            WriteIndented = true // pretty-print with indentation
                        }));
                    }

                    //remove teh stack avg values
                    //var stackAvgValues = await _analyticsRepository
                    //        .GetAvgValuesForMappings(
                    //            stackMappings.Select(m => m.MappingId).ToList(), startTime, endTime);

                        // SUM each aggregated tag across all stacks for formula
                        foreach (var tag in dependentTags.Where(t => _stackAggregatedTags.Contains(t.TagName)))
                        {
                            tagValues[tag.TagName] = stackMappings
                                .Where(m => m.TagId == tag.TagId)
                                .Average(m => stackAvgValues
                                    .FirstOrDefault(a => a.MappingId == m.MappingId)?.AvgValue ?? 0f);
                        }

                        // Fill mapping details with real IDs for each stack mapping
                        foreach (var mapping in stackMappings)
                        {
                            var tagName = dependentTags.First(t => t.TagId == mapping.TagId).TagName;
                            allPlantMappingDetails.Add(new TagMappingDto
                            {
                                MappingId = mapping.MappingId,
                                TagId = mapping.TagId,
                                Tagname = tagName,
                                AvgValue = stackAvgValues.FirstOrDefault(a => a.MappingId == mapping.MappingId)?.AvgValue ?? 0f,
                                MinValue = stackAvgValues.FirstOrDefault(a => a.MappingId == mapping.MappingId)?.MinValue ?? 0f,
                                MaxValue = stackAvgValues.FirstOrDefault(a => a.MappingId == mapping.MappingId)?.MaxValue ?? 0f
                            });
                        }
                    }

                    //+ Calculate KPI and add to result ───────────────────────────
                    try
                    {
                        resultAssets.Add(new AssetMappingDto
                        {
                            AssetName = plant.Name,
                            KpiValue = _formulaService.Calculate(kpiName, tagValues),
                            Mappings = allPlantMappingDetails
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception: {ex}");
                        throw; // rethrow if you want the app to fail fast
                    }


                }

                return new KpiMappingResultDto
                {
                    KpiName = kpiName,
                    Assets = resultAssets
                };
            }

        // ── STACK ────────────────────────────────────────────────────────────────
        private async Task<KpiMappingResultDto> CalculateStackKpi(
            string kpiName,
            List<Tag> dependentTags,
            DateTime startTime,
            DateTime endTime)
        {

             bool exists = await _analyticsRepository.DataExist();

            if (!exists)
            {
                Console.WriteLine("No data available to calculate KPI");


                return new KpiMappingResultDto
                {
                    KpiName = kpiName,
                    Assets = new List<AssetMappingDto>()
                };
            }
            var dependentTagIds = dependentTags.Select(t => t.TagId).ToList();

            // Get all stacks
            var assets = await _assetRepositary.GetAssetsByType("Stack");
            var assetIds = assets.Select(a => a.AssetId).ToList();

            // Get mappings for all stacks with dependent tags
            var mappings = await _mappingRepository
                .GetMappingsByAssetIdsAndTagIds(assetIds, dependentTagIds);

            // Fetch avg values
            var avgValues = await _analyticsRepository
                .GetAvgValuesForMappings(
                    mappings.Select(m => m.MappingId).ToList(), startTime, endTime);

            // Build response per stack with KPI calculated individually
            return new KpiMappingResultDto
            {
                KpiName = kpiName,
                Assets = assets.Select(asset =>
                {
                    var assetMappings = mappings
                        .Where(m => m.AssetId == asset.AssetId).ToList();

                    // Build tag dictionary for formula
                    var tagValues = assetMappings.ToDictionary(
                        m => dependentTags.First(t => t.TagId == m.TagId).TagName,
                        m => avgValues.FirstOrDefault(a => a.MappingId == m.MappingId)?.AvgValue ?? 0f
                    );

                    return new AssetMappingDto
                    {
                        AssetName = asset.Name,
                        KpiValue = _formulaService.Calculate(kpiName, tagValues),
                        Mappings = assetMappings.Select(m => new TagMappingDto
                        {
                            MappingId = m.MappingId,
                            TagId = m.TagId,
                            Tagname = dependentTags.FirstOrDefault(t => t.TagId == m.TagId)?.TagName,
                            AvgValue = avgValues.FirstOrDefault(a => a.MappingId == m.MappingId)?.AvgValue ?? 0f,
                            MinValue = avgValues.FirstOrDefault(a => a.MappingId == m.MappingId)?.MinValue ?? 0f,
                            MaxValue = avgValues.FirstOrDefault(a => a.MappingId == m.MappingId)?.MaxValue ?? 0f
                        }).ToList()
                    };
                }).ToList()
            };
        }
    }
}