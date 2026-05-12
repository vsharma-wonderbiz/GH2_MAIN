using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOS;
using Application.Interface;
using Application.Services;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Gh2_Test.Services
{
    public class AnalyticsServiceTest
    {
        private readonly Mock<IAnalyticsRepository> _mockRepo;
        private readonly Mock<IAssetRepository> _mockAssetRepo;
        private readonly AnalyticsService _service;

        public AnalyticsServiceTest()
        {
            _mockRepo= new Mock<IAnalyticsRepository>();
            _mockAssetRepo=new Mock<IAssetRepository>();
            _service = new AnalyticsService(
                _mockRepo.Object,
                _mockAssetRepo.Object
                );
        }
        [Fact]
        public async Task GetAnalyticsData_ShouldThrowException_WhenAssetNotFound()
        {
            // Arrange
            var request = new AnalyticsRequestDto
            {
                AssetName = "Pump1",
                TagName = "Temperature",
                StartTime = DateTime.UtcNow.AddHours(-1),
                EndTime = DateTime.UtcNow
            };

            _mockAssetRepo
                .Setup(x => x.GetByNameAsync(request.AssetName))
                .ReturnsAsync((Assets?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.GetAnalyticsData(request));

            Assert.Equal("Asset not found.", exception.Message);
        }

        [Fact]
        public async Task GetAnalyticsData_ShouldThrowException_WhenTimeRangeIsInvalid()
        {
            // Arrange
            var request = new AnalyticsRequestDto
            {
                AssetName = "Pump1",
                TagName = "Temperature",
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddHours(-1)
            };


            var Asset = new Assets("Pump1");

            _mockAssetRepo
                .Setup(x => x.GetByNameAsync(request.AssetName))
                .ReturnsAsync(Asset);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.GetAnalyticsData(request));

            Assert.Equal("Invalid time range.", exception.Message);
        }

        [Fact]
        public async Task GetAnalyticsData_ShouldReturnAnalyticsResponse_ForValidRequest()
        {
            // Arrange
            var request = new AnalyticsRequestDto
            {
                AssetName = "Pump1",
                TagName = "Temperature",
                StartTime = DateTime.UtcNow.AddHours(-5),
                EndTime = DateTime.UtcNow
            };

            var expectedResponse = new AnalyticsResponseDto
            {
                AsseName="Pump1",
                TagName= "Temperature",
                count=It.IsAny<int>(),
                Values = new List<ValueDto>()
            };

            var Asset = new Assets("Pump1");

            _mockAssetRepo
                .Setup(x => x.GetByNameAsync(request.AssetName))
                .ReturnsAsync(Asset);

            _mockRepo
                .Setup(x => x.GetAggregatedSensorData(
                    request.AssetName,
                    request.TagName,
                    request.StartTime,
                    request.EndTime,
                    It.IsAny<int>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _service.GetAnalyticsData(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResponse, result);

            _mockRepo.Verify(x =>
                x.GetAggregatedSensorData(
                    request.AssetName,
                    request.TagName,
                    request.StartTime,
                    request.EndTime,
                    It.IsAny<int>()),
                Times.Once);
        }xxxl

        [Fact]
        public async Task GetAnalyticsData_ShouldUseCorrectBucket_ForFiveHours()
        {
            // Arrange
            var request = new AnalyticsRequestDto
            {
                AssetName = "Pump1",
                TagName = "Temperature",
                StartTime = DateTime.UtcNow.AddHours(-5),
                EndTime = DateTime.UtcNow
            };

            var expectedResponse = new AnalyticsResponseDto
            {
                AsseName = "Pump1",
                TagName = "Temperature",
                count = It.IsAny<int>(),
                Values = new List<ValueDto>()
            };

            var Asset = new Assets("Pump1");

            _mockAssetRepo
                       .Setup(x => x.GetByNameAsync(request.AssetName))
                       .ReturnsAsync(Asset);

            _mockRepo
                .Setup(x => x.GetAggregatedSensorData(
                    request.AssetName,
                    request.TagName,
                    request.StartTime,
                    request.EndTime,
                    1))
                .ReturnsAsync(expectedResponse);

            // Act
            await _service.GetAnalyticsData(request);

            // Assert
            _mockRepo.Verify(x =>
                x.GetAggregatedSensorData(
                    request.AssetName,
                    request.TagName,
                    request.StartTime,
                    request.EndTime,
                    1),
                Times.Once);
        }

        [Fact]
        public async Task GetAnalyticsData_ShouldUseCorrectBucket_ForTenDays()
        {
            // Arrange
            var request = new AnalyticsRequestDto
            {
                AssetName = "Pump1",
                TagName = "Temperature",
                StartTime = DateTime.UtcNow.AddDays(-10),
                EndTime = DateTime.UtcNow
            };

            var expectedResponse = new AnalyticsResponseDto
            {
                AsseName = "Pump1",
                TagName = "Temperature",
                count = It.IsAny<int>(),
                Values = new List<ValueDto>()
            };

            var Asset = new Assets("Pump1");

            _mockAssetRepo
              .Setup(x => x.GetByNameAsync(request.AssetName))
              .ReturnsAsync(Asset);

            _mockRepo
                .Setup(x => x.GetAggregatedSensorData(
                    request.AssetName,
                    request.TagName,
                    request.StartTime,
                    request.EndTime,
                    30))
                .ReturnsAsync(expectedResponse);

            // Act
            await _service.GetAnalyticsData(request);

            // Assert
            _mockRepo.Verify(x =>
                x.GetAggregatedSensorData(
                    request.AssetName,
                    request.TagName,
                    request.StartTime,
                    request.EndTime,
                    30),
                Times.Once);
        }
    }
}
