using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Service2;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
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
        .AddSqlClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter()
    );

builder.Services.AddScoped<ICustomerService, CustomerService>();

var app = builder.Build();
app.MapHealthChecks("/healthz");
app.MapGet("/{customerId:int}", async ([FromRoute]int customerId, [FromServices]ICustomerService service,  [FromServices]ILogger<Program> logger, CancellationToken token) =>
{
    logger.LogInformation("Processing customer {CustomerId}", customerId);
    var customer = await service.GetCustomer(customerId);
    logger.LogInformation("Retrieved customer {CustomerId}", customerId);
    return Results.Ok(customer);
});

await app.RunAsync();

public record Customer(int Id, string Name, string Email) { }

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