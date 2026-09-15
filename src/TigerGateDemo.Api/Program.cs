using FluentValidation;
using FluentValidation.AspNetCore;
using Serilog;
using TigerGateDemo.Api.Dtos;
using TigerGateDemo.Api.Middleware;
using TigerGateDemo.Api.Services;
using TigerGateDemo.Core.Abstractions;
using TigerGateDemo.Core.Common;
using TigerGateDemo.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration).WriteTo.Console());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddFluentValidation(options =>
    options.RegisterValidatorsFromAssemblyContaining<CreateProductRequestValidator>());

builder.Services.AddDataAccess(builder.Configuration.GetConnectionString("Shop"));
builder.Services.AddSingleton<IPricingPolicy, TieredPricingPolicy>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddSingleton<CryptoService>();
builder.Services.AddScoped<ReportExportService>();
builder.Services.AddScoped<OrderService>();

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>())
          .AllowAnyHeader()
          .AllowAnyMethod()));

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapControllers();

app.Run();

public partial class Program;
