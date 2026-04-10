using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ScopedDelegateDemo.Infrastructure;

public class AzureServiceBusWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ServiceBusProcessor _processor;
    private readonly ILogger<AzureServiceBusWorker> _logger;

    public AzureServiceBusWorker(
        IServiceScopeFactory scopeFactory,
        ServiceBusProcessor processor,
        ILogger<AzureServiceBusWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _processor = processor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor.ProcessMessageAsync += MessageHandler;
        _processor.ProcessErrorAsync += ErrorHandler;

        _logger.LogInformation("Starting Azure Service Bus Processor...");
        await _processor.StartProcessingAsync(stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("Worker shutting down gracefully.");
        }
    }

    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        using (IServiceScope scope = _scopeFactory.CreateScope())
        {
            try
            {
                if (!args.Message.ApplicationProperties.TryGetValue("MessageType", out var messageTypeObj))
                {
                    _logger.LogWarning("Message {MessageId} missing 'MessageType'. Ignoring.", args.Message.MessageId);
                    return;
                }

                string messageType = messageTypeObj.ToString()!;
                string payload = args.Message.Body.ToString();

                var handlerFactory = scope.ServiceProvider.GetRequiredService<Func<string, Func<string, Task>>>();
                var handleMessage = handlerFactory(messageType);

                await handleMessage(payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process message {MessageId}", args.Message.MessageId);
                throw; 
            }
        }
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Service Bus Error in {ErrorSource}", args.ErrorSource);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        await _processor.StopProcessingAsync(stoppingToken);
        _processor.ProcessMessageAsync -= MessageHandler;
        _processor.ProcessErrorAsync -= ErrorHandler;
        await base.StopAsync(stoppingToken);
    }
}