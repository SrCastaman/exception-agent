using ExceptionAgent.Application.Allocation;
using ExceptionAgent.Data;
using ExceptionAgent.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ExceptionAgent.Tests.Allocation;

public class AllocationScenarioServiceTests
{
    [Fact]
    public async Task BuildScenariosAsync_ShouldUseSupplierEmailEventsForCurrentDates()
    {
        await using var connection =
            new SqliteConnection("DataSource=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AppDbContext(options);

        await context.Database.EnsureCreatedAsync();

        DbSeeder.Seed(context);

        // --------------------------------------------------
        // Obtener los POs del escenario
        // --------------------------------------------------

        var purchaseOrder1042 = await context.PurchaseOrders
            .Include(po => po.Lines)
            .SingleAsync(po => po.Reference == "PO-1042");

        var purchaseOrder1044 = await context.PurchaseOrders
            .Include(po => po.Lines)
            .SingleAsync(po => po.Reference == "PO-1044");

        // --------------------------------------------------
        // Crear emails de prueba
        // --------------------------------------------------

        var email1042 = new Email
        {
            Sender = "compras@abcindustrial.es",
            Recipient = "empresa@test.com",
            Subject = "Retraso PO-1042",
            Body = "El pedido PO-1042 llegará el 20/08/2026.",
            Date = new DateTime(2026, 8, 16),
            SupplierId = purchaseOrder1042.SupplierId
        };

        var email1044 = new Email
        {
            Sender = "compras@abcindustrial.es",
            Recipient = "empresa@test.com",
            Subject = "Retraso PO-1044",
            Body = "El pedido PO-1044 llegará el 21/08/2026.",
            Date = new DateTime(2026, 8, 16),
            SupplierId = purchaseOrder1044.SupplierId
        };

        context.Emails.AddRange(
            email1042,
            email1044);

        await context.SaveChangesAsync();

        // --------------------------------------------------
        // Crear eventos de proveedor
        // --------------------------------------------------

        context.SupplierEmailEvents.AddRange(
            new SupplierEmailEvent
            {
                EmailId = email1042.Id,
                PurchaseOrderId = purchaseOrder1042.Id,
                EventType = "DELIVERY_DELAY",
                AffectedQuantity = 50,
                NewExpectedDate = new DateTime(2026, 8, 20),
                Evidence =
                    "El proveedor comunica que PO-1042 llegará el 20/08/2026."
            },
            new SupplierEmailEvent
            {
                EmailId = email1044.Id,
                PurchaseOrderId = purchaseOrder1044.Id,
                EventType = "DELIVERY_DELAY",
                AffectedQuantity = 80,
                NewExpectedDate = new DateTime(2026, 8, 21),
                Evidence =
                    "El proveedor comunica que PO-1044 llegará el 21/08/2026."
            });

        await context.SaveChangesAsync();

        // --------------------------------------------------
        // Crear servicios
        // --------------------------------------------------

        var dataService = new AllocationDataService(context);

        var service = new AllocationScenarioService(
            dataService);

        // --------------------------------------------------
        // Construir escenarios para PO-1042
        // --------------------------------------------------

        var result = await service.BuildScenariosAsync(
            purchaseOrder1042);

        Assert.NotNull(result);

        // ==================================================
        // PO-1042
        // ==================================================

        var normalPo1042 = result!.Normal.Supplies
            .Single(s => s.Reference == "PO-1042");

        Assert.Equal(
            new DateTime(2026, 8, 15),
            normalPo1042.AvailableDate);

        var currentPo1042 = result.Current.Supplies
            .Single(s => s.Reference == "PO-1042");

        Assert.Equal(
            new DateTime(2026, 8, 20),
            currentPo1042.AvailableDate);

        var withoutDelayPo1042 =
            result.WithoutInvestigatedDelay.Supplies
                .Single(s => s.Reference == "PO-1042");

        Assert.Equal(
            new DateTime(2026, 8, 15),
            withoutDelayPo1042.AvailableDate);

        // ==================================================
        // PO-1044
        // ==================================================

        var normalPo1044 = result.Normal.Supplies
            .Single(s => s.Reference == "PO-1044");

        Assert.Equal(
            new DateTime(2026, 8, 16),
            normalPo1044.AvailableDate);

        var currentPo1044 = result.Current.Supplies
            .Single(s => s.Reference == "PO-1044");

        Assert.Equal(
            new DateTime(2026, 8, 21),
            currentPo1044.AvailableDate);

        var withoutDelayPo1044 =
            result.WithoutInvestigatedDelay.Supplies
                .Single(s => s.Reference == "PO-1044");

        Assert.Equal(
            new DateTime(2026, 8, 21),
            withoutDelayPo1044.AvailableDate);
    }
}