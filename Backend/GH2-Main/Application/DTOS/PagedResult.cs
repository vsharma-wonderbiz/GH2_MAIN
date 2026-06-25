using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace Application.DTOS
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; }
        public int totalcounts {  get; set; }
        public int PageNumber {  get; set; }
        public int PageSize { get; set; }
    }
}
