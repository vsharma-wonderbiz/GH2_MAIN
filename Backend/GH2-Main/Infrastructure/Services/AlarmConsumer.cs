using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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
        private IConnection _connection;
        private readonly IServiceScopeFactory _scopeFactory;
        private IModel _channel;
        //private readonly IRepository<AlarmInfo> _repo;

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

            _channel.QueueDeclare(
                queue: "alarm_queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            // Prevent overloading consumer
            _channel.BasicQos(0, 1, false);
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
                    Console.WriteLine("Deserialization failed ");
                    return;
                }

                Console.WriteLine($"Event: {alarm.Event}");

                //these saves the message comg from the queue inot the database
                if (alarm.Event?.ToUpper() == "ALARM_TRIGGERED")
                {
                    var entry = new AlarmInfo(
                        alarm.MappingId,
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


        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();   
            base.Dispose();
        }
    }
}