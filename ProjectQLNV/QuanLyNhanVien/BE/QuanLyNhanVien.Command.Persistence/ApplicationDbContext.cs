using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using QuanLyNhanVien.Command.Domain.Entities;

namespace QuanLyNhanVien.Command.Persistence;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<Contract> Contracts { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Position> Positions { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SalaryHistory> SalaryHistories { get; set; }

    public virtual DbSet<Skill> Skills { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<UserToken> UserTokens { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=LAPTOP-K2ONPAFR;Database=EmployeeManagementJWT1;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.AttendanceId).HasName("PK__Attendan__8B69261CD3ECD77D");

            entity.ToTable("Attendance");

            entity.Property(e => e.CheckInTime).HasColumnType("datetime");
            entity.Property(e => e.CheckOutTime).HasColumnType("datetime");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Notes).HasMaxLength(200);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Employee).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Attendanc__Emplo__4AB81AF0");
        });

        modelBuilder.Entity<Contract>(entity =>
        {
            entity.HasKey(e => e.ContractId).HasName("PK__Contract__C90D346975AF5492");

            entity.ToTable("Contract");

            entity.Property(e => e.ContractType).HasMaxLength(50);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.Salary).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Employee).WithMany(p => p.Contracts)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Contract__Employ__4D94879B");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DepartmentId).HasName("PK__Departme__B2079BEDA039A4C3");

            entity.ToTable("Department");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DepartmentName).HasMaxLength(100);
            entity.Property(e => e.Location).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Manager).WithMany(p => p.Departments)
                .HasForeignKey(d => d.ManagerId)
                .HasConstraintName("FK_Department_Employee");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("PK__Employee__7AD04F116CA007BA");

            entity.ToTable("Employee");

            entity.HasIndex(e => e.Email, "UQ__Employee__A9D105345B413D0E").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DateOfBirth).HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.HireDate).HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Department).WithMany(p => p.Employees)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("FK__Employee__Depart__3C69FB99");

            entity.HasOne(d => d.Position).WithMany(p => p.Employees)
                .HasForeignKey(d => d.PositionId)
                .HasConstraintName("FK__Employee__Positi__3D5E1FD2");
        });

        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasKey(e => e.PositionId).HasName("PK__Position__60BB9A79F161C840");

            entity.ToTable("Position");

            entity.Property(e => e.BaseSalary).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.PositionName).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Role__8AFACE1A90A2FB9E");

            entity.ToTable("Role");

            entity.HasIndex(e => e.RoleName, "UQ__Role__8A2B61602C80C98B").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.RoleName).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<SalaryHistory>(entity =>
        {
            entity.HasKey(e => e.SalaryHistoryId).HasName("PK__SalaryHi__25D843F6ABBE6271");

            entity.ToTable("SalaryHistory");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EffectiveDate).HasColumnType("datetime");
            entity.Property(e => e.Salary).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Employee).WithMany(p => p.SalaryHistories)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SalaryHis__Emplo__5070F446");
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(e => e.SkillId).HasName("PK__Skill__DFA091874E9FCE3C");

            entity.ToTable("Skill");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.ProficiencyLevel).HasMaxLength(50);
            entity.Property(e => e.SkillName).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Employee).WithMany(p => p.Skills)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Skill__EmployeeI__534D60F1");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__User__1788CC4C4DD8E15C");

            entity.ToTable("User");

            entity.HasIndex(e => e.Username, "UQ__User__536C85E41CCE4EEB").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PasswordHash).HasMaxLength(256);
            entity.Property(e => e.PasswordSalt).HasMaxLength(256);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            entity.Property(e => e.Username).HasMaxLength(50);

            entity.HasOne(d => d.Employee).WithMany(p => p.Users)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__User__EmployeeId__440B1D61");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId }).HasName("PK__UserRole__AF2760AD8C4EBD06");

            entity.ToTable("UserRole");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserRole__RoleId__47DBAE45");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserRole__UserId__46E78A0C");
        });

        modelBuilder.Entity<UserToken>(entity =>
        {
            entity.HasKey(e => e.TokenId).HasName("PK__UserToke__658FEEEADE5D4E7F");

            entity.ToTable("UserToken");

            entity.HasIndex(e => e.AccessToken, "UQ__UserToke__A4E40AB2A3703461").IsUnique();

            entity.HasIndex(e => e.RefreshToken, "UQ__UserToke__DEA298DA4038AAC7").IsUnique();

            entity.Property(e => e.AccessToken).HasMaxLength(500);
            entity.Property(e => e.AccessTokenExpiryDate).HasColumnType("datetime");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.RefreshToken).HasMaxLength(500);
            entity.Property(e => e.RefreshTokenExpiryDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.User).WithMany(p => p.UserTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserToken__UserI__5812160E");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
