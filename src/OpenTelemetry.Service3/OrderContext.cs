using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OpenTelemetry.Service3;

public class OrderContextFactory : IDesignTimeDbContextFactory<OrderContext>
{
    public OrderContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OrderContext>();
        optionsBuilder.UseSqlServer(Environment.GetEnvironmentVariable("OrdersConnectionString"));
        optionsBuilder.EnableSensitiveDataLogging();
        return new OrderContext(optionsBuilder.Options);
    }
}
public sealed class OrderContext: DbContext
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    public OrderContext(DbContextOptions<OrderContext> options) : base(options)
    {
        this.Database.EnsureCreated();
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>().HasIndex(order => order.Id).IsUnique();
        modelBuilder.Entity<OrderItem>().HasIndex(orderItem => orderItem.Id).IsUnique();
        modelBuilder.Entity<Order>()
            .HasMany(e => e.Items)
            .WithOne(e => e.Order)
            .HasForeignKey(e => e.OrderId)
            .HasPrincipalKey(e => e.Id);
    
        base.OnModelCreating(modelBuilder);
        new DbInitializer(modelBuilder).Seed();
    }
}

public class Order{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Number { get; set; }
    public int CustomerId { get; set; }
    
    public ICollection<OrderItem> Items { get; set; }
        
}
public class OrderItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class DbInitializer
{
    private readonly ModelBuilder modelBuilder;

    public DbInitializer(ModelBuilder modelBuilder)
    {
        this.modelBuilder = modelBuilder;
    }
    
    public void Seed()
    {
        var items = new[]
        {
            new OrderItem
            {
                Id = 1,
                OrderId = 1,
                Name = "Item 1",
                Price = 10,
                Quantity = 2
            },
            new OrderItem
            {
                Id = 2,
                OrderId = 1,
                Name = "Item 2",
                Price = 20,
                Quantity = 1
            },
            new OrderItem()
            {
                Id = 3,
                OrderId = 2,
                Name = "Item 3",
                Price = 30,
                Quantity = 1
            },
            new OrderItem
            {
                Id = 4,
                OrderId = 2,
                Name = "Item 1",
                Price = 10,
                Quantity = 21
            },
            new OrderItem()
            {
                Id = 5,
                OrderId = 3,
                Name = "Item 3",
                Price = 30,
                Quantity = 1
            },
            new OrderItem()
            {

                Id = 6,
                OrderId = 4,
                Name = "Item 34",
                Price = 0.5m,
                Quantity = 15
            },
            new OrderItem
            {
                Id = 7,
                OrderId = 4,
                Name = "Item 21",
                Price = 23.5m,
                Quantity = 143
            },
            new OrderItem
            {
                Id = 8,
                OrderId = 5,
                Name = "Item 1",
                Price = 10,
                Quantity = 2
            },
            new OrderItem
            {
                Id = 9,
                OrderId = 6,
                Name = "Item 1",
                Price = 10,
                Quantity = 21
            },
            new OrderItem()
            {
                Id = 10,
                OrderId = 6,
                Name = "Item 3",
                Price = 30,
                Quantity = 1
            },
            new OrderItem()
            {
                Id = 11,
                OrderId = 6,
                Name = "Item 34",
                Price = 0.5m,
                Quantity = 15
            }
        };
        var orders = new[]
        {
            new Order
            {
                Id = 1,
                Number = "ORD-1",
                CustomerId = 1
            },
            new Order
            {
                Id = 2,
                Number = "ORD-2",
                CustomerId = 1,
            },
            new Order
            {
                Id = 3,
                Number = "ORD-3",
                CustomerId = 2,
            },
            new Order
            {
                Id = 4,
                Number = "ORD-4",
                CustomerId = 2,
            },
            new Order{
                Id = 5,
                Number = "ORD-5", 
                CustomerId = 5
            },
            new Order
            {
                Id = 6,
                Number = "ORD-6", 
                CustomerId = 5,
            },
        };
        modelBuilder.Entity<Order>().HasData(orders);
        modelBuilder.Entity<OrderItem>().HasData(items);

    }
}