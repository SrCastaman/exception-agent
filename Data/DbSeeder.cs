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

            // --------------------------------------------------
            // PO-1042
            // Retraso + stock insuficiente + 1 cliente afectado
            // --------------------------------------------------

            var delayedOrder = new PurchaseOrder
            {
                Reference = "PO-1042",
                SupplierId = supplier.Id,
                OrderDate = new DateTime(2026, 8, 10),
                ExpectedDate = new DateTime(2026, 8, 15),
                Status = "PartiallyReceived"
            };

            // --------------------------------------------------
            // PO-1043
            // Retraso + stock suficiente
            // --------------------------------------------------

            var stockSufficientOrder = new PurchaseOrder
            {
                Reference = "PO-1043",
                SupplierId = supplier.Id,
                OrderDate = new DateTime(2026, 8, 11),
                ExpectedDate = new DateTime(2026, 8, 15),
                Status = "PartiallyReceived"
            };

            // --------------------------------------------------
            // PO-1044
            // Retraso + stock insuficiente + 2 clientes afectados
            // --------------------------------------------------

            var multipleCustomersOrder = new PurchaseOrder
            {
                Reference = "PO-1044",
                SupplierId = supplier.Id,
                OrderDate = new DateTime(2026, 8, 12),
                ExpectedDate = new DateTime(2026, 8, 16),
                Status = "PartiallyReceived"
            };

            context.PurchaseOrders.AddRange(
                delayedOrder,
                stockSufficientOrder,
                multipleCustomersOrder
            );

            context.SaveChanges();

            // Líneas de compra

            var delayedOrderLine = new PurchaseOrderLine
            {
                PurchaseOrderId = delayedOrder.Id,
                ProductId = motor.Id,
                OrderedQuantity = 100,
                ReceivedQuantity = 60
            };

            var stockSufficientOrderLine = new PurchaseOrderLine
            {
                PurchaseOrderId = stockSufficientOrder.Id,
                ProductId = sensor.Id,
                OrderedQuantity = 100,
                ReceivedQuantity = 40
            };

            var multipleCustomersOrderLine = new PurchaseOrderLine
            {
                PurchaseOrderId = multipleCustomersOrder.Id,
                ProductId = motor.Id,
                OrderedQuantity = 100,
                ReceivedQuantity = 20
            };

            context.PurchaseOrderLines.AddRange(
                delayedOrderLine,
                stockSufficientOrderLine,
                multipleCustomersOrderLine
            );

            // Inventario

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

            // --------------------------------------------------
            // PEDIDOS DE CLIENTE
            // --------------------------------------------------

            // Caso PO-1042
            var customerOrder1 = new CustomerOrder
            {
                Reference = "CO-8821",
                ProductId = motor.Id,
                Quantity = 25,
                RequiredDate = new DateTime(2026, 8, 19)
            };

            // Caso PO-1043
            // Necesita 50 y hay 80 en stock -> NO debería estar en riesgo
            var customerOrder2 = new CustomerOrder
            {
                Reference = "CO-8822",
                ProductId = sensor.Id,
                Quantity = 50,
                RequiredDate = new DateTime(2026, 8, 19)
            };

            // Caso PO-1044
            var customerOrder3 = new CustomerOrder
            {
                Reference = "CO-8823",
                ProductId = motor.Id,
                Quantity = 30,
                RequiredDate = new DateTime(2026, 8, 18)
            };

            var customerOrder4 = new CustomerOrder
            {
                Reference = "CO-8824",
                ProductId = motor.Id,
                Quantity = 20,
                RequiredDate = new DateTime(2026, 8, 19)
            };

            context.CustomerOrders.AddRange(
                customerOrder1,
                customerOrder2,
                customerOrder3,
                customerOrder4
            );

            // --------------------------------------------------
            // EMAILS DEL PROVEEDOR
            // --------------------------------------------------

            var email1042 = new Email
            {
                Sender = "compras@abcindustrial.es",
                Recipient = "compras@nuestraempresa.es",
                Subject = "Retraso PO-1042",
                Date = new DateTime(2026, 8, 16),
                Body =
                    "Buenos días. Las 40 unidades restantes del pedido PO-1042 sufrirán un retraso y llegarán el 20/08. Un saludo.",
                SupplierId = supplier.Id
            };

            // PO-1043:
            // llega el 20/08, pero hay 80 unidades en stock
            var email1043 = new Email
            {
                Sender = "compras@abcindustrial.es",
                Recipient = "compras@nuestraempresa.es",
                Subject = "Retraso PO-1043",
                Date = new DateTime(2026, 8, 16),
                Body =
                    "Buenos días. El pedido PO-1043 sufrirá un retraso. Las 60 unidades pendientes llegarán el 20/08. Un saludo.",
                SupplierId = supplier.Id
            };

            // PO-1044:
            // llega el 21/08 y hay dos clientes que necesitan el producto antes
            var email1044 = new Email
            {
                Sender = "compras@abcindustrial.es",
                Recipient = "compras@nuestraempresa.es",
                Subject = "Retraso PO-1044",
                Date = new DateTime(2026, 8, 16),
                Body =
                    "Buenos días. Las 80 unidades restantes del pedido PO-1044 sufrirán un retraso y llegarán el 21/08. Un saludo.",
                SupplierId = supplier.Id
            };

            context.Emails.AddRange(
                email1042,
                email1043,
                email1044
            );

            context.SaveChanges();
        }
    }
}