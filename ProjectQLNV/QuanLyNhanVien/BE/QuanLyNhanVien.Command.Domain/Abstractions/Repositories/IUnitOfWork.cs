using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Command.Domain.Abstractions.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<TEntity, TKey> Repository<TEntity, TKey>() where TEntity : class;
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
        void Commit();
        void Rollback();
    }
}
