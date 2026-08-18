using ExceptionAgent.Application.Allocation.Models;
using ExceptionAgent.Domain.Entities;

namespace ExceptionAgent.Application.Allocation;

public class AllocationScenarioService
{
    private readonly AllocationDataService _dataService;

    public AllocationScenarioService(
        AllocationDataService dataService)
    {
        _dataService = dataService;
    }

    public async Task<AllocationScenarioSet?> BuildScenariosAsync(
        PurchaseOrder purchaseOrder)
    {
        if (purchaseOrder.Lines == null ||
            purchaseOrder.Lines.Count == 0)
        {
            return null;
        }

        var productIds = purchaseOrder.Lines
            .Select(line => line.ProductId)
            .Distinct()
            .ToList();

        var data = await _dataService.GetDataAsync(productIds);

        var demands = data.CustomerOrders
            .Select(order => new Demand
            {
                Reference = order.Reference,
                ProductId = order.ProductId,
                Quantity = order.Quantity,
                RequiredDate = order.RequiredDate
            })
            .ToList();

        var supplyScenarios = BuildSupplyScenarios(data);

        var normalScenario = BuildScenario(
            supplyScenarios,
            demands,
            useCurrentDates: false,
            investigatedPurchaseOrderReference: null);

        var currentScenario = BuildScenario(
            supplyScenarios,
            demands,
            useCurrentDates: true,
            investigatedPurchaseOrderReference: null);

        var withoutInvestigatedDelayScenario = BuildScenario(
            supplyScenarios,
            demands,
            useCurrentDates: true,
            investigatedPurchaseOrderReference: purchaseOrder.Reference);

        return new AllocationScenarioSet
        {
            Normal = normalScenario,
            Current = currentScenario,
            WithoutInvestigatedDelay =
                withoutInvestigatedDelayScenario
        };
    }

    private static List<SupplyScenario> BuildSupplyScenarios(
        AllocationData data)
    {
        var scenarios = data.PurchaseOrders
            .SelectMany(po =>
                po.Lines.Select(line => new SupplyScenario
                {
                    Reference = po.Reference,
                    ProductId = line.ProductId,
                    Quantity = Math.Max(
                        0,
                        line.OrderedQuantity -
                        line.ReceivedQuantity),

                    NormalAvailableDate = po.ExpectedDate,

                    CurrentAvailableDate =
                        GetCurrentAvailableDate(
                            po,
                            data.SupplierEmailEvents)
                }))
            .Where(s => s.Quantity > 0)
            .ToList();

        scenarios.AddRange(
            data.Inventories
                .Where(i => i.AvailableQuantity > 0)
                .Select(i => new SupplyScenario
                {
                    Reference = "STOCK",
                    ProductId = i.ProductId,
                    Quantity = i.AvailableQuantity,
                    NormalAvailableDate = DateTime.MinValue,
                    CurrentAvailableDate = DateTime.MinValue
                }));

        return scenarios;
    }

    private static AllocationScenario BuildScenario(
        List<SupplyScenario> supplyScenarios,
        List<Demand> demands,
        bool useCurrentDates,
        string? investigatedPurchaseOrderReference)
    {
        var supplies = supplyScenarios
            .Select(scenario =>
            {
                var availableDate =
                    useCurrentDates
                        ? scenario.CurrentAvailableDate
                        : scenario.NormalAvailableDate;

                if (useCurrentDates &&
                    investigatedPurchaseOrderReference != null &&
                    scenario.Reference ==
                    investigatedPurchaseOrderReference)
                {
                    availableDate =
                        scenario.NormalAvailableDate;
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

        return new AllocationScenario
        {
            Supplies = supplies,
            Demands = demands.ToList()
        };
    }

    private static DateTime GetCurrentAvailableDate(
        PurchaseOrder purchaseOrder,
        List<SupplierEmailEvent> emailEvents)
    {
        var latestEvent = emailEvents
            .Where(e =>
                e.PurchaseOrderId == purchaseOrder.Id &&
                e.NewExpectedDate.HasValue)
            .OrderByDescending(e => e.Id)
            .FirstOrDefault();

        return latestEvent?.NewExpectedDate
            ?? purchaseOrder.ExpectedDate;
    }
}