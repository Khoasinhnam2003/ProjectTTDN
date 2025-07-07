using Microsoft.OpenApi.Models;
using System.Reflection;

namespace QuanLyNhanVien.Command.API.DependencyInjection.Extensions
{
    public static class SwaggerExtensions
    {
        public static IServiceCollection AddCustomSwagger(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "QuanLyNhanVien.Command.API",
                    Version = "v1"
                });
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                    Console.WriteLine($"XML comments loaded from {xmlPath}");
                }
                else
                {
                    Console.WriteLine($"XML file not found at {xmlPath}");
                }
            });
            return services;
        }
    }
}
