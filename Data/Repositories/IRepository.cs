using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HR_Products.Models.Entitites;

namespace HR_Products.Data.Repositories
{
    public interface IRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> GetByIdAsync(int id);
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(int id);
    }
}