using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;


var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<UserCreatedEventConsumer>();
    x.UsingAzureServiceBus((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("AzureServiceBus"));
        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
await host.RunAsync();