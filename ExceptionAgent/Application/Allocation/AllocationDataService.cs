using ExceptionAgent.Application.Allocation.Models;
using ExceptionAgent.Data;
using ExceptionAgent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExceptionAgent.Application.Allocation;

public class AllocationDataService
{
    private readonly AppDbContext _context;

    public AllocationDataService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AllocationData> GetDataAsync(
        IEnumerable<int> productIds)
    {
        var ids = productIds
            .Distinct()
            .ToList();

        var purchaseOrders = await _context.PurchaseOrders
            .Where(p => p.Lines.Any(line =>
                ids.Contains(line.ProductId)))
            .Include(p => p.Lines)
            .ToListAsync();

        var inventories = await _context.Inventories
            .Where(i => ids.Contains(i.ProductId))
            .ToListAsync();

        var customerOrders = await _context.CustomerOrders
            .Where(c => ids.Contains(c.ProductId))
            .ToListAsync();

        var purchaseOrderIds = purchaseOrders
            .Select(p => p.Id)
            .ToList();

        var supplierEmailEvents = await _context.SupplierEmailEvents
            .Where(e => purchaseOrderIds.Contains(e.PurchaseOrderId))
            .OrderByDescending(e => e.Id)
            .ToListAsync();

        return new AllocationData
        {
            PurchaseOrders = purchaseOrders,
            Inventories = inventories,
            CustomerOrders = customerOrders,
            SupplierEmailEvents = supplierEmailEvents
        };
    }
}