using ExceptionAgent.Domain.Entities;

namespace ExceptionAgent.Application.Allocation.Models;

public class AllocationData
{
    public List<PurchaseOrder> PurchaseOrders { get; set; } = new();

    public List<Inventory> Inventories { get; set; } = new();

    public List<CustomerOrder> CustomerOrders { get; set; } = new();

    public List<SupplierEmailEvent> SupplierEmailEvents { get; set; } = new();
}