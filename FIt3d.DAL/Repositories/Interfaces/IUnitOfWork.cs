using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;

namespace FIt3d.DAL.Repositories.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<TEntity> GetRepository<TEntity>() where TEntity : class;

        Task<TOperation> ProcessInTransactionAsync<TOperation>(Func<Task<TOperation>> operation);
        Task ProcessInTransactionAsync(Func<Task> operation);

        Task<IDbContextTransaction> BeginTransactionAsync();
        Task CommitTransactionAsync(IDbContextTransaction transaction);
        Task RollbackTransactionAsync(IDbContextTransaction transaction);

        int Commit();
        Task<int> CommitAsync();
        Task<int> SaveChangesAsync();
    }
}
