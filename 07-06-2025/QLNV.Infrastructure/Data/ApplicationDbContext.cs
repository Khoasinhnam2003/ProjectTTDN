using Microsoft.EntityFrameworkCore;
using QLNV.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLNV.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
            ChangeTracker.LazyLoadingEnabled = false;
        }

        protected ApplicationDbContext()
        {
        }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Job> Jobs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Salary)
                      .HasColumnType("decimal(18,2)"); // Chỉ định kiểu cột decimal với 18 chữ số, 2 chữ số thập phân
            });

            modelBuilder.Entity<Job>(entity =>
            {
                entity.HasKey(j => j.Id);
                entity.Property(j => j.Id).ValueGeneratedOnAdd();
                entity.Property(j => j.Title).HasMaxLength(100).IsRequired();
                entity.Property(j => j.Description).HasMaxLength(500);

                entity.HasOne(j => j.Employee)
                      .WithMany(e => e.Jobs)
                      .HasForeignKey(j => j.EmployeeId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
