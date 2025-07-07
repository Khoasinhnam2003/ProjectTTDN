using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using System.Text.Json;

namespace QuanLyNhanVien.Query.API.Middlewares
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
                        var (statusCode, errorResponse) = GetErrorResponse(exception, exceptionHandlerPathFeature?.Path);
                        context.Response.StatusCode = statusCode;
                        context.Response.ContentType = "application/json";

                        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
                    }
                });
            });
            return app;
        }

        private static (int StatusCode, object ErrorResponse) GetErrorResponse(Exception exception, string? path)
        {
            return exception switch
            {
                ValidationException ex => (
                    (int)HttpStatusCode.BadRequest,
                    new
                    {
                        Error = "Validation failed.",
                        Details = ex.Errors.Select(e => new { Message = e.ErrorMessage, Property = e.PropertyName }),
                        Path = path
                    }
                ),
                _ => (
                    (int)HttpStatusCode.InternalServerError,
                    new
                    {
                        Error = "An error occurred while processing the request.",
                        Details = exception.Message,
                        Path = path
                    }
                )
            };
        }
    }
}
