using Microsoft.IdentityModel.Tokens; // Thêm namespace cho JWT
using QuanLyNhanVien.Query.API.DependencyInjection.Extensions; // For Swagger
using QuanLyNhanVien.Query.API.Middlewares; // For GlobalExceptionHandler
using System.Text;
using System.Text.Json.Serialization; // Thêm namespace này để dùng ReferenceHandler

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Thêm cấu hình để xử lý vòng lặp tham chiếu
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
        options.JsonSerializerOptions.MaxDepth = 64; // Tùy chọn tăng độ sâu nếu cần
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
        };
    });

// Thêm Authorization
builder.Services.AddAuthorization();

// Đăng ký các dịch vụ khác
builder.Services.AddSwagger(); // Add Swagger services
builder.Services.AddApplicationServices(builder.Configuration); // Register application services
builder.Services.AddHttpContextAccessor(); // Thêm để hỗ trợ kiểm tra vai trò trong handler (nếu cần)

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerWithUi(); // Use Swagger UI in development
}

app.UseCors("AllowReactApp");

// Add GlobalExceptionHandler middleware
app.UseGlobalExceptionHandler();

app.UseHttpsRedirection();
// Thêm UseAuthentication trước UseAuthorization
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();