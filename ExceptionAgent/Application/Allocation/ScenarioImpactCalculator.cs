using ExceptionAgent.Application.Allocation.Models;

namespace ExceptionAgent.Application.Allocation;

public class ScenarioImpactCalculator
{
    public ScenarioImpactResult Compare(
        AllocationResult normalResult,
        AllocationResult currentResult,
        AllocationScenario currentScenario,
        string investigatedPurchaseOrderReference)
    {
        var result = new ScenarioImpactResult();

        var normalUncovered = normalResult.UncoveredDemands
            .ToDictionary(
                demand => demand.DemandReference,
                demand => demand.Quantity);

        var currentUncovered = currentResult.UncoveredDemands
            .ToDictionary(
                demand => demand.DemandReference,
                demand => demand.Quantity);

        var allDemandReferences = normalUncovered.Keys
            .Union(currentUncovered.Keys)
            .ToList();

        var investigatedSupply = currentScenario.Supplies
            .FirstOrDefault(s =>
                s.Reference == investigatedPurchaseOrderReference);

        var supplierExpectedDate =
            investigatedSupply?.AvailableDate
            ?? DateTime.MinValue;

        foreach (var demandReference in allDemandReferences)
        {
            var normalShortage =
                normalUncovered.TryGetValue(
                    demandReference,
                    out var normalQuantity)
                    ? normalQuantity
                    : 0;

            var currentShortage =
                currentUncovered.TryGetValue(
                    demandReference,
                    out var currentQuantity)
                    ? currentQuantity
                    : 0;

            if (currentShortage > normalShortage)
            {
                var additionalShortage =
                    currentShortage - normalShortage;

                result.NewlyAtRiskDemands.Add(
                    demandReference);

                result.AdditionalShortageQuantity +=
                    additionalShortage;

                var demand = currentResult.UncoveredDemands
                    .First(d =>
                        d.DemandReference == demandReference);

                var allocatedQuantity = currentResult.Allocations
                    .Where(a =>
                        a.DemandReference == demandReference)
                    .Sum(a => a.Quantity);

                var stockAllocatedQuantity = currentResult.Allocations
                    .Where(a =>
                        a.DemandReference == demandReference &&
                        a.SupplyReference == "STOCK")
                    .Sum(a => a.Quantity);

                result.AffectedCustomerOrders.Add(
                    new AffectedDemand
                    {
                        Reference = demand.DemandReference,
                        ProductId = demand.ProductId,
                        Quantity = demand.Quantity,
                        AvailableStock = stockAllocatedQuantity,
                        AllocatedStock = allocatedQuantity,
                        ShortageQuantity = additionalShortage,
                        RequiredDate = demand.RequiredDate,
                        SupplierExpectedDate =
                            supplierExpectedDate,
                        SupplierDeliveryAfterRequiredDate =
                            supplierExpectedDate >
                            demand.RequiredDate,
                        AtRisk = true
                    });
            }
            else if (normalShortage > currentShortage)
            {
                result.NoLongerAtRiskDemands.Add(
                    demandReference);

                result.RecoveredShortageQuantity +=
                    normalShortage - currentShortage;
            }
        }

        result.RiskDate = result.AffectedCustomerOrders
            .OrderBy(order => order.RequiredDate)
            .Select(order => (DateTime?)order.RequiredDate)
            .FirstOrDefault();

        return result;
    }
}