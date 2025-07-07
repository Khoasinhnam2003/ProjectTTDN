using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuanLyNhanVien.Query.Application.UseCases.Employees;
using QuanLyNhanVien.Query.Domain.Abstractions.Repositories;
using QuanLyNhanVien.Query.Persistence;
using QuanLyNhanVien.Query.Persistence.Repositories;
using System.Reflection;

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
            Assembly.GetAssembly(typeof(GetAllEmployeesQueryHandler)) ?? throw new InvalidOperationException("Query handler assembly not found")));
        Console.WriteLine("MediatR registered successfully.");

        // Register IUnitOfWork
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        Console.WriteLine("IUnitOfWork registered successfully.");

        // Register GenericRepository
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        Console.WriteLine("GenericRepository registered successfully.");

        return services;
    }
}