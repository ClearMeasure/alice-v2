using Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<WorkerEndpoint>();
var host = builder.Build();
host.Run();
