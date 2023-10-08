using System.Text.Json.Serialization;

namespace OpenTelemetry.Service1;

public record CustomerOrder(string name, string email, string orderNumber, IEnumerable<OrderItem> items, decimal total) { }

public record Customer(int Id, string Name, string Email) { }

public record Order(int Id, string Number, int CustomerId, IEnumerable<OrderItem> Items) { }

public struct OrderItem
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}