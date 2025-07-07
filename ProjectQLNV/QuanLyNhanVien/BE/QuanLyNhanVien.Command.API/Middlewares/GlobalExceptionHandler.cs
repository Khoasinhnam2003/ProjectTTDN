using Microsoft.AspNetCore.Diagnostics;
using System.Net;

namespace QuanLyNhanVien.Command.API.Middleware
{
    public static class GlobalExceptionHandler
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                    var exception = exceptionHandlerPathFeature?.Error;

                    if (exception != null)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        await context.Response.WriteAsync($"An error occurred: {exception.Message}");
                    }
                });
            });
            return app;
        }
    }
}
