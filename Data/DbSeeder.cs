using ExceptionAgent.Domain.Entities;

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

            var component = new Product
            {
                Reference = "CMP-C10",
                Name = "Component C10"
            };

            

            context.Products.AddRange(motor, sensor, component);

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

            var relativeDateOrder = new PurchaseOrder
            {
                Reference = "PO-1045",
                SupplierId = supplier.Id,
                OrderDate = new DateTime(2026, 8, 16),
                ExpectedDate = new DateTime(2026, 8, 18),
                Status = "PartiallyReceived"
            };

            var unknownDateOrder = new PurchaseOrder
            {
                Reference = "PO-1046",
                SupplierId = supplier.Id,
                OrderDate = new DateTime(2026, 8, 16),
                ExpectedDate = new DateTime(2026, 8, 18),
                Status = "PartiallyReceived"
            };

            var noReferenceOrder = new PurchaseOrder
            {
                Reference = "PO-1047",
                SupplierId = supplier.Id,
                OrderDate = new DateTime(2026, 8, 16),
                ExpectedDate = new DateTime(2026, 8, 18),
                Status = "PartiallyReceived"
            };

            var ambiguousOrder = new PurchaseOrder
            {
                Reference = "PO-1048",
                SupplierId = supplier.Id,
                OrderDate = new DateTime(2026, 8, 16),
                ExpectedDate = new DateTime(2026, 8, 18),
                Status = "PartiallyReceived"
            };

            var secondAmbiguousOrder = new PurchaseOrder
            {
                Reference = "PO-1049",
                SupplierId = supplier.Id,
                OrderDate = new DateTime(2026, 8, 16),
                ExpectedDate = new DateTime(2026, 8, 18),
                Status = "PartiallyReceived"
            };

            context.PurchaseOrders.AddRange(
                delayedOrder,
                stockSufficientOrder,
                multipleCustomersOrder,
                relativeDateOrder,
                unknownDateOrder,
                noReferenceOrder,
                ambiguousOrder,
                secondAmbiguousOrder
            );

            context.SaveChanges();

            // Líneas de compra

            var delayedOrderLine = new PurchaseOrderLine
            {
                PurchaseOrderId = delayedOrder.Id,
                ProductId = motor.Id,
                OrderedQuantity = 100,
                ReceivedQuantity = 50
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

            var relativeDateOrderLine = new PurchaseOrderLine
            {
                PurchaseOrderId = relativeDateOrder.Id,
                ProductId = component.Id,
                OrderedQuantity = 25,
                ReceivedQuantity = 0
            };

            var unknownDateOrderLine = new PurchaseOrderLine
            {
                PurchaseOrderId = unknownDateOrder.Id,
                ProductId = component.Id,
                OrderedQuantity = 15,
                ReceivedQuantity = 0
            };

            var noReferenceOrderLine = new PurchaseOrderLine
            {
                PurchaseOrderId = noReferenceOrder.Id,
                ProductId = component.Id,
                OrderedQuantity = 30,
                ReceivedQuantity = 0
            };

            var ambiguousOrderLine = new PurchaseOrderLine
            {
                PurchaseOrderId = ambiguousOrder.Id,
                ProductId = component.Id,
                OrderedQuantity = 40,
                ReceivedQuantity = 0
            };

            var secondAmbiguousOrderLine = new PurchaseOrderLine
            {
                PurchaseOrderId = secondAmbiguousOrder.Id,
                ProductId = component.Id,
                OrderedQuantity = 40,
                ReceivedQuantity = 0
            };



            context.PurchaseOrderLines.AddRange(
                delayedOrderLine,
                stockSufficientOrderLine,
                multipleCustomersOrderLine,
                relativeDateOrderLine,
                unknownDateOrderLine,
                noReferenceOrderLine,
                ambiguousOrderLine,
                secondAmbiguousOrderLine
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

            var componentInventory = new Inventory
            {
                ProductId = component.Id,
                AvailableQuantity = 0
            };


            context.Inventories.AddRange(
                motorInventory,
                sensorInventory,
                componentInventory
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

            var customerOrder5 = new CustomerOrder
            {
                Reference = "CO-8825",
                ProductId = component.Id,
                Quantity = 20,
                RequiredDate = new DateTime(2026, 8, 18)
            };

            var unknownDateCustomerOrder = new CustomerOrder
            {
                Reference = "CO-8826",
                ProductId = component.Id,
                Quantity = 20,
                RequiredDate = new DateTime(2026, 8, 19)
            };

            var noReferenceCustomerOrder = new CustomerOrder
            {
                Reference = "CO-8827",
                ProductId = component.Id,
                Quantity = 20,
                RequiredDate = new DateTime(2026, 8, 19)
            };

            context.CustomerOrders.AddRange(
                customerOrder1,
                customerOrder2,
                customerOrder3,
                customerOrder4,
                customerOrder5,
                unknownDateCustomerOrder,
                noReferenceCustomerOrder
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
                    "Buenos días. Las 50 unidades restantes del pedido PO-1042 sufrirán un retraso y llegarán el 20/08. Un saludo.",
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

            var email1045 = new Email
            {
                Sender = "compras@abcindustrial.es",
                Recipient = "compras@nuestraempresa.es",
                Subject = "Actualización de entrega",
                Date = new DateTime(2026, 8, 16),
                Body =
                    "Buenos días. Finalmente no podremos cumplir la fecha prevista del pedido PO-1045. Las 25 unidades llegarán el próximo miércoles. Un saludo.",
                SupplierId = supplier.Id
            };

            var email1046 = new Email
            {
                Sender = "compras@abcindustrial.es",
                Recipient = "compras@nuestraempresa.es",
                Subject = "Retraso PO-1046",
                Date = new DateTime(2026, 8, 16),
                Body =
                    "Buenos días. La entrega del pedido PO-1046 sufrirá un retraso de unos días. Os avisaremos cuando tengamos una nueva fecha. Un saludo.",
                SupplierId = supplier.Id
            };

            var email1047 = new Email
            {
                Sender = "compras@abcindustrial.es",
                Recipient = "compras@nuestraempresa.es",
                Subject = "Actualización de entrega",
                Date = new DateTime(2026, 8, 16),
                Body =
                    "Buenos días. Las 30 unidades restantes sufrirán un retraso y llegarán el 20/08. Un saludo.",
                SupplierId = supplier.Id
            };

            var ambiguousEmail = new Email
            {
                Sender = "compras@abcindustrial.es",
                Recipient = "compras@nuestraempresa.es",
                Subject = "Actualización de entrega",
                Date = new DateTime(2026, 8, 16),
                Body =
                    "Buenos días. Las 40 unidades restantes sufrirán un retraso y llegarán el 20/08. Un saludo.",
                SupplierId = supplier.Id
            };

            context.Emails.AddRange(
                email1042,
                email1043,
                email1044,
                email1045,
                email1046,
                email1047,
                ambiguousEmail
            );

            context.SaveChanges();
        }
    }
}