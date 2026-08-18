using ExceptionAgent.Application.Allocation;
using ExceptionAgent.Contracts;

namespace ExceptionAgent.Application.Exceptions;

public class ExceptionRiskCalculationService
{
    private readonly AllocationImpactService _allocationImpactService;

    public ExceptionRiskCalculationService(
        AllocationImpactService allocationImpactService)
    {
        _allocationImpactService = allocationImpactService;
    }

    public async Task<RiskCalculationResult> CalculateAsync(
        int purchaseOrderId)
    {
        var impact = await _allocationImpactService
            .CalculateImpactAsync(purchaseOrderId);

        if (impact == null)
        {
            return new RiskCalculationResult
            {
                CustomerOrders = new List<CustomerOrderContext>(),
                TotalShortageQuantity = 0,
                CalculatedSeverity = "LOW",
                RiskDate = null
            };
        }

        var calculatedSeverity =
            impact.NewlyAtRiskDemands.Any()
                ? "HIGH"
                : "LOW";

        return new RiskCalculationResult
        {
            CustomerOrders = impact.AffectedCustomerOrders
                .Select(order => new CustomerOrderContext
                {
                    Reference = order.Reference,
                    ProductId = order.ProductId,
                    Quantity = order.Quantity,
                    RequiredDate = order.RequiredDate,
                    AvailableStock = order.AvailableStock,
                    AllocatedStock = order.AllocatedStock,
                    ShortageQuantity = order.ShortageQuantity,
                    SupplierExpectedDate = order.SupplierExpectedDate,
                    SupplierDeliveryAfterRequiredDate =
                        order.SupplierDeliveryAfterRequiredDate,
                    AtRisk = order.AtRisk
                })
                .ToList(),

            TotalShortageQuantity =
                impact.AdditionalShortageQuantity,

            CalculatedSeverity =
                calculatedSeverity,

            RiskDate =
                impact.RiskDate
        };
    }
}