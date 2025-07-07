using FluentValidation;
using System.Net;
using System.Text.Json;

namespace QuanLyNhanVien.Command.API.Middleware
{
    public static class ValidationExceptionHandler
    {
        public static IApplicationBuilder UseValidationExceptionHandler(this IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                Console.WriteLine($"Request: {context.Request.Path}");

                try
                {
                    await next();
                }
                catch (ValidationException ex)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    context.Response.ContentType = "application/json";

                    var errorResponse = new
                    {
                        Error = "Validation failed.",
                        Details = ex.Errors.Select(e => new { Message = e.ErrorMessage, Property = e.PropertyName })
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
                }
            });
            return app;
        }
    }
}
