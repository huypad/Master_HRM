using HRM.Data;
using HRM.Repositories;
using HRM.Security;
using HRM.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("Logs/search-log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddDbContext<HrmDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("HRMConnection")));

// Đăng ký Repository và Service cho Nhân viên
builder.Services.AddScoped<INhanVienRepository, NhanVienRepository>();
builder.Services.AddScoped<INhanVienService, NhanVienService>();

// Đăng ký Service mã hóa & bảo mật
builder.Services.AddScoped<HRM.Services.ISecurityService, HRM.Services.SecurityService>();
builder.Services.AddScoped<HRM.Helpers.Security.ISecurityService, HRM.Helpers.Security.MockSecurityService>();
builder.Services.AddScoped<HRM.Security.IHybridSecurityService, HRM.Security.RealSecurityService>();

// Đăng ký Service quản lý và tra cứu Bệnh nhân (Patient)
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IPatientSearchService, PatientSearchService>();

builder.Services.AddScoped<HRM.Security.IHybridSecurityService, HRM.Security.RealSecurityService>();
builder.Services.AddScoped<IPatientSearchService, PatientSearchService>();
builder.Services.AddScoped<IPatientService, PatientService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "HRM Backend",
        Version = "v1"
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.RoutePrefix = "swagger"; 
    });
}

app.MapControllers();
app.Run();
