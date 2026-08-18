using ExceptionAgent.Application.Allocation;
using ExceptionAgent.Data;
using ExceptionAgent.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ExceptionAgent.Tests.Allocation;

public class AllocationDataServiceTests
{
    [Fact]
    public async Task GetDataAsync_ShouldReturnDataForRequestedProducts()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AppDbContext(options);

        await context.Database.EnsureCreatedAsync();

        var supplier = new Supplier
        {
            Name = "Test Supplier",
            Email = "test@test.com"
        };

        var motor = new Product
        {
            Reference = "MOTOR",
            Name = "Motor"
        };

        var sensor = new Product
        {
            Reference = "SENSOR",
            Name = "Sensor"
        };

        context.Suppliers.Add(supplier);
        context.Products.AddRange(motor, sensor);

        await context.SaveChangesAsync();

        var purchaseOrder = new PurchaseOrder
        {
            Reference = "PO-TEST",
            SupplierId = supplier.Id,
            ExpectedDate = new DateTime(2026, 8, 20),
            Status = "PartiallyReceived"
        };

        context.PurchaseOrders.Add(purchaseOrder);

        await context.SaveChangesAsync();

        var purchaseOrderLine = new PurchaseOrderLine
        {
            PurchaseOrderId = purchaseOrder.Id,
            ProductId = motor.Id,
            OrderedQuantity = 40,
            ReceivedQuantity = 0
        };

        var inventory = new Inventory
        {
            ProductId = motor.Id,
            AvailableQuantity = 5
        };

        var customerOrder = new CustomerOrder
        {
            Reference = "CO-TEST",
            ProductId = motor.Id,
            Quantity = 20,
            RequiredDate = new DateTime(2026, 8, 18)
        };

        context.PurchaseOrderLines.Add(purchaseOrderLine);
        context.Inventories.Add(inventory);
        context.CustomerOrders.Add(customerOrder);

        await context.SaveChangesAsync();

        var service = new AllocationDataService(context);

        var result = await service.GetDataAsync(
            new[] { motor.Id });

        Assert.Single(result.PurchaseOrders);
        Assert.Equal(
            "PO-TEST",
            result.PurchaseOrders[0].Reference);

        Assert.Single(result.Inventories);
        Assert.Equal(
            5,
            result.Inventories[0].AvailableQuantity);

        Assert.Single(result.CustomerOrders);
        Assert.Equal(
            "CO-TEST",
            result.CustomerOrders[0].Reference);
    }
}