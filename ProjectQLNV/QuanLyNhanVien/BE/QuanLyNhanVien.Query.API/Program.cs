using Microsoft.IdentityModel.Tokens;
using QuanLyNhanVien.Query.API.DependencyInjection.Extensions;
using QuanLyNhanVien.Query.API.Middlewares;
using Serilog;
using Serilog.Sinks.MSSqlServer;
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
        .WriteTo.File("logs/query-log-.txt", rollingInterval: RollingInterval.Day)
        .WriteTo.MSSqlServer(
            connectionString: context.Configuration.GetConnectionString("DefaultConnection"),
            sinkOptions: new MSSqlServerSinkOptions
            {
                TableName = "QueryLogs", // Sử dụng bảng riêng cho Query để tránh xung đột với Command
                AutoCreateSqlTable = true
            });
});

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
        options.JsonSerializerOptions.MaxDepth = 64;
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        builder => builder
            .WithOrigins("http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

// Thêm Authentication với scheme mặc định "Bearer"
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

// Thêm Authorization
builder.Services.AddAuthorization();

// Đăng ký các dịch vụ khác
builder.Services.AddSwagger();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerWithUi();
}

app.UseCors("AllowReactApp");

// Add GlobalExceptionHandler middleware
app.UseGlobalExceptionHandler();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

try
{
    Log.Information("Query application is starting...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Query application failed to start");
}
finally
{
    Log.CloseAndFlush();
}