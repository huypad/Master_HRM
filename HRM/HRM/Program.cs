using HRM.Data;
using HRM.Helpers.Security;
using HRM.Repositories;
using HRM.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<HrmDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("HRMConnection")));

builder.Services.AddScoped<ISecurityService, MockSecurityService>();
builder.Services.AddScoped<INhanVienRepository, NhanVienRepository>();
builder.Services.AddScoped<INhanVienService, NhanVienService>();

// Healthcare DB & Benchmark Services
builder.Services.AddScoped<IRealSecurityService, RealSecurityService>();
builder.Services.AddScoped<PatientSearchService>();
builder.Services.AddScoped<PatientService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Healthcare & HRM API Benchmark",
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
