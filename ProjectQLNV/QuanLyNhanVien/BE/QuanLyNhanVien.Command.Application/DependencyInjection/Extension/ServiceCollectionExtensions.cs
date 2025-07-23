using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuanLyNhanVien.Command.Application.UseCases.Employees;
using QuanLyNhanVien.Command.Domain.Abstractions.Repositories;
using QuanLyNhanVien.Command.Persistence;
using QuanLyNhanVien.Command.Persistence.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QuanLyNhanVien.Command.Application.DependencyInjection.Extension
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration), "Configuration is null.");
            }

            // Register ApplicationDbContext
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("DefaultConnection string is not configured.");
                }
                options.UseSqlServer(connectionString);
                Console.WriteLine($"DbContext registered with connection string: {connectionString}");
            });

            // Register MediatR
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
                Assembly.GetExecutingAssembly(),
                Assembly.GetAssembly(typeof(CreateEmployeeCommandHandler))!));
            Console.WriteLine("MediatR registered successfully.");

            // Register IUnitOfWork
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            Console.WriteLine("IUnitOfWork registered successfully.");

            // Register GenericRepository
            services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
            Console.WriteLine("GenericRepository registered successfully.");

            return services;
        }
    }
}
