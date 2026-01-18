using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using FIt3d.DAL.Data;
using FIt3d.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace FIt3d.DAL.Repositories.Implements
{
    public class UnitOfWork : IUnitOfWork
    {
        public Fit3dDbContext Context { get; }
        private Dictionary<Type, object>? _repositories;

        public UnitOfWork(Fit3dDbContext context)
        {
            Context = context;
        }

        #region Repository Management
        public IGenericRepository<TEntity> GetRepository<TEntity>() where TEntity : class
        {
            _repositories ??= new Dictionary<Type, object>();
            if (_repositories.TryGetValue(typeof(TEntity), out object? repository))
            {
                return (IGenericRepository<TEntity>)repository;
            }

            repository = new GenericRepository<TEntity>(Context);
            _repositories.Add(typeof(TEntity), repository);
            return (IGenericRepository<TEntity>)repository;
        }
        #endregion

        #region Packed Transaction Management
        public async Task<TOperation> ProcessInTransactionAsync<TOperation>(Func<Task<TOperation>> operation)
        {
            var executionStrategy = Context.Database.CreateExecutionStrategy();
            return await executionStrategy.ExecuteAsync(
                state: operation,
                operation: async (context, state, cancellationToken) =>
                {
                    await using var transaction = await Context.Database.BeginTransactionAsync(cancellationToken);
                    try
                    {
                        var result = await state();
                        await Context.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        return result;
                    }
                    catch
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        throw;
                    }
                },
                verifySucceeded: null);
        }

        public async Task ProcessInTransactionAsync(Func<Task> operation)
        {
            await ProcessInTransactionAsync(async () =>
            {
                await operation();
                return true;
            });
        }
        #endregion

        #region Transaction Management
        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await Context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync(IDbContextTransaction transaction)
        {
            await transaction.CommitAsync();
        }

        public async Task RollbackTransactionAsync(IDbContextTransaction transaction)
        {
            await transaction.RollbackAsync();
        }
        #endregion

        #region IDisposable Implementation
        public void Dispose()
        {
            Context?.Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Save Changes
        public int Commit()
        {
            TrackChanges();
            return Context.SaveChanges();
        }

        public async Task<int> CommitAsync()
        {
            TrackChanges();
            return await Context.SaveChangesAsync();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await CommitAsync();
        }
        #endregion

        #region Validation
        private void TrackChanges()
        {
            var validationErrors = Context.ChangeTracker.Entries<IValidatableObject>()
                .SelectMany(e => e.Entity.Validate(null!))
                .Where(e => e != ValidationResult.Success)
                .ToArray();
            if (validationErrors.Any())
            {
                var exceptionMessage = string.Join(Environment.NewLine,
                    validationErrors.Select(error => $"Properties {error.MemberNames} Error: {error.ErrorMessage}"));
                throw new ValidationException(exceptionMessage);
            }
        }
        #endregion
    }
}
