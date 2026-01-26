using API.Middlewares;
using BLL.Interfaces;
using BLL.Services;
using DAL;
using DAL.Interfaces;
using DAL.Repositories;
using DTOs.Constants;
using DTOs.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using API;

var builder = WebApplication.CreateBuilder(args);

// ==================================================================
// 1. CẤU HÌNH SERVICES (DEPENDENCY INJECTION)
// ==================================================================

// --- A. Database Context ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (builder.Configuration.GetValue<bool>("UseInMemoryDatabase"))
    {
        options.UseInMemoryDatabase("InMemoryDbForTesting");
    }
    else
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
});

// --- B. Đăng ký Repositories ---
// Đăng ký Repository Generic trước
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Đăng ký các Repository cụ thể
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IAssignmentRepository, AssignmentRepository>();
builder.Services.AddScoped<ILabelRepository, LabelRepository>();

// --- C. Đăng ký Services (Business Logic) ---
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<ILabelService, LabelService>();

// --- D. Cấu hình CORS (Cho phép Frontend truy cập) ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        b => b.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// --- E. Cấu hình Authentication (JWT) ---
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"] ?? "SecretKeyMustBeLongerThan16Characters");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        ClockSkew = TimeSpan.Zero // Không cho phép lệch giờ (Token hết hạn là chặn ngay)
    };
});

// --- F. Cấu hình Controllers & Swagger ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Data Labeling API", Version = "v1" });

    // Cấu hình nút "Authorize" (Ổ khóa) trên Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập token vào ô bên dưới theo định dạng: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// ==================================================================
// 2. CẤU HÌNH HTTP REQUEST PIPELINE (MIDDLEWARES)
// ==================================================================

// --- QUAN TRỌNG: Đăng ký Middleware xử lý lỗi toàn cục ---
// Nó phải nằm TRƯỚC các middleware khác để bắt lỗi từ chúng
app.UseMiddleware<ExceptionMiddleware>();

// --- Data Seeder (Chạy khi khởi động app) ---
// Tự động tạo Manager và Sample Data nếu chưa có
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Gọi hàm SeedData static từ file DataSeeder.cs
        await DataSeeder.SeedData(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi xảy ra khi chạy Data Seeder.");
    }
}

// --- Môi trường Development ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// --- Kích hoạt CORS ---
app.UseCors("AllowAll");

// --- Kích hoạt Authentication & Authorization ---
app.UseAuthentication(); // Phải đứng trước Authorization
app.UseAuthorization();

app.MapControllers();

app.Run();

namespace API
{
    public partial class Program { }
}
