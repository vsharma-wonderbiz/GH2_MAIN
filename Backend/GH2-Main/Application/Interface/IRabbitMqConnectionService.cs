using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace Application.Interface
{
    public interface IRabbitMqConnectionService : IDisposable
    {
        IConnection GetConnection();
    }
}
