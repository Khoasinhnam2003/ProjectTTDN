using Microsoft.EntityFrameworkCore;
using QLNV.Models.Entities;

namespace QLNV.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        protected ApplicationDbContext()
        {
        }
        public DbSet<Employee> Employees { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.Id); // Định nghĩa Id là khóa chính
                entity.Property(e => e.Id).ValueGeneratedOnAdd(); // Tự động tăng giá trị khi thêm mới

                // Cấu hình thêm nếu cần, ví dụ: độ dài tối đa của các trường
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Phone).HasMaxLength(20);
            });
        }
    }
}
