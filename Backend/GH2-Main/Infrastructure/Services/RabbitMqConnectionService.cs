using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interface;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace Infrastructure.Services
{
    public class RabbitMqConnectionService : IRabbitMqConnectionService
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public RabbitMqConnectionService(IConfiguration configuration)
        {
            var factory = new ConnectionFactory
            {
                HostName ="localhost",
                UserName = "guest",
                Password = "guest",
                Port = 5672
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
        }

        public IConnection GetConnection() => _connection;


        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}
