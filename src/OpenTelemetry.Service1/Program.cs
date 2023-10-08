using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Service1;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)       
    .AddJsonFile($"appsettings{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

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

Console.WriteLine($"send metrics and trace to: {app.Configuration.GetValue<string>("OpenTelemetry:OtlpExporter:Endpoint")}");

app.MapHealthChecks("/healthz");
app.MapGet("customer/{customerId:int}/Order/{orderId:int}", 
    async ([FromRoute]int customerId, [FromRoute]int orderId, [FromServices]IOptions<MicroservicesUrlOptions> options,
        [FromServices]IHttpClientFactory httpClientFactory, [FromServices]ILogger<Program> logger, CancellationToken token) =>
{
    logger.LogInformation("Processing customer {CustomerId}", customerId);
    var jsonSerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    var urls  = options.Value?? throw new ArgumentNullException(nameof(options));
    var customerUrl = $"{urls.CustomerUrl}/{customerId}";
    var orderUrl = $"{urls.OrderUrl}/{orderId}";
    using (logger.BeginScope("Processing Order {OrderId} for customer {CustomerId}", orderId, customerId))
    {
        using (var httpClient = httpClientFactory.CreateClient())
        {
            using (logger.BeginScope("Retrieving customer information {CustomerId}", customerId))
            {
                  
                var response = await httpClient.GetAsync(customerUrl,
                    HttpCompletionOption.ResponseContentRead, token);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("Failed to retrieve customer information  {CustomerId}", customerId);
                    throw new CustomerNotFoundException(customerId);
                }

                logger.LogTrace("Retrieved customer information");
                await using var stream = response.Content.ReadAsStream();
                logger.LogTrace("Deserializing customer information");
                var customer = await JsonSerializer.DeserializeAsync<Customer>(stream, jsonSerializerOptions,  cancellationToken: token);
                logger.LogTrace("Deserialized customer information {CustomerId}", customerId);

                using var scope = logger.BeginScope("Order {OrderId} for customer {CustomerId}", orderId, customerId);
                var responseOrder = await httpClient.GetAsync(orderUrl,
                    HttpCompletionOption.ResponseContentRead, token);
                if (!responseOrder.IsSuccessStatusCode)
                {
                    logger.LogError("Failed to retrieve order information for {OrderId}", orderId);
                    throw new OrderNotFoundException(orderId);
                }
                    
                await using var streamOrder = responseOrder.Content.ReadAsStream();
                var order = await JsonSerializer.DeserializeAsync<Order>(streamOrder, jsonSerializerOptions, cancellationToken: token);
                Debug.Assert(customer != null, nameof(customer) + " != null");
                Debug.Assert(order != null, nameof(order) + " != null");
                return new CustomerOrder(customer.Name, customer.Email, order.Number, order.Items,
                    order.Items.Sum(i => i.Price * i.Quantity));
            }
        }
    }
});

await app.RunAsync();
