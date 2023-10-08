namespace OpenTelemetry.Service1;

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