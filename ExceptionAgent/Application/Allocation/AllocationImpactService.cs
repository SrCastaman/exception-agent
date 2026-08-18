using ExceptionAgent.Application.Allocation.Models;
using ExceptionAgent.Data;
using ExceptionAgent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExceptionAgent.Application.Allocation;

public class AllocationImpactService
{
    private readonly AppDbContext _context;
    private readonly AllocationScenarioService _scenarioService;
    private readonly AllocationEngine _allocationEngine;
    private readonly ScenarioImpactCalculator _impactCalculator;

    public AllocationImpactService(
        AppDbContext context,
        AllocationScenarioService scenarioService,
        AllocationEngine allocationEngine,
        ScenarioImpactCalculator impactCalculator)
    {
        _context = context;
        _scenarioService = scenarioService;
        _allocationEngine = allocationEngine;
        _impactCalculator = impactCalculator;
    }

    public async Task<ScenarioImpactResult?> CalculateImpactAsync(
        int purchaseOrderId)
    {
        var purchaseOrder = await _context.PurchaseOrders
            .Include(po => po.Lines)
            .SingleOrDefaultAsync(po => po.Id == purchaseOrderId);

        if (purchaseOrder == null)
        {
            return null;
        }

        var scenarios = await _scenarioService
            .BuildScenariosAsync(purchaseOrder);

        if (scenarios == null)
        {
            return null;
        }

        var currentResult = _allocationEngine.Allocate(
            scenarios.Current.Supplies,
            scenarios.Current.Demands);

        var withoutInvestigatedDelayResult =
            _allocationEngine.Allocate(
                scenarios.WithoutInvestigatedDelay.Supplies,
                scenarios.WithoutInvestigatedDelay.Demands);

        return _impactCalculator.Compare(
            withoutInvestigatedDelayResult,
            currentResult,
            scenarios.Current,
            purchaseOrder.Reference);
    }
}