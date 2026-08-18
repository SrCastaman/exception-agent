using ExceptionAgent.Application.Allocation.Models;


namespace ExceptionAgent.Application.Allocation.Policies;

public class DatePriorityAllocationPolicy : IAllocationPolicy
{
    public AllocationResult Allocate(
        List<Supply> supplies,
        List<Demand> demands)
    {
        var result = new AllocationResult();

        var orderedSupplies = supplies
            .OrderBy(supply => supply.AvailableDate)
            .ToList();

        var orderedDemands = demands
            .OrderBy(demand => demand.RequiredDate)
            .ThenBy(demand => demand.Reference)
            .ToList();

        foreach (var demand in orderedDemands)
        {
            var remainingDemand = demand.Quantity;

            foreach (var supply in orderedSupplies)
            {
                if (remainingDemand <= 0)
                {
                    break;
                }

                if (supply.ProductId != demand.ProductId)
                {
                    continue;
                }

                if (supply.Quantity <= 0)
                {
                    continue;
                }

                if (supply.AvailableDate > demand.RequiredDate)
                {
                    continue;
                }

                var allocatedQuantity = Math.Min(
                    supply.Quantity,
                    remainingDemand);

                result.Allocations.Add(new SupplyAllocation
                {
                    SupplyReference = supply.Reference,
                    DemandReference = demand.Reference,
                    Quantity = allocatedQuantity,
                    AvailableDate = supply.AvailableDate,
                    RequiredDate = demand.RequiredDate
                });

                supply.Quantity -= allocatedQuantity;
                remainingDemand -= allocatedQuantity;
            }

            if (remainingDemand > 0)
            {
                result.UncoveredDemands.Add(new UncoveredDemand
                {
                    DemandReference = demand.Reference,
                    ProductId = demand.ProductId,
                    Quantity = remainingDemand,
                    RequiredDate = demand.RequiredDate
                });
            }
        }

        return result;
    }
}