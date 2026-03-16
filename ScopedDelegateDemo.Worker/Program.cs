using Azure.Messaging.ServiceBus;
using ScopedDelegateDemo.Application;
using ScopedDelegateDemo.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

string connectionString = builder.Configuration["ServiceBus:ConnectionString"]!;
string queueName = builder.Configuration["ServiceBus:QueueName"]!;

builder.Services.AddSingleton(sp => new ServiceBusClient(connectionString));

builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<ServiceBusClient>();
    var options = new ServiceBusProcessorOptions
    {
        AutoCompleteMessages = true,
        MaxConcurrentCalls = 5
    };
    return client.CreateProcessor(queueName, options);
});


builder.Services.AddScoped<OrderHandler>();
builder.Services.AddScoped<CustomerHandler>();


builder.Services.AddScoped<Func<string, Func<string, Task>>>(provider => messageType =>
{
    return messageType switch
    {
        "OrderCreated" => provider.GetRequiredService<OrderHandler>().HandleAsync,
        "CustomerUpdated" => provider.GetRequiredService<CustomerHandler>().HandleAsync,
        _ => throw new NotSupportedException($"Unknown message type: {messageType}")
    };
});

builder.Services.AddHostedService<AzureServiceBusWorker>();

var host = builder.Build();
host.Run();