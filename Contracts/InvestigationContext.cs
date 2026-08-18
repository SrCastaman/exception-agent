namespace ExceptionAgent.Contracts;

public class InvestigationContext
{
    public ExceptionContext Exception { get; set; } = new();

    public PurchaseOrderContext PurchaseOrder { get; set; } = new();

    public SupplierContext Supplier { get; set; } = new();

    public List<InventoryContext> Inventory { get; set; } = new();

    public List<CustomerOrderContext> CustomerOrders { get; set; } = new();

    public List<EmailContext> Emails { get; set; } = new();

    public string CalculatedSeverity { get; set; } = string.Empty;

    public int TotalShortageQuantity { get; set; }

    public DateTime? RiskDate { get; set; }
}

public class ExceptionContext
{
    public string Type { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}

public class PurchaseOrderContext
{
    public string Reference { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; }

    public DateTime ExpectedDate { get; set; }

    public DateTime? UpdatedExpectedDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public int OrderedQuantity { get; set; }

    public int ReceivedQuantity { get; set; }

    public int PendingQuantity { get; set; }
}

public class SupplierContext
{
    public string Name { get; set; } = string.Empty;
}

public class InventoryContext
{
    public int ProductId { get; set; }

    public int AvailableQuantity { get; set; }
}

public class CustomerOrderContext
{
    public string Reference { get; set; } = string.Empty;

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public DateTime RequiredDate { get; set; }
    
    public int AvailableStock { get; set; }

    public int AllocatedStock { get; set; }

    public int ShortageQuantity { get; set; }

    public DateTime SupplierExpectedDate { get; set; }

    public bool SupplierDeliveryAfterRequiredDate { get; set; }

    public bool AtRisk { get; set; }
}

public class EmailContext
{
    public string Sender { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public string Body { get; set; } = string.Empty;
}