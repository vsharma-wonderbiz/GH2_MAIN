    using Xunit;
    using Moq;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Application.Services;
    using Application.Interface;
    using Application.DTOS;
    using Domain.Entities;

    public class MappingServiceTests
    {
        private readonly Mock<IMappingRepositary> _mockRepo;
        private readonly MappingService _service;

        public MappingServiceTests()
        {
            _mockRepo = new Mock<IMappingRepositary>();
            _service = new MappingService(_mockRepo.Object);
        }

        [Fact]
        public async Task BuildTheOpcConfig_ReturnsValidData()
        {
        // Arrange
        var mapping = new MappingTable(1, 1, "node12");

         typeof(MappingTable)
           .GetProperty("MappingId")
           .SetValue(mapping, 1);

        typeof(MappingTable)
            .GetProperty("Asset")
            .SetValue(mapping, new Assets("Pump"));

        typeof(MappingTable)
            .GetProperty("Tag")
            .SetValue(mapping, new Tag(
                1, "Temp", "C", 0.123456f, 1.25f, "float32", 0.9f, false
            ));

        var mappings=new List<MappingTable> { mapping };

        var config = new ProtocolConfig(1, 40010, 2, 3, 2);


        _mockRepo.Setup(x => x.GetAllMappingWithConfigs())
                     .ReturnsAsync(mappings);

            _mockRepo.Setup(x => x.Isconfig(1))
                     .ReturnsAsync(true);

            _mockRepo.Setup(x => x.GetModbusConfigFromMapppingId(1))
                     .ReturnsAsync(config);

            // Act
            var result = await _service.BuildTheOpcConfig();

            // Assert
            Assert.Single(result);
            Assert.Equal("Pump", result[0].asset_name);
            Assert.Equal("Temp", result[0].tag_name);
            Assert.Equal(9, result[0].register_address); // 40010 - 40001
            Assert.Equal(0.9, result[0].deadband);
        }

        [Fact]
        public async Task BuildTheOpcConfig_Throws_WhenAssetIsNull()
        {
        var mappings = new List<MappingTable>
            {
               new MappingTable(1,2,"string")
            };

        _mockRepo.Setup(x => x.GetAllMappingWithConfigs())
                     .ReturnsAsync(mappings);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.BuildTheOpcConfig());
        }

        [Fact]
        public async Task BuildTheOpcConfig_Throws_WhenTagIsNull()
        {
        var mappings = new List<MappingTable>
            {
               new MappingTable(1,2,"string")
            };

        _mockRepo.Setup(x => x.GetAllMappingWithConfigs())
                     .ReturnsAsync(mappings);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.BuildTheOpcConfig());
        }

        [Fact]
        public async Task BuildTheOpcConfig_Throws_WhenConfigIsNull()
        {
        var mappings = new List<MappingTable>
            {
               new MappingTable(1,2,"string")
            };



        _mockRepo.Setup(x => x.GetAllMappingWithConfigs())
                     .ReturnsAsync(mappings);

            _mockRepo.Setup(x => x.Isconfig(1))
                     .ReturnsAsync(true);

          _mockRepo.Setup(x => x.GetModbusConfigFromMapppingId(1))
                   .ReturnsAsync((ProtocolConfig?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.BuildTheOpcConfig());
        }

        [Fact]
        public async Task BuildTheOpcConfig_Skips_WhenConfigDoesNotExist()
        {
        var mapping = new MappingTable(1, 1, "node12");

        typeof(MappingTable)
            .GetProperty("Asset")
            .SetValue(mapping, new Assets("pump"));


        typeof(MappingTable)
            .GetProperty("Tag")
            .SetValue(mapping, new Tag(1, "power", "v", 0.025f, 1.25f, "float32", 0.9f, true));

        var mappings = new List<MappingTable> { mapping };

        _mockRepo.Setup(x => x.GetAllMappingWithConfigs())
                     .ReturnsAsync(mappings);

            _mockRepo.Setup(x => x.Isconfig(1))
                     .ReturnsAsync(false);

            var result = await _service.BuildTheOpcConfig();

            Assert.Empty(result);
        }
    }