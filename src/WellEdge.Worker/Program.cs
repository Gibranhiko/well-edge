using WellEdge.Application;
using WellEdge.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<SimulatorOptions>(
    builder.Configuration.GetSection("Simulator"));

builder.Services.AddSingleton<SensorSimulator>();
builder.Services.AddHostedService<SensorWorker>();

var host = builder.Build();
host.Run();
