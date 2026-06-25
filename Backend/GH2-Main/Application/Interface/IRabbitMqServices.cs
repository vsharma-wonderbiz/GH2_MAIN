using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interface
{
    public interface IRabbitMqServices
    {
        Task PublishAsync<T>(string queueName, T message);
    }
}
