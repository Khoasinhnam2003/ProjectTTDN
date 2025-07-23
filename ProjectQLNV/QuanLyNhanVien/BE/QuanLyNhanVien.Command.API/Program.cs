using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using QuanLyNhanVien.Command.API.DependencyInjection.Extensions;
using QuanLyNhanVien.Command.API.DependencyInjection.Options;
using QuanLyNhanVien.Command.API.Middleware;
using QuanLyNhanVien.Command.Application.DependencyInjection.Extension;
using Serilog;
using Serilog.Sinks.MSSqlServer;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình Serilog
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
        .WriteTo.MSSqlServer(
            connectionString: context.Configuration.GetConnectionString("DefaultConnection"),
            sinkOptions: new MSSqlServerSinkOptions
            {
                TableName = "Logs",
                AutoCreateSqlTable = true
            });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
        options.JsonSerializerOptions.MaxDepth = 64;
    });

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});
builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

// Register custom services with proper logging
var logger = builder.Services.BuildServiceProvider().GetService<ILogger<Program>>();
try
{
    builder.Services.AddApplicationServices(builder.Configuration);
    logger?.LogInformation("Application services registered successfully.");
}
catch (Exception ex)
{
    logger?.LogError(ex, "Error registering application services");
    throw;
}

try
{
    builder.Services.AddCustomSwagger();
    logger?.LogInformation("Swagger services registered successfully.");
}
catch (Exception ex)
{
    logger?.LogError(ex, "Error registering Swagger services");
    throw;
}

builder.Services.AddTransient<IConfigureOptions<SwaggerUIOptions>, SwaggerConfigureOptions>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        builder => builder
            .WithOrigins("http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

// Thêm Authentication và Authorization với JWT
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
        }
        options.RoutePrefix = "swagger";
        options.OAuthClientId("your-client-id");
        options.OAuthClientSecret("your-client-secret");
    });
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseHsts();
}

app.UseCors("AllowReactApp");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseGlobalExceptionHandler();
app.UseValidationExceptionHandler();
app.MapControllers();

try
{
    Log.Information("Application is starting...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    Log.CloseAndFlush();
}