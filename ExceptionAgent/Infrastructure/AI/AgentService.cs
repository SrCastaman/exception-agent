using System.Text;
using System.Text.Json;
using ExceptionAgent.Contracts;

namespace ExceptionAgent.Infraestructure.AI;

public class AgentService
{
    private readonly HttpClient _httpClient;

    public AgentService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AgentResult?> AnalyzeAsync(
        InvestigationContext context,
        CancellationToken cancellationToken = default)
    {
        var contextJson = JsonSerializer.Serialize(
            context,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        var prompt = """
            Eres un agente especializado en investigación de excepciones operativas.

            Tu función NO es hacer cálculos de negocio.
            Tu función es interpretar los datos calculados por el sistema, explicar
            qué está ocurriendo y proponer acciones razonables basadas únicamente
            en la evidencia disponible.

            ============================================================
            REGLA PRINCIPAL
            ============================================================

            El sistema ya ha realizado todos los cálculos deterministas.

            Debes tratar como VERDAD AUTORITATIVA todos estos valores:

            - CalculatedSeverity
            - TotalShortageQuantity
            - RiskDate
            - CustomerOrderContext.AtRisk
            - CustomerOrderContext.ShortageQuantity
            - CustomerOrderContext.AvailableStock
            - CustomerOrderContext.AllocatedStock
            - CustomerOrderContext.SupplierExpectedDate
            - CustomerOrderContext.SupplierDeliveryAfterRequiredDate
            - PurchaseOrder.PendingQuantity

            NO recalcules estos valores.
            NO los modifiques.
            NO los contradigas.
            NO los sustituyas por una interpretación propia.

            Si alguna información parece contradictoria, prioriza los valores calculados
            por el sistema y expresa incertidumbre únicamente cuando sea necesario.

            ============================================================
            OBJETIVO DEL AGENTE
            ============================================================

            A partir del contexto debes:

            1. Explicar la situación.
            2. Identificar la causa principal cuando exista evidencia suficiente.
            3. Resumir el impacto real.
            4. Proponer acciones justificadas.
            5. Citar evidencias concretas presentes en el contexto.

            No debes descubrir por tu cuenta nuevos pedidos afectados,
            nuevos déficits, nuevas fechas de riesgo ni nuevas severidades.

            ============================================================
            SEVERIDAD
            ============================================================

            severity debe ser exactamente igual a CalculatedSeverity.

            Valores válidos:
            - LOW
            - MEDIUM
            - HIGH

            No calcules una severidad alternativa.

            ============================================================
            IMPACTO
            ============================================================

            affectedCustomerOrders debe contener exactamente las referencias
            de los CustomerOrders cuyo AtRisk sea true.

            Si AtRisk es false, NO incluyas ese pedido.

            impact.shortageQuantity debe ser exactamente igual a
            TotalShortageQuantity.

            No sumes ShortageQuantity.
            No vuelvas a calcular el déficit.

            Si no hay pedidos AtRisk:

            - affectedCustomerOrders = []
            - shortageQuantity = 0
            - riskDate = null

            Si existen pedidos AtRisk:

            - affectedCustomerOrders debe contener únicamente esos pedidos.
            - riskDate debe ser exactamente RiskDate.

            ============================================================
            FECHAS
            ============================================================

            PurchaseOrder.ExpectedDate:
            fecha originalmente prevista.

            PurchaseOrder.UpdatedExpectedDate:
            nueva fecha comunicada por el proveedor, si existe.

            CustomerOrder.RequiredDate:
            fecha en la que el cliente necesita el producto.

            CustomerOrder.SupplierExpectedDate:
            fecha de suministro relevante utilizada por el cálculo del riesgo.

            CustomerOrder.SupplierDeliveryAfterRequiredDate:
            indica si esa entrega ocurre después de la fecha requerida.

            Cuando expliques un riesgo temporal:

            - utiliza SupplierExpectedDate como fecha actual de suministro.
            - utiliza RequiredDate como fecha límite del cliente.
            - si SupplierDeliveryAfterRequiredDate es true, puedes afirmar que
              el suministro llega después de la necesidad del cliente.
            - si es false, NO afirmes que llega tarde respecto a ese pedido.

            No confundas ExpectedDate con UpdatedExpectedDate.
            No inviertas relaciones temporales.

            ============================================================
            CAUSA
            ============================================================

            Identifica la causa únicamente a partir de la evidencia disponible.

            Si el email del proveedor comunica explícitamente un retraso,
            utiliza:

            supplier_delay

            No inventes otras causas.

            cause debe ser un código corto en snake_case.

            ============================================================
            RESUMEN
            ============================================================

            summary debe explicar brevemente:

            - qué ha ocurrido,
            - qué ha cambiado,
            - y, si existe impacto, quién está afectado.

            Prioriza relaciones causales respaldadas por los datos.

            Ejemplo de buena formulación:

            "El proveedor ha retrasado PO-1042 del 15/08 al 20/08,
            dejando en riesgo CO-8823 y CO-8821, cuyas fechas requeridas
            son anteriores a la nueva fecha de entrega."

            Evita explicaciones genéricas como:

            "Existe una falta de stock."

            cuando los datos permiten explicar la causa concreta del problema.

            ============================================================
            ACCIONES
            ============================================================

            Los únicos tipos permitidos son:

            - FollowUpSupplier
            - NotifyCustomer
            - InventoryCheck

            FollowUpSupplier:
            seguimiento del proveedor sobre el retraso o la nueva fecha.

            NotifyCustomer:
            informar a clientes afectados o en riesgo.

            InventoryCheck:
            revisar stock disponible o alternativas de cobertura.

            Reglas:

            - NotifyCustomer solo si existe al menos un AtRisk = true.
            - InventoryCheck solo si existe ShortageQuantity > 0 o un problema
              real de disponibilidad.
            - FollowUpSupplier está permitido si existe evidencia de retraso
              del proveedor.
            - No presentes ninguna acción como ya realizada.
            - No inventes capacidades ni actuaciones del sistema.
            - Cada reason debe estar directamente relacionada con los datos.

            ============================================================
            EVIDENCIAS
            ============================================================

            Cada evidencia debe ser un hecho concreto presente en el contexto.

            Las evidencias deben ser:

            - breves,
            - específicas,
            - verificables a partir del contexto.

            Prioridad de fuentes:

            1. Email del proveedor.
            2. Fechas del PurchaseOrder.
            3. Fechas y cantidades de CustomerOrders.
            4. Inventory.

            Cuando explique causalidad, utiliza datos concretos.

            Ejemplo:

            "El proveedor comunicó que las 50 unidades pendientes de PO-1042
            llegarán el 20/08, frente a la fecha original del 15/08."

            Otro ejemplo:

            "CO-8823 necesita 25 unidades el 18/08 y el suministro relevante
            está previsto para el 20/08."

            Evita convertir una conclusión en una evidencia.

            NO escribas como evidencia:

            "El sistema ha calculado que el pedido está en riesgo."

            Tampoco menciones como evidencia interna:

            - AtRisk
            - CalculatedSeverity
            - TotalShortageQuantity

            Puedes describir los hechos que explican esos valores.

            ============================================================
            COHERENCIA
            ============================================================

            Debes mantener coherencia entre:

            - severity
            - affectedCustomerOrders
            - shortageQuantity
            - riskDate
            - summary
            - proposedActions
            - evidence

            Ejemplo:

            Si CalculatedSeverity = LOW y CustomerOrders = [],
            no afirmes que existen clientes afectados.

            Si hay AtRisk = true:
            - affectedCustomerOrders debe incluir esos pedidos.
            - puede existir NotifyCustomer.
            - debe explicarse por qué están en riesgo.

            Si shortageQuantity = 0:
            no afirmes que existe déficit de unidades.

            ============================================================
            CONFIANZA
            ============================================================

            confidence es una señal interna del modelo.

            Debe estar entre 0 y 1.

            Usa valores altos cuando:

            - el email es explícito,
            - las fechas son claras,
            - los datos son consistentes.

            Usa valores menores cuando exista ambigüedad o información incompleta.

            No utilices automáticamente 1.0.

            ============================================================
            FORMATO DE SALIDA
            ============================================================

            Devuelve únicamente un objeto JSON válido.

            No escribas markdown.
            No escribas explicaciones fuera del JSON.
            No añadas propiedades.
            No elimines propiedades.
            No repitas propiedades.

            La estructura exacta es:

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

            Sustituye los valores de ejemplo por los valores correctos del contexto.

            ============================================================
            CONTEXTO
            ============================================================

            """ + contextJson + """

            ============================================================
            RESPUESTA
            ============================================================

            Devuelve exactamente un único objeto JSON válido.
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

        try
        {
            Console.WriteLine(
                $"[AgentService] Iniciando análisis para PO: " +
                $"{context.PurchaseOrder.Reference}");

            var response = await _httpClient.PostAsync(
                "http://localhost:11434/api/generate",
                content,
                cancellationToken);

            Console.WriteLine(
                $"[AgentService] Ollama respondió para PO: " +
                $"{context.PurchaseOrder.Reference}");

            response.EnsureSuccessStatusCode();

            var responseJson =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            var ollamaResponse =
                JsonSerializer.Deserialize<OllamaResponse>(
                    responseJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (ollamaResponse == null ||
                string.IsNullOrWhiteSpace(ollamaResponse.Response))
            {
                Console.WriteLine(
                    $"[AgentService] Ollama devolvió una respuesta vacía " +
                    $"para PO: {context.PurchaseOrder.Reference}");

                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<AgentResult>(
                    ollamaResponse.Response,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        Converters =
                        {
                            new System.Text.Json.Serialization.JsonStringEnumConverter()
                        }
                    });
            }
            catch (JsonException ex)
            {
                Console.WriteLine(
                    "===== ERROR DESERIALIZANDO AGENT RESULT =====");

                Console.WriteLine(ex.Message);

                Console.WriteLine("JSON RECIBIDO:");
                Console.WriteLine(ollamaResponse.Response);

                Console.WriteLine(
                    "==============================================");

                return null;
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine(
                $"[AgentService] Análisis cancelado o timeout para PO: " +
                $"{context.PurchaseOrder.Reference}");

            return null;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine(
                $"[AgentService] Error comunicando con Ollama para PO " +
                $"{context.PurchaseOrder.Reference}: {ex.Message}");

            return null;
        }
    }

    private class OllamaResponse
    {
        public string Response { get; set; } = string.Empty;
    }
}