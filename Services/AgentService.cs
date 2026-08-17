using System.Text;
using System.Text.Json;
using ExceptionAgent.Models;

namespace ExceptionAgent.Services;

public class AgentService
{
    private readonly HttpClient _httpClient;

    public AgentService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AgentResult?> AnalyzeAsync(
        InvestigationContext context)
    {
        var contextJson = JsonSerializer.Serialize(
            context,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        var prompt = """
            Eres un agente de investigación de excepciones operativas.

            Tu tarea es interpretar el contexto proporcionado y generar un diagnóstico
            basado exclusivamente en los datos disponibles.

            PRINCIPIO FUNDAMENTAL:

            El sistema ya ha realizado los cálculos deterministas y las reglas de negocio.
            Tú NO debes volver a calcularlos, modificarlos ni contradecirlos.
            Tu función es interpretar esos resultados y explicar la situación.

            REGLAS GENERALES:

            - Utiliza exclusivamente la información proporcionada en el contexto.
            - No inventes información.
            - No hagas suposiciones no respaldadas por los datos.
            - No inventes pedidos, productos, cantidades, fechas, proveedores, eventos
              ni acciones realizadas.
            - Devuelve únicamente un objeto JSON válido.
            - No escribas markdown.
            - No añadas propiedades fuera de la estructura indicada.
            - No repitas propiedades.
            - La respuesta debe contener exactamente las propiedades especificadas.

            DATOS CALCULADOS POR EL SISTEMA:

            - PurchaseOrder.PendingQuantity ya ha sido calculado.
            - CustomerOrderContext.AvailableStock ya ha sido calculado.
            - CustomerOrderContext.AllocatedStock ya ha sido calculado.
            - CustomerOrderContext.ShortageQuantity ya ha sido calculado.
            - CustomerOrderContext.AtRisk ya ha sido calculado.
            - CustomerOrderContext.SupplierExpectedDate contiene la fecha de entrega
              relevante utilizada por el sistema.
            - CustomerOrderContext.SupplierDeliveryAfterRequiredDate ya ha sido calculado.
            - CalculatedSeverity ya ha sido calculada por el sistema.
            - TotalShortageQuantity ya ha sido calculado por el sistema.

            Nunca vuelvas a calcular ninguno de estos valores.
            Nunca los modifiques ni los contradigas.

            SEVERIDAD:

            - La propiedad severity debe ser exactamente igual a CalculatedSeverity.
            - No calcules una severidad diferente.
            - Los únicos valores válidos son LOW, MEDIUM o HIGH.

            IMPACTO:

            - affectedCustomerOrders debe contener exactamente las referencias de
              los pedidos cuyo AtRisk sea true.
            - No incluyas pedidos cuyo AtRisk sea false.
            - shortageQuantity debe ser exactamente igual a TotalShortageQuantity.
            - No sumes ShortageQuantity por tu cuenta.
            - No vuelvas a calcular el déficit.
            - Si no existe ningún pedido con AtRisk = true:
              - affectedCustomerOrders debe ser [].
              - shortageQuantity debe ser 0.
              - riskDate debe ser null.
            - Si existen pedidos con AtRisk = true:
              - riskDate debe ser la RequiredDate más próxima entre esos pedidos.
            - No inventes una fecha de riesgo.

            FECHAS:

            - PurchaseOrder.ExpectedDate es la fecha original esperada.
            - SupplierExpectedDate es la fecha de entrega relevante para la situación actual.
            - Utiliza SupplierExpectedDate cuando describas la entrega actual del proveedor.
            - RequiredDate es la fecha requerida por el pedido de cliente.
            - No confundas estas fechas.
            - No inviertas nunca una relación temporal.
            - Si SupplierExpectedDate es posterior a RequiredDate, indica que la entrega
              del proveedor es posterior a la fecha requerida.
            - Si SupplierExpectedDate es anterior o igual a RequiredDate, no afirmes que
              la entrega del proveedor es posterior.

            CAUSA:

            - Identifica la causa principal únicamente a partir de la evidencia disponible.
            - Prioriza información explícita de los emails.
            - Si el proveedor comunica explícitamente un retraso, utiliza:
              "supplier_delay".
            - cause debe ser un código corto en snake_case.
            - No inventes causas.

            RESUMEN:

            - summary debe explicar brevemente qué está ocurriendo.
            - Debe basarse únicamente en hechos del contexto.
            - Si existen pedidos AtRisk, explica brevemente su relación con el retraso.
            - Si no existen pedidos AtRisk, no afirmes que los clientes están afectados.

            ACCIONES:

            Los únicos tipos permitidos son:

            - FollowUpSupplier
            - NotifyCustomer
            - InventoryCheck

            Significado:

            - FollowUpSupplier: hacer seguimiento con el proveedor.
            - NotifyCustomer: informar a un cliente afectado o en riesgo.
            - InventoryCheck: revisar stock o alternativas de inventario.

            REGLAS DE ACCIONES:

            - No inventes otros tipos.
            - NotifyCustomer solo puede aparecer si existe al menos un pedido AtRisk = true.
            - InventoryCheck solo puede aparecer si existe al menos un pedido con
              ShortageQuantity > 0 o un problema real de disponibilidad.
            - FollowUpSupplier puede aparecer cuando exista evidencia de un retraso
              del proveedor.
            - Cada reason debe explicar por qué la acción está justificada.
            - No describas acciones como realizadas.
            - No inventes capacidades del sistema.

            EVIDENCIAS:

            - Cada evidencia debe describir un hecho concreto presente en el contexto.
            - No inventes evidencias.
            - No presentes una conclusión del agente como si fuera una evidencia.
            - Las evidencias deben ser breves y específicas.
            - No menciones como evidencia propiedades internas del sistema como:
              AtRisk, CalculatedSeverity o TotalShortageQuantity.
            - Evita expresiones del tipo "el sistema ha calculado...".
            - Cuando sea posible, utiliza los datos originales que justifican el resultado:
              stock disponible, cantidad requerida, fecha requerida, fecha de entrega,
              cantidad pendiente y contenido del email.
            - Si comparas fechas, utiliza correctamente cuál es anterior y cuál es posterior.

            CONFIANZA:

            - confidence es una señal interna del modelo.
            - Debe ser un número entre 0 y 1.
            - No representa una probabilidad estadística.
            - Utiliza valores altos cuando la evidencia sea clara y consistente.
            - Utiliza valores menores cuando exista información ambigua, incompleta
              o contradictoria.
            - No utilices automáticamente 1.0.

            CONTEXTO:

            """ + contextJson + """

            Devuelve EXACTAMENTE un único objeto JSON con esta estructura:

            {
              "severity": "LOW",
              "cause": "supplier_delay",
              "summary": "",
              "confidence": 0.0,
              "impact": {
                "affectedCustomerOrders": [],
                "shortageQuantity": 0,
                "riskDate": null
              },
              "proposedActions": [
                {
                  "type": "FollowUpSupplier",
                  "reason": ""
                }
              ],
              "evidence": [
                {
                  "description": ""
                }
              ]
            }

            RESTRICCIONES FINALES:

            - Los valores del ejemplo son únicamente ejemplos de estructura.
            - Sustitúyelos por los valores correspondientes al contexto.
            - Mantén exactamente los nombres de las propiedades.
            - No añadas propiedades.
            - No elimines propiedades.
            - No repitas propiedades.
            - No escribas texto fuera del objeto JSON.
            - La respuesta debe ser un único objeto JSON válido y completo.
            """;

        var request = new
        {
            model = "qwen3:8b",
            prompt = prompt,
            stream = false,
            format = "json"
        };

        var json = JsonSerializer.Serialize(request);

        using var content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(
            "http://localhost:11434/api/generate",
            content);

        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();


        var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(
            responseJson,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

        if (ollamaResponse == null ||
            string.IsNullOrWhiteSpace(ollamaResponse.Response))
        {
            return null;
        }

        try
        {
            var result = JsonSerializer.Deserialize<AgentResult>(
                ollamaResponse.Response,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters =
                    {
                        new System.Text.Json.Serialization.JsonStringEnumConverter()
                    }
                });

            return result;
        }
        catch (JsonException ex)
        {
            Console.WriteLine("===== ERROR DESERIALIZANDO AGENT RESULT =====");
            Console.WriteLine(ex.Message);
            Console.WriteLine("JSON RECIBIDO:");
            Console.WriteLine(ollamaResponse.Response);
            Console.WriteLine("==============================================");

            return null;
        }
    }

    private class OllamaResponse
    {
        public string Response { get; set; } = string.Empty;
    }
}