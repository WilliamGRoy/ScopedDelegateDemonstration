using Microsoft.Extensions.Logging;

namespace ScopedDelegateDemo.Application;

public class OrderHandler
{
    private readonly ILogger<OrderHandler> _logger;

    public OrderHandler(ILogger<OrderHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(string payload)
    {
        _logger.LogInformation("[ORDER] Processing order payload: {Payload}", payload);
        await Task.Delay(500); 
    }
}