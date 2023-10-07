using System.Text.Json;
using Microsoft.Extensions.Options;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<MicroservicesUrlOptions>()
    .Configure<IConfiguration>(
        (options, configuration) => 
            configuration.GetSection("MicroservicesUrl").Bind(options));

builder.Services.AddHttpClient();
builder.Services.AddHealthChecks();
builder.Services.AddLogging();
builder.Logging.AddJsonConsole(options => options.JsonWriterOptions = new JsonWriterOptions { Indented = true });
builder.Logging.AddOpenTelemetry(options =>
    options.AddOtlpExporter((exporterOptions, processorOptions) => { }));
builder.Services.AddOpenTelemetry()
    .WithMetrics(b => b
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter())
    .WithTracing(b => b
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter()
        );

var app = builder.Build();

app.MapGet("customer/{customerId:int}/Order/{orderId:int}", 
    async (int customerId, int orderId, IOptions<MicroservicesUrlOptions> options,
IHttpClientFactory httpClientFactory, ILogger logger, CancellationToken token) =>
{
    
    
    using (logger.BeginScope("Processing Order {OrderId} for customer {CustomerId}", orderId, customerId))
    {
        using (var httpClient = httpClientFactory.CreateClient())
        {
            using (logger.BeginScope("Retrieving customer information {CustomerId}", customerId))
            {
                var response = await httpClient.GetAsync($"{options.Value.CustomerUrl}/{customerId}",
                    HttpCompletionOption.ResponseContentRead, token);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("Failed to retrieve customer information  {CustomerId}", customerId);
                    throw new CustomerNotFoundException(customerId);
                }

                logger.LogTrace("Retrieved customer information");
                await using var stream = response.Content.ReadAsStream();
                logger.LogTrace("Deserializing customer information");
                var customer = await JsonSerializer.DeserializeAsync<Customer>(stream, cancellationToken: token);
                logger.LogTrace("Deserialized customer information {CustomerId}", customerId);
                

                using (logger.BeginScope("Order {OrderId} for customer {CustomerId}", orderId, customerId))
                {
                    var responseOrder = await httpClient.GetAsync($"{options.Value.OrderUrl}/{orderId}",
                        HttpCompletionOption.ResponseContentRead, token);
                    if (!responseOrder.IsSuccessStatusCode)
                    {
                        logger.LogError("Failed to retrieve order information for {OrderId}", orderId);
                        throw new OrderNotFoundException(orderId);
                    }
                    
                    await using var streamOrder = responseOrder.Content.ReadAsStream();
                    var order = await JsonSerializer.DeserializeAsync<Order>(streamOrder, cancellationToken: token);
                    return new CustomerOrder(customer!.Name, customer.Email, order!.Number, order.Items,
                        order.Items.Sum(i => i.Price * i.Quantity));
                }
            }
        }
    }
});

await app.RunAsync();


public class MicroservicesUrlOptions
{
    public string CustomerUrl { get; set; }
    public string OrderUrl { get; set; }
}
public record CustomerOrder(string Name, string Email, string OrderNumber, IEnumerable<OrderItem> Items, decimal Total) { }
public record Customer(int Id, string Name, string Email) { }
public record Order(int Id, string Number, int CustomerId, IEnumerable<OrderItem> Items){}
public struct OrderItem
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class CustomerNotFoundException : Exception
{
    public CustomerNotFoundException(int customerId) : base($"Customer {customerId} not found")
    {
        CustomerId = customerId;
    }

    public int CustomerId { get; init; }

    public void Deconstruct(out int customerId)
    {
        customerId = CustomerId;
    }
}
public class OrderNotFoundException : Exception
{
    public OrderNotFoundException(int orderId) : base($"Order {orderId} not found")
    {
        OrderId = orderId;
    }

    public int OrderId { get; init; }

    public void Deconstruct(out int orderId)
    {
        orderId = OrderId;
    }
}