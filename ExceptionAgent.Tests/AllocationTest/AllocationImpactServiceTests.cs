using ExceptionAgent.Application.Allocation;
using ExceptionAgent.Application.Allocation.Policies;
using ExceptionAgent.Data;
using ExceptionAgent.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ExceptionAgent.Tests.Allocation;

public class AllocationImpactServiceTests
{
    [Fact]
    public async Task CalculateImpactAsync_ShouldCalculateImpactOfPo1042()
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
        // Obtener los POs
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
        // Crear dependencias
        // --------------------------------------------------

        var dataService = new AllocationDataService(context);

        var scenarioService = new AllocationScenarioService(
            dataService);

        var policy = new DatePriorityAllocationPolicy();

        var allocationEngine = new AllocationEngine(
            policy);

        var impactCalculator =
            new ScenarioImpactCalculator();

        var impactService = new AllocationImpactService(
            context,
            scenarioService,
            allocationEngine,
            impactCalculator);

        // --------------------------------------------------
        // Calcular impacto de PO-1042
        // --------------------------------------------------

        var result = await impactService.CalculateImpactAsync(
            purchaseOrder1042.Id);

        // --------------------------------------------------
        // Comprobaciones
        // --------------------------------------------------

        Assert.NotNull(result);

        Assert.Equal(
            50,
            result!.AdditionalShortageQuantity);

        Assert.Contains(
            "CO-8823",
            result.NewlyAtRiskDemands);

        Assert.Contains(
            "CO-8821",
            result.NewlyAtRiskDemands);

        Assert.DoesNotContain(
            "CO-8824",
            result.NewlyAtRiskDemands);

        Assert.Empty(
            result.NoLongerAtRiskDemands);

        Assert.Equal(
            0,
            result.RecoveredShortageQuantity);
    }
}