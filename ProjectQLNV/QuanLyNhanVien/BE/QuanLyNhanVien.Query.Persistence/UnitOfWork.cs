using Microsoft.EntityFrameworkCore.Storage;
using QuanLyNhanVien.Query.Domain.Abstractions.Repositories;
using QuanLyNhanVien.Query.Persistence.Repositories;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Query.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Hashtable _repositories;
        private bool _disposed = false;
        private IDbContextTransaction _transaction;

        public UnitOfWork(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _repositories = new Hashtable();
        }
        public IGenericRepository<T> Repository<T>() where T : class
        {
            var type = typeof(T).Name;
            if (!_repositories.ContainsKey(type))
            {
                try
                {
                    var repositoryInstance = new GenericRepository<T>(_dbContext);
                    _repositories.Add(type, repositoryInstance);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Không thể tạo repository cho {type}: {ex.Message}");
                }
            }

            return (IGenericRepository<T>)_repositories[type]!;
        }
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
        public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            _transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            return _transaction;
        }
        public async Task CommitTransactionAsync()
        {
            if (_transaction == null)
                throw new InvalidOperationException("No active transaction to commit.");

            try
            {
                await SaveChangesAsync();
                _transaction.Commit();
            }
            finally
            {
                _transaction.Dispose();
                _transaction = null;
            }
        }
        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction == null)
                throw new InvalidOperationException("No active transaction to commit.");

            try
            {
                await SaveChangesAsync(cancellationToken);
                _transaction.Commit();
            }
            finally
            {
                _transaction.Dispose();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction == null)
                throw new InvalidOperationException("No active transaction to rollback.");

            try
            {
                _transaction.Rollback();
            }
            finally
            {
                _transaction.Dispose();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _dbContext.Dispose();
                    _transaction?.Dispose();
                    _transaction = null;
                }
                _disposed = true;
            }
        }

        public IGenericRepository<TEntity, TKey> Repository<TEntity, TKey>() where TEntity : class
        {
            var type = typeof(TEntity).Name;
            if (!_repositories.ContainsKey(type))
            {
                try
                {
                    var repositoryInstance = new GenericRepository<TEntity, TKey>(_dbContext);
                    _repositories.Add(type, repositoryInstance);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Không thể tạo repository cho {type}: {ex.Message}");
                }
            }

            return (IGenericRepository<TEntity, TKey>)_repositories[type]!;
        }

        ~UnitOfWork()
        {
            Dispose(false);
        }
    }


}
