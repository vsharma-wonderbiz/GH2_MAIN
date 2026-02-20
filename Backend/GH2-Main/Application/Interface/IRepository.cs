using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interface
{
    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id);
        Task<T?> GetByNameAsync(string Name);
        Task<IEnumerable<T>> GetAllAsync();
        void Update(T entity);
        void Delete(T  entity);
        Task AddAsync(T entity);
        Task SaveChangesAsync();
    }
}
