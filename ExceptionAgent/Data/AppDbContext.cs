using ExceptionAgent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExceptionAgent.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Supplier> Suppliers { get; set; }

    public DbSet<Product> Products { get; set; }

    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }

    public DbSet<PurchaseOrderLine> PurchaseOrderLines { get; set; }

    public DbSet<Inventory> Inventories { get; set; }

    public DbSet<CustomerOrder> CustomerOrders { get; set; }

    public DbSet<Email> Emails { get; set; }

    public DbSet<SupplierEmailEvent> SupplierEmailEvents { get; set; }

    public DbSet<OperationalException> OperationalExceptions { get; set; }

    public DbSet<ExceptionEvidence> ExceptionEvidences { get; set; }

    public DbSet<EmailProcessingResult> EmailProcessingResults => Set<EmailProcessingResult>();



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PurchaseOrder>()
            .HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseOrderLine>()
            .HasOne<Product>()
            .WithMany()
            .HasForeignKey(p => p.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseOrderLine>()
            .HasOne<PurchaseOrder>()
            .WithMany(p => p.Lines)
            .HasForeignKey(p => p.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Inventory>()
            .HasOne<Product>()
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CustomerOrder>()
            .HasOne<Product>()
            .WithMany()
            .HasForeignKey(c => c.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Email>()
            .HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(e => e.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<SupplierEmailEvent>()
            .HasOne<Email>()
            .WithMany()
            .HasForeignKey(e => e.EmailId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SupplierEmailEvent>()
            .HasOne<PurchaseOrder>()
            .WithMany()
            .HasForeignKey(e => e.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OperationalException>()
            .HasOne(e => e.PurchaseOrder)
            .WithMany()
            .HasForeignKey(e => e.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ExceptionEvidence>()
            .HasOne<OperationalException>()
            .WithMany()
            .HasForeignKey(e => e.OperationalExceptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}