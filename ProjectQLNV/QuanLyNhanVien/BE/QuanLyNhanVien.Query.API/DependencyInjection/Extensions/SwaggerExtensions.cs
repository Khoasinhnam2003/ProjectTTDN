namespace QuanLyNhanVien.Query.API.DependencyInjection.Extensions
{
    public static class SwaggerExtensions
    {
        public static IServiceCollection AddSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "QuanLyNhanVien Query API",
                    Version = "v1",
                    Description = "API for querying employee data"
                });
            });
            return services;
        }

        public static IApplicationBuilder UseSwaggerWithUi(this IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "QuanLyNhanVien Query API V1");
                c.RoutePrefix = "swagger"; // Must match launchUrl
            });
            return app;
        }
    }
}
