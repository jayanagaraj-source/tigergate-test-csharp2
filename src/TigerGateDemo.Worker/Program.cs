using Serilog;
using TigerGateDemo.Core.Abstractions;
using TigerGateDemo.Core.Common;
using TigerGateDemo.Data;
using TigerGateDemo.Worker;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger);

builder.Services.AddDataAccess(builder.Configuration.GetConnectionString("Shop"));
builder.Services.AddSingleton<IPricingPolicy, TieredPricingPolicy>();
builder.Services.AddSingleton<ReportArchiver>();
builder.Services.AddHostedService<StockReconciliationWorker>();

var host = builder.Build();
host.Run();
