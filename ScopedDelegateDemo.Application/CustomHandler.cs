using Microsoft.Extensions.Logging;

namespace ScopedDelegateDemo.Application;

public class CustomerHandler
{
    private readonly ILogger<CustomerHandler> _logger;

    public CustomerHandler(ILogger<CustomerHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(string payload)
    {
        _logger.LogInformation("[CUSTOMER] Processing customer update: {Payload}", payload);
        await Task.Delay(500); // Simulate database work
    }
}