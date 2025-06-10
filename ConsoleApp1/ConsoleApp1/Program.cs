using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

public class Order
{
    public int Id { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public List<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; }
    public int Quantity { get; set; } = 1;
}

public class ShopContext : DbContext
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=Shopdb;Trusted_Connection=True;");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasColumnType("decimal(18,2)");
    }
}
public class OrderService
{
    private readonly ShopContext _context;

    public OrderService(ShopContext context)
    {
        _context = context;
    }

    public void AddOrder(Order order)
    {
        _context.Orders.Add(order);
        _context.SaveChanges();
    }

    public void RemoveOrder(int orderId)
    {
        var order = _context.Orders
            .Include(o => o.Items)
            .FirstOrDefault(o => o.Id == orderId);

        if (order != null)
        {
            _context.Orders.Remove(order);
            _context.SaveChanges();
        }
    }
    public List<Order> GetAllOrders()
    {
        return _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .ToList();
    }
    public void AddProductToOrder(int orderId, int productId, int quantity = 1)
    {
        var order = _context.Orders.Find(orderId);
        var product = _context.Products.Find(productId);

        if (order == null || product == null) return;

        var item = new OrderItem
        {
            OrderId = orderId,
            ProductId = productId,
            Quantity = quantity
        };

        _context.OrderItems.Add(item);
        _context.SaveChanges();
    }
}

class Program
{
    static void Main()
    {
        using (var context = new ShopContext())
        {
            context.Database.EnsureCreated();

            if (!context.Products.Any())
            {
                SeedProducts(context);
            }
        }
    }

    static void SeedProducts(ShopContext context)
    {
        context.Products.AddRange(
            new Product { Name = "Milk", Price = 2.5m },
            new Product { Name = "Bread", Price = 1.3m },
            new Product { Name = "Apples", Price = 3.0m }
        );
        context.SaveChanges();
        Console.WriteLine("Seeded initial products");
    }

    static void DisplayOrders(OrderService service)
    {
        var orders = service.GetAllOrders();

        if (!orders.Any())
        {
            Console.WriteLine("No orders found");
            return;
        }

        foreach (var order in orders)
        {
            Console.WriteLine($"Order #{order.Id} ({order.CreatedDate})");
            Console.WriteLine("Items:");
            foreach (var item in order.Items)
            {
                Console.WriteLine($"  {item.Product.Name} x {item.Quantity} = {item.Product.Price * item.Quantity}$");
            }
            Console.WriteLine($"Total: {order.Items.Sum(i => i.Product.Price * i.Quantity)}$");
        }
    }

    static string GetOrderStatus(OrderService service)
    {
        return service.GetAllOrders().Any()
            ? "Orders exist in system"
            : "No orders in system";
    }
}