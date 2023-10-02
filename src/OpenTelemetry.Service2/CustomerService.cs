namespace OpenTelemetry.Service2;

public class CustomerService : ICustomerService
{
    private readonly ILogger<ICustomerService> _logger;

    public CustomerService(ILogger<ICustomerService> logger)
    {
        _logger = logger;
    }
    
    public async Task<Customer> GetCustomer(int customerId, CancellationToken token = default)
    {
        using (_logger.BeginScope(new Dictionary<string,object>()
               {
                   ["CustomerId"] = customerId
               }))
        {
            if(customerId%2 == 0)
            {
                _logger.LogError("Customer {CustomerId} not found", customerId);
                throw new CustomerNotFoundException(customerId);
            }

            if (customerId % 5 == 0)
            {
                await Task.Delay(1000, token);
                _logger.LogWarning("Customer {CustomerId} is taking a long time to retrieve", customerId);
            }

            return new Customer(customerId, "Customer " + customerId, $"customer{customerId}@cloudtown.io");
        }
    }
}


public interface ICustomerService
{
    public Task<Customer> GetCustomer(int customerId, CancellationToken token = default);
}