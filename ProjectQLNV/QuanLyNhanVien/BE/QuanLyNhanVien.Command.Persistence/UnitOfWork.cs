using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using QuanLyNhanVien.Command.Domain.Abstractions.Repositories;
using QuanLyNhanVien.Command.Persistence.Repositories;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Command.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _dbContext;
        private Dictionary<Type, object> _repositories;
        private bool _disposed = false;
        private IDbContextTransaction _transaction;

        public UnitOfWork(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _repositories = new Dictionary<Type, object>();
        }

        public IGenericRepository<TEntity, TKey> Repository<TEntity, TKey>() where TEntity : class
        {
            var type = typeof(TEntity);
            if (_repositories.TryGetValue(type, out var repository))
            {
                return (IGenericRepository<TEntity, TKey>)repository;
            }

            try
            {
                var repositoryInstance = new GenericRepository<TEntity, TKey>(_dbContext);
                _repositories[type] = repositoryInstance;
                return repositoryInstance;
            }
            catch (Exception ex)
            {
                throw new Exception($"Không thể tạo repository cho {type.Name}: {ex.Message}", ex);
            }
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("Giao dịch đã được bắt đầu trước đó.");
            }
            _transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            return _transaction;
        }

        public void Commit()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("Không có giao dịch nào để commit.");
            }
            _transaction.Commit();
            _transaction.Dispose();
            _transaction = null;
        }

        public void Rollback()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("Không có giao dịch nào để rollback.");
            }
            _transaction.Rollback();
            _transaction.Dispose();
            _transaction = null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _dbContext.Dispose();
                    if (_transaction != null)
                    {
                        _transaction.Dispose();
                        _transaction = null;
                    }
                    foreach (var repository in _repositories.Values)
                    {
                        if (repository is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                    }
                    _repositories.Clear();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
