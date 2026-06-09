using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Interface;
using Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Application.DTOS;
using System.Security.Claims;
using System.Threading.Channels;

namespace Infrastructure.Services
{
    public class AlarmConsumer : BackgroundService
    {
        private IConnection _connection;
        private readonly  IServiceScopeFactory _scopeFactory;
        private IModel _channel;
        private IModel _recommchannel;
        

        public AlarmConsumer(IServiceScopeFactory scopeFactory)
        {
            //_repo = repo;
            _scopeFactory = scopeFactory;
            InitializeRabbitMq();
        }

        private void InitializeRabbitMq()
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost", // change if needed
                UserName = "guest",
                Password = "guest"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _recommchannel = _connection.CreateModel();

            _channel.QueueDeclare(
                queue: "alarm_queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            _recommchannel.QueueDeclare(
                queue: "recommendation_queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
                );

            // Prevent overloading consumer
            _channel.BasicQos(0, 1, false);
            _recommchannel.BasicQos(0,1,false);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (sender, eventArgs) =>
            {
                var body = eventArgs.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                Console.WriteLine($"Received: {message}");

                
                await ProcessMessage(message);

                
                _channel.BasicAck(eventArgs.DeliveryTag, false);
                
            };

            _channel.BasicConsume(
                queue: "alarm_queue",
                autoAck: false,
                consumer: consumer
            );


            var recommendationconsumer = new EventingBasicConsumer(_recommchannel);

            recommendationconsumer.Received += async (sender, eventArgs) =>
            {
                var body = eventArgs.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                Console.WriteLine($"Received: {message}");


                await ProcessRecommendationMessage(message);


                _recommchannel.BasicAck(eventArgs.DeliveryTag, false);
            };

            _recommchannel.BasicConsume(
                queue: "recommendation_queue",
                autoAck: false,
                consumer: recommendationconsumer
            );

            

            return Task.CompletedTask;
        }

        private async Task ProcessMessage(string message)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IAlarmRepositary>();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var alarm = JsonSerializer.Deserialize<AlarmEventDto>(message, options);

                if (alarm == null)
                {
                    Console.WriteLine("Deserialization failed");
                    return;
                }

                Console.WriteLine($"Event: {alarm.Event}");

                //these saves the message comg from the queue inot the database
                if (alarm.Event?.ToUpper() == "ALARM_TRIGGERED")
                {
                    var entry = new AlarmInfo(
                        alarm.MappingId,
                        alarm.AssetName,
                        alarm.Signal,
                        (float)alarm.CurrentValue,
                        alarm.AlarmType
                    );

                    await repo.AddAsync(entry);
                    await repo.SaveChangesAsync();

                    Console.WriteLine("Saved to DB ");
                }

                //these updates the notification once that gets resolved 
                else if (alarm.Event?.ToUpper() == "ALARM_CLEARED")
                {
                    var activeAlarm = await repo.GetActiveAlarm(alarm.MappingId, alarm.Signal);

                    if (activeAlarm != null)
                    {
                        activeAlarm.Resolve();
                        repo.Update(activeAlarm);
                        await repo.SaveChangesAsync();

                        Console.WriteLine("Alarm cleared");
                    }
                    else
                    {
                        Console.WriteLine("No active alarm found ");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB Error: {ex.Message}");
            }
        }

        private async Task ProcessRecommendationMessage(string message)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IRecommendationRepositary>();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var recommendation =
                    JsonSerializer.Deserialize<RecommendationEventDto>(
                        message,
                        options
                    );

                if (recommendation == null)
                {
                    Console.WriteLine("Deserialization failed");
                    return;
                }

                Console.WriteLine($"Event: {recommendation.Event}");

                if(recommendation.Event?.ToUpper() == "RECOMMENDATION_TRIGGERED")
                {
                    var Recommendation = new RecommendationInfo(
                        recommendation.MappingId,
                        recommendation.AssetName,
                        recommendation.Signal,
                        recommendation.recommendation_type,
                        recommendation.CurrentVal,
                        recommendation.TriggerVal ?? throw new ArgumentException(nameof(recommendation.TriggerVal)),
                        recommendation.Message ?? throw new ArgumentException(nameof(recommendation.Message))
                        );

                      await repo.AddAsync(Recommendation);
                       await repo.SaveChangesAsync();
                }
                
                else if(recommendation.Event?.ToUpper()== "RECOMMENDATION_CLEARED")
                {
                    var activeRecommendation= await repo.GetActiveRecommendation(recommendation.MappingId,recommendation.Signal);

                    if(activeRecommendation != null)
                    {
                        activeRecommendation.Resolve();
                        repo.Update(activeRecommendation);
                        await repo.SaveChangesAsync();

                        Console.WriteLine("Alarm cleared");
                    }
                    else
                    {
                        Console.WriteLine("No active alarm found ");
                    }
                }

                    Console.WriteLine(
                        JsonSerializer.Serialize(
                            recommendation,
                            new JsonSerializerOptions
                            {
                                WriteIndented = true
                            }
                        )
                    );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }



        public override void Dispose()
        {
            _channel?.Close();
            _recommchannel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}