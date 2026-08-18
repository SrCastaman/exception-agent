using ExceptionAgent.Application.Allocation;
using ExceptionAgent.Application.Allocation.Models;
using ExceptionAgent.Application.Allocation.Policies;
using ExceptionAgent.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ExceptionAgent.Tests.Allocation;

public class AllocationScenarioIntegrationTests
{
    [Fact]
    public async Task MotorScenario_ShouldProduceExpectedAllocation()
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

        var motor = await context.Products
            .SingleAsync(p => p.Reference == "MOT-X200");

        var dataService = new AllocationDataService(context);

        var data = await dataService.GetDataAsync(
            new[] { motor.Id });

        var scenarioBuilder = new AllocationScenarioBuilder();

        var scenario = scenarioBuilder.Build(
            data.PurchaseOrders,
            data.PurchaseOrders
                .SelectMany(po => po.Lines)
                .ToList(),
            data.Inventories,
            data.CustomerOrders);

        var policy = new DatePriorityAllocationPolicy();

        var engine = new AllocationEngine(policy);

        var result = engine.Allocate(
            scenario.Supplies,
            scenario.Demands);

        Assert.Equal(3, scenario.Supplies.Count);
        Assert.Equal(3, scenario.Demands.Count);

        Assert.Empty(result.UncoveredDemands);

        var co8823Allocations = result.Allocations
            .Where(a => a.DemandReference == "CO-8823")
            .ToList();

        Assert.Equal(2, co8823Allocations.Count);

        Assert.Equal(
            5,
            co8823Allocations
                .Single(a => a.SupplyReference == "STOCK")
                .Quantity);

        Assert.Equal(
            25,
            co8823Allocations
                .Single(a => a.SupplyReference == "PO-1042")
                .Quantity);

        var co8821Allocation = result.Allocations
            .Single(a => a.DemandReference == "CO-8821");

        Assert.Equal(
            "PO-1042",
            co8821Allocation.SupplyReference);

        Assert.Equal(
            25,
            co8821Allocation.Quantity);

        var co8824Allocation = result.Allocations
            .Single(a => a.DemandReference == "CO-8824");

        Assert.Equal(
            "PO-1044",
            co8824Allocation.SupplyReference);

        Assert.Equal(
            20,
            co8824Allocation.Quantity);

        Assert.Equal(
            4,
            result.Allocations.Count);
    }
    [Fact]
    public async Task MotorScenario_ShouldCalculateMarginalImpactOfDelayedPurchaseOrders()
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

        var motor = await context.Products
            .SingleAsync(p => p.Reference == "MOT-X200");

        var dataService = new AllocationDataService(context);

        var data = await dataService.GetDataAsync(
            new[] { motor.Id });

        var demands = data.CustomerOrders
            .Select(order => new Demand
            {
                Reference = order.Reference,
                ProductId = order.ProductId,
                Quantity = order.Quantity,
                RequiredDate = order.RequiredDate
            })
            .ToList();

        var supplyScenarios = data.PurchaseOrders
            .SelectMany(po => po.Lines
                .Where(line => line.ProductId == motor.Id)
                .Select(line => new SupplyScenario
                {
                    Reference = po.Reference,
                    ProductId = line.ProductId,
                    Quantity = Math.Max(
                        0,
                        line.OrderedQuantity -
                        line.ReceivedQuantity),
                    NormalAvailableDate = po.ExpectedDate,
                    CurrentAvailableDate = po.ExpectedDate
                }))
            .ToList();

        // El stock actual también forma parte del suministro.
        supplyScenarios.AddRange(
            data.Inventories
                .Where(i => i.ProductId == motor.Id)
                .Select(i => new SupplyScenario
                {
                    Reference = "STOCK",
                    ProductId = i.ProductId,
                    Quantity = i.AvailableQuantity,
                    NormalAvailableDate = DateTime.MinValue,
                    CurrentAvailableDate = DateTime.MinValue
                }));

        var policy = new DatePriorityAllocationPolicy();

        var engine = new AllocationEngine(policy);

        // Escenario A: situación normal.
        var normalResult = AllocateScenario(
            supplyScenarios,
            demands,
            engine,
            po1042Delay: null,
            po1044Delay: null);

        // Escenario B: solo PO-1042 retrasado.
        var onlyPo1042DelayedResult = AllocateScenario(
            supplyScenarios,
            demands,
            engine,
            po1042Delay: new DateTime(2026, 8, 20),
            po1044Delay: null);

        // Escenario C: solo PO-1044 retrasado.
        var onlyPo1044DelayedResult = AllocateScenario(
            supplyScenarios,
            demands,
            engine,
            po1042Delay: null,
            po1044Delay: new DateTime(2026, 8, 21));

        // Escenario D: ambos retrasados.
        var bothDelayedResult = AllocateScenario(
            supplyScenarios,
            demands,
            engine,
            po1042Delay: new DateTime(2026, 8, 20),
            po1044Delay: new DateTime(2026, 8, 21));

        // Situación normal: todo queda cubierto.
        Assert.Empty(normalResult.UncoveredDemands);

        // Solo PO-1042 retrasado:
        // PO-1044 sigue llegando el 16/08, por lo que todo sigue cubierto.
        Assert.Empty(
            onlyPo1042DelayedResult.UncoveredDemands);

        // Solo PO-1044 retrasado:
        // PO-1042 sigue llegando el 15/08.
        // Queda sin cubrir únicamente CO-8824: 20 unidades.
        Assert.Equal(
            20,
            onlyPo1044DelayedResult.UncoveredDemands
                .Sum(d => d.Quantity));

        // Ambos retrasados:
        // Solo hay 5 unidades de stock antes de las fechas necesarias.
        // Déficit total: 70.
        Assert.Equal(
            70,
            bothDelayedResult.UncoveredDemands
                .Sum(d => d.Quantity));

        // Impacto marginal de PO-1042:
        // 70 - 20 = 50 unidades.
        var impactOfPo1042 =
            bothDelayedResult.UncoveredDemands.Sum(d => d.Quantity)
            - onlyPo1044DelayedResult.UncoveredDemands.Sum(d => d.Quantity);

        // Impacto marginal de PO-1044:
        // 70 - 0 = 70 unidades.
        var impactOfPo1044 =
            bothDelayedResult.UncoveredDemands.Sum(d => d.Quantity)
            - onlyPo1042DelayedResult.UncoveredDemands.Sum(d => d.Quantity);

        Assert.Equal(50, impactOfPo1042);
        Assert.Equal(70, impactOfPo1044);

        // En el escenario con ambos retrasados:
        // CO-8823 → 25 unidades sin cubrir.
        Assert.Equal(
            25,
            bothDelayedResult.UncoveredDemands
                .Single(d => d.DemandReference == "CO-8823")
                .Quantity);

        // CO-8821 → 25 unidades sin cubrir.
        Assert.Equal(
            25,
            bothDelayedResult.UncoveredDemands
                .Single(d => d.DemandReference == "CO-8821")
                .Quantity);

        // CO-8824 → 20 unidades sin cubrir.
        Assert.Equal(
            20,
            bothDelayedResult.UncoveredDemands
                .Single(d => d.DemandReference == "CO-8824")
                .Quantity);
    }

    private static AllocationResult AllocateScenario(
        List<SupplyScenario> supplyScenarios,
        List<Demand> demands,
        AllocationEngine engine,
        DateTime? po1042Delay,
        DateTime? po1044Delay)
    {
        var supplies = supplyScenarios
            .Select(scenario =>
            {
                var availableDate =
                    scenario.NormalAvailableDate;

                if (scenario.Reference == "PO-1042" &&
                    po1042Delay.HasValue)
                {
                    availableDate = po1042Delay.Value;
                }

                if (scenario.Reference == "PO-1044" &&
                    po1044Delay.HasValue)
                {
                    availableDate = po1044Delay.Value;
                }

                return new Supply
                {
                    Reference = scenario.Reference,
                    ProductId = scenario.ProductId,
                    Quantity = scenario.Quantity,
                    AvailableDate = availableDate
                };
            })
            .ToList();

        return engine.Allocate(
            supplies,
            demands);
    }
}