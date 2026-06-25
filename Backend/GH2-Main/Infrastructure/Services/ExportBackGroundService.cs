using System.Formats.Asn1;
using System.Globalization;
using System.Text;
using Application.DTOS;
using Application.Interface;
using Infrastructure.Implementation;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using CsvHelper;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Domain.Entities;
using System.Diagnostics;

namespace Infrastructure.Services
{
    public class ExportBackGroundService : BackgroundService
    {
        private readonly IRabbitMqConnectionService _rabbitMqService;
        private readonly IServiceScopeFactory _scopeFactory;
        private IModel _channel;

        public ExportBackGroundService(IRabbitMqConnectionService rabbitMqService,IServiceScopeFactory scopeFactory)
        {
            _rabbitMqService = rabbitMqService;
            _scopeFactory = scopeFactory;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var connection = _rabbitMqService.GetConnection();

            _channel = connection.CreateModel();

            // Process one message at a time
            _channel.BasicQos(
                prefetchSize: 0,
                prefetchCount: 1,
                global: false);

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (sender, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = JsonSerializer.Deserialize<ExportRequestPayload>(
                        Encoding.UTF8.GetString(body));

                    Console.WriteLine($"Received Message: {JsonSerializer.Serialize(message)}");

                    await GenerateCsvAsync(message);

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };


            _channel.BasicConsume(
                queue: "Export_Queue",
                autoAck: false,
                consumer: consumer);    

            return Task.CompletedTask;
        }
        private async Task GenerateCsvAsync(ExportRequestPayload request)
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IAnalyticsRepository>();

            var reader = await repo.GetFlattenedExportDataAsync(request);
            var fileName =
                   $"Export_{request.AssetName}_{request.ExportJobID}.csv";
            var filePath = Path.Combine("Exports", fileName);
            Directory.CreateDirectory("Exports");

            using var writer = new StreamWriter(filePath);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            csv.WriteField("TimeStamp");
            csv.WriteField("AssetName");   
            foreach (var tag in request.TagNames)
                csv.WriteField(tag);
            await csv.NextRecordAsync();

            while (await reader.ReadAsync())
            {
                csv.WriteField(reader["TimeStamp"]);
                csv.WriteField(request.AssetName);  
                foreach (var tag in request.TagNames)
                    csv.WriteField(reader[tag]);
                await csv.NextRecordAsync();
            }

            await writer.FlushAsync();
            Console.WriteLine($"CSV Generated : {filePath}");

            await UpdateTheRequest(filePath,request);
        }

        private async Task UpdateTheRequest(
    string filename,
    ExportRequestPayload request)
        {
            using var scope = _scopeFactory.CreateScope();

            var repo =
                scope.ServiceProvider
                    .GetRequiredService<IRepository<ExportRequest>>();

            var exportRequest = await repo.GetByIdAsync(request.ExportJobID);

            if (exportRequest == null)
            {
                throw new ArgumentException("No Request was found");
            }

            exportRequest.MarkAsCompletedWithFile(filename);

            await repo.SaveChangesAsync();
        }


        public override void Dispose()
        {
            _channel?.Close();
            _channel?.Dispose();

            base.Dispose();
        }
    }
}