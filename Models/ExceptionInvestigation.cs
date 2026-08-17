namespace ExceptionAgent.Models;

public class ExceptionInvestigation
{
    public OperationalException Exception { get; set; } = null!;

    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    public Supplier Supplier { get; set; } = null!;

    public List<PurchaseOrderLine> PurchaseOrderLines { get; set; } = new();

    public List<Inventory> Inventory { get; set; } = new();

    public List<CustomerOrder> CustomerOrders { get; set; } = new();

    public List<Email> SupplierEmails { get; set; } = new();

    public List<SupplierEmailEvent> SupplierEmailEvents { get; set; } = new();
}