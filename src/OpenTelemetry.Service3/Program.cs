using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Service3;
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
        .AddSqlClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter()
    );
builder.Services.AddDbContext<OrderContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Orders"));
});


var app = builder.Build();
app.MapHealthChecks("/healthz");
app.MapGet("/{orderId:int}", async ([FromRoute] int orderId, [FromServices] OrderContext ctx, 
    [FromServices] ILogger<Program> logger, CancellationToken token) =>
{

    using (logger.BeginScope("taking order {OrderId}", orderId))
    {
        var order = await ctx.Orders.Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, token);
        if (order is null)
        {
            logger.LogError("Order {OrderId} not found", orderId);
            throw new OrderNotFoundException(orderId);
        }

        logger.LogInformation("Order {OrderId} found", orderId);
        return new OrderDto(order.Id, order.Number, order.CustomerId,
            order.Items.Select(i => new OrderItemDto(i.Name, i.Price, i.Quantity))
                .ToArray());
    }
});

await app.RunAsync();

public record OrderDto(int Id, string Number, int CustomerId, IEnumerable<OrderItemDto> Items){}
public struct OrderItemDto
{
    public OrderItemDto(string name, decimal price, int quantity)
    {
        Name = name;
        Price = price;
        Quantity = quantity;   
    }

    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
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