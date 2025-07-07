using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using QuanLyNhanVien.Command.API.DependencyInjection.Extensions;
using QuanLyNhanVien.Command.API.DependencyInjection.Options;
using QuanLyNhanVien.Command.API.Middleware;
using QuanLyNhanVien.Command.Application.DependencyInjection.Extension;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
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
            .WithOrigins("http://localhost:3000") // Cho phép origin của frontend
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()); // Nếu dùng cookie hoặc token
});



// Thêm Authentication và Authorization với JWT
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
            // Loại bỏ TokenDecryptionKey vì không cần mã hóa token
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

app.UseCors("AllowReactApp"); // Áp dụng CORS trước khi xử lý request

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseGlobalExceptionHandler();
app.UseValidationExceptionHandler();

app.MapControllers();

app.Run();