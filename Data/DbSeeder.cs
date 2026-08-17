using ExceptionAgent.Models;

namespace ExceptionAgent.Data
{
    public static class DbSeeder
    {
        public static void Seed(AppDbContext context)
        {
            if (context.Suppliers.Any())
            {
                return;
            }

            var supplier = new Supplier
            {
                Name = "ABC Industrial",
                Email = "compras@abcindustrial.es"
            };

            context.Suppliers.Add(supplier);

            var motor = new Product
            {
                Reference = "MOT-X200",
                Name = "Motor X200"
            };

            var sensor = new Product
            {
                Reference = "SEN-S50",
                Name = "Sensor S50"
            };

            context.Products.AddRange(motor, sensor);

            context.SaveChanges();

            var delayedOrder = new PurchaseOrder
            {
                Reference = "PO-1042",
                SupplierId = supplier.Id,
                OrderDate = new DateTime(2026, 8, 10),
                ExpectedDate = new DateTime(2026, 8, 15),
                Status = "PartiallyReceived"
            };

            var normalOrder = new PurchaseOrder
            {
                Reference = "PO-1043",
                SupplierId = supplier.Id,
                OrderDate = new DateTime(2026, 8, 14),
                ExpectedDate = new DateTime(2026, 8, 20),
                Status = "Confirmed"
            };

            context.PurchaseOrders.AddRange(delayedOrder, normalOrder);

            context.SaveChanges();

            var delayedOrderLine = new PurchaseOrderLine
            {
                PurchaseOrderId = delayedOrder.Id,
                ProductId = motor.Id,
                OrderedQuantity = 100,
                ReceivedQuantity = 60
            };

            var normalOrderLine = new PurchaseOrderLine
            {
                PurchaseOrderId = normalOrder.Id,
                ProductId = sensor.Id,
                OrderedQuantity = 50,
                ReceivedQuantity = 0
            };

            context.PurchaseOrderLines.AddRange(
                delayedOrderLine,
                normalOrderLine
            );


            var motorInventory = new Inventory
            {
                ProductId = motor.Id,
                AvailableQuantity = 5
            };

            var sensorInventory = new Inventory
            {
                ProductId = sensor.Id,
                AvailableQuantity = 80
            };

            context.Inventories.AddRange(
                motorInventory,
                sensorInventory
            );

            var customerOrder = new CustomerOrder
            {
                Reference = "CO-8821",
                ProductId = motor.Id,
                Quantity = 25,
                RequiredDate = new DateTime(2026, 8, 19)
            };

            context.CustomerOrders.Add(customerOrder);


            var supplierEmail = new Email
            {
                Sender = "compras@abcindustrial.es",
                Recipient = "compras@nuestraempresa.es",
                Subject = "Retraso PO-1042",
                Date = new DateTime(2026, 8, 16),
                Body = "Buenos días. Las 40 unidades restantes del pedido PO-1042 sufrirán un retraso y llegarán el 20/08. Un saludo.",
                SupplierId = supplier.Id
            };

            context.Emails.Add(supplierEmail);

            context.SaveChanges();
        }
    }
}
