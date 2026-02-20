using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interface;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Implementation
{
    public class Repository<T> : IRepository<T> where T :class 
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<T> _dbset;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _dbset = _context.Set<T>();
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            return await _dbset.FindAsync(id);

        }
        public async Task<T?> GetByNameAsync(string Name)
        {
            return await _dbset.FindAsync(Name);
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbset.ToListAsync();
        }

        public async Task AddAsync(T entity)
        {
            await _dbset.AddAsync(entity);
        }

        public void Update(T entity)
        {
            _dbset.Update(entity);
        }

        public void Delete(T entity)
        {
            _dbset.Remove(entity);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }


    }
}
