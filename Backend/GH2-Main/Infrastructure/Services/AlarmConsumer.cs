using System.Text;
using System.Text.Json;
using Application.DTOS;
using Application.Interface;
using Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Infrastructure.Services
{
    public class AlarmConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IRabbitMqConnectionService _rabbitMqService;

        private IModel _channel;
        private IModel _recommchannel;

        public AlarmConsumer(
            IServiceScopeFactory scopeFactory,
            IRabbitMqConnectionService rabbitMqService)
        {
            _scopeFactory = scopeFactory;
            _rabbitMqService = rabbitMqService;

            InitializeRabbitMq();
        }

        private void InitializeRabbitMq()
        {
            var connection = _rabbitMqService.GetConnection();

            _channel = connection.CreateModel();
            _recommchannel = connection.CreateModel();

            _channel.QueueDeclare(
                queue: "alarm_queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _recommchannel.QueueDeclare(
                queue: "recommendation_queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _channel.BasicQos(
                prefetchSize: 0,
                prefetchCount: 1,
                global: false);

            _recommchannel.BasicQos(
                prefetchSize: 0,
                prefetchCount: 1,
                global: false);
        }

        protected override Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (sender, eventArgs) =>
            {
                try
                {
                    var body = eventArgs.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    Console.WriteLine($"Alarm Message Received: {message}");

                    await ProcessMessage(message);

                    _channel.BasicAck(
                        eventArgs.DeliveryTag,
                        false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Alarm Processing Error: {ex.Message}");

                    _channel.BasicNack(
                        eventArgs.DeliveryTag,
                        false,
                        true);
                }
            };

            _channel.BasicConsume(
                queue: "alarm_queue",
                autoAck: false,
                consumer: consumer);

            var recommendationConsumer =
                new EventingBasicConsumer(_recommchannel);

            recommendationConsumer.Received += async (
                sender,
                eventArgs) =>
            {
                try
                {
                    var body = eventArgs.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    Console.WriteLine(
                        $"Recommendation Message Received: {message}");

                    await ProcessRecommendationMessage(message);

                    _recommchannel.BasicAck(
                        eventArgs.DeliveryTag,
                        false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Recommendation Processing Error: {ex.Message}");

                    _recommchannel.BasicNack(
                        eventArgs.DeliveryTag,
                        false,
                        true);
                }
            };

            _recommchannel.BasicConsume(
                queue: "recommendation_queue",
                autoAck: false,
                consumer: recommendationConsumer);

            return Task.CompletedTask;
        }

        private async Task ProcessMessage(string message)
        {
            using var scope = _scopeFactory.CreateScope();

            var repo =
                scope.ServiceProvider
                    .GetRequiredService<IAlarmRepositary>();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var alarm =
                JsonSerializer.Deserialize<AlarmEventDto>(
                    message,
                    options);

            if (alarm == null)
            {
                Console.WriteLine("Alarm deserialization failed.");
                return;
            }

            if (alarm.Event?.ToUpper() == "ALARM_TRIGGERED")
            {
                var entry = new AlarmInfo(
                    alarm.MappingId,
                    alarm.AssetName,
                    alarm.Signal,
                    (float)alarm.CurrentValue,
                    alarm.AlarmType);

                await repo.AddAsync(entry);
                await repo.SaveChangesAsync();

                Console.WriteLine("Alarm saved.");
            }
            else if (alarm.Event?.ToUpper() == "ALARM_CLEARED")
            {
                var activeAlarm =
                    await repo.GetActiveAlarm(
                        alarm.MappingId,
                        alarm.Signal);

                if (activeAlarm != null)
                {
                    activeAlarm.Resolve();

                    repo.Update(activeAlarm);

                    await repo.SaveChangesAsync();

                    Console.WriteLine("Alarm cleared.");
                }
            }
        }

        private async Task ProcessRecommendationMessage(
            string message)
        {
            using var scope = _scopeFactory.CreateScope();

            var repo =
                scope.ServiceProvider
                    .GetRequiredService<IRecommendationRepositary>();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var recommendation =
                JsonSerializer.Deserialize<RecommendationEventDto>(
                    message,
                    options);

            if (recommendation == null)
            {
                Console.WriteLine(
                    "Recommendation deserialization failed.");

                return;
            }

            if (recommendation.Event?.ToUpper() ==
                "RECOMMENDATION_TRIGGERED")
            {
                var recommendationInfo =
                    new RecommendationInfo(
                        recommendation.MappingId,
                        recommendation.AssetName,
                        recommendation.Signal,
                        recommendation.recommendation_type,
                        recommendation.CurrentVal,
                        recommendation.TriggerVal
                            ?? throw new ArgumentException(
                                nameof(recommendation.TriggerVal)),
                        recommendation.Message
                            ?? throw new ArgumentException(
                                nameof(recommendation.Message)));

                await repo.AddAsync(recommendationInfo);

                await repo.SaveChangesAsync();

                Console.WriteLine(
                    "Recommendation saved.");
            }
            else if (recommendation.Event?.ToUpper() ==
                     "RECOMMENDATION_CLEARED")
            {
                var activeRecommendation =
                    await repo.GetActiveRecommendation(
                        recommendation.MappingId,
                        recommendation.Signal);

                if (activeRecommendation != null)
                {
                    activeRecommendation.Resolve();

                    repo.Update(activeRecommendation);

                    await repo.SaveChangesAsync();

                    Console.WriteLine(
                        "Recommendation cleared.");
                }
            }
        }

        public override void Dispose()
        {
            _channel?.Close();
            _channel?.Dispose();

            _recommchannel?.Close();
            _recommchannel?.Dispose();

            base.Dispose();
        }
    }
}