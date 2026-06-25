using System.ComponentModel.DataAnnotations;
using Application.DTOS;
using Application.Interface;
using Domain.Entities;

namespace Application.Services
{
    public class ExportService : IExportService
    {
        private readonly IAssetRepository _assetRepo;
        private readonly IMappingRepositary _mapRepo;
        private readonly IRabbitMqServices _rabbitMqServices;
        private readonly IExportRequestRepository _exportRequest;
        private readonly IRepository<ExportRequestTags> _exportRequestTags;

        public ExportService(
            IAssetRepository assetRepo,
            IMappingRepositary mapRepo,
            IRabbitMqServices rabbitMqService,
            IExportRequestRepository exportRequest,
            IRepository<ExportRequestTags> exportRequestTags)
        {
            _assetRepo = assetRepo;
            _mapRepo = mapRepo;
            _rabbitMqServices = rabbitMqService;
            _exportRequest = exportRequest;
            _exportRequestTags = exportRequestTags;
        }

        public async Task PublishExportRequest(
            ExportRequestDto request,
            string user)
        {
            try
            {
                await ValidateRequest(request);

                var requestId = await SaveExportRequest(
                    request,
                    user);

                await SaveExportTags(
                    requestId,
                    request.TagNames);

                await PublishMessage(requestId, request);
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    "Failed to queue export request. Please try again later.",
                    ex);
            }
        }

        public async Task<DownloadFileDto> DownloadExportFile(int ExportJobId)
        {
            var exportRequest =
        await _exportRequest.GetByIdAsync(ExportJobId);

            if (exportRequest == null)
                throw new Exception("Export request not found");

            if (!File.Exists(exportRequest.FilePath))
                throw new Exception("File not found");

            var stream = new FileStream(
                exportRequest.FilePath,
                FileMode.Open,
                FileAccess.Read);

            return new DownloadFileDto
            {
                FileStream = stream,
                FileName = exportRequest.FilePath,
                ContentType = "text/csv"
            };
        }

     public async Task<PagedResult<ExportRequest>> GetPagedExports(
    int pageNumber,
    int pageSize)
        {
            return await _exportRequest.GetPagedExport(
                pageNumber,
                pageSize);
        }

        private async Task ValidateRequest(
            ExportRequestDto request)
        {
            ValidateBasicRequest(request);

            await ValidateAsset(request.AssetNamme);

            await ValidateTagsMappedToAsset(
                request.AssetNamme,
                request.TagNames);
        }

        private void ValidateBasicRequest(
            ExportRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.AssetNamme))
                throw new ValidationException(
                    "Asset name is required.");

            if (request.TagNames == null ||
                !request.TagNames.Any())
                throw new ValidationException(
                    "At least one tag must be selected.");

            if (request.TagNames
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() != request.TagNames.Count)
                throw new ValidationException(
                    "Duplicate tags are not allowed.");

            if (request.StartTime >= request.EndTime)
                throw new ValidationException(
                    "Start time must be before end time.");

            if ((request.EndTime - request.StartTime)
                .TotalDays > 90)
                throw new ValidationException(
                    "Maximum export range is 90 days.");
        }

        private async Task ValidateAsset(
            string assetName)
        {
            var asset =
                await _assetRepo.GetByNameAsync(assetName);

            if (asset == null)
                throw new ValidationException(
                    $"Stack '{assetName}' does not exist.");
        }

        private async Task ValidateTagsMappedToAsset(
            string assetName,
            List<string> requestedTags)
        {
            var mappedTags =
                await _mapRepo.GetAllTagsMappedOnStack(assetName);

            var invalidTags = requestedTags
                .Except(
                    mappedTags,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (invalidTags.Any())
            {
                throw new ValidationException(
                    $"Invalid tags: {string.Join(", ", invalidTags)}");
            }
        }

        private async Task<int> SaveExportRequest(
            ExportRequestDto request,
            string user)
        {
            var exportLog = new ExportRequest(
                request.AssetNamme,
                request.StartTime,
                request.EndTime,
                user);

            await _exportRequest.AddAsync(exportLog);
            await _exportRequest.SaveChangesAsync();

            return exportLog.ExportRequestId;
        }

        private async Task SaveExportTags(
            int requestId,
            List<string> tags)
        {
            var exportTags = tags
                .Select(tag => new ExportRequestTags(
                    requestId,
                    tag))
                .ToList();

            await _exportRequestTags.AddRangeAsync(exportTags);
            await _exportRequestTags.SaveChangesAsync();
        }

        private async Task PublishMessage(int requestid,
            ExportRequestDto request)
        {

            var Request = new ExportRequestPayload
            {
                ExportJobID = requestid,
                AssetName = request.AssetNamme,
                TagNames = request.TagNames.ToList(),
                StartTime = request.StartTime,
                EndTime = request.EndTime
            };

            await _rabbitMqServices.PublishAsync(
                "Export_Queue",
                Request);
        }
    }
}