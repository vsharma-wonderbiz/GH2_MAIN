using System.Text;

using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Infrastructure.Services
{
    public class AlarmConsumer : BackgroundService
    {
        private IConnection _connection;
        private IModel _channel;

        public AlarmConsumer()
        {
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

            consumer.Received += (sender, eventArgs) =>
            {
                var body = eventArgs.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                Console.WriteLine($"Received: {message}");

                //Call your logic here
                ProcessMessage(message);

                // Acknowledge message
                _channel.BasicAck(eventArgs.DeliveryTag, false);
            };

            _channel.BasicConsume(
                queue: "alarm_queue",
                autoAck: false,
                consumer: consumer
            );

            return Task.CompletedTask;
        }

        private void ProcessMessage(string message)
        {
            // integrate your AlarmManager here
            Console.WriteLine($"Processing Alarm: {message}");
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}