using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Application.Interface;

namespace Infrastructure.Implementation
{
    public class RabbitMqServices : IRabbitMqServices
    {
        private readonly IRabbitMqConnectionService _rabbitMqConnService;

        public RabbitMqServices(
            IRabbitMqConnectionService rabbitMqConnService)
        {
            _rabbitMqConnService = rabbitMqConnService;
        }

        public async Task PublishAsync<T>(
            string queueName,
            T message)
        {
            var connection = _rabbitMqConnService.GetConnection();

            using var channel = connection.CreateModel();

            channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var body = Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(message));

            channel.BasicPublish(
                 exchange: "",
                 routingKey: queueName,
                 mandatory: false,
                 basicProperties: null,
                 body: body
                );

        }
    }
}
