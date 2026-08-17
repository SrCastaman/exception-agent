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

            Tu tarea es analizar el contexto proporcionado y generar un diagnóstico de la excepción.

            PRINCIPIO FUNDAMENTAL:

            El sistema ha realizado previamente los cálculos objetivos que puede resolver de forma determinista.
            Tú debes interpretar esos datos y relacionarlos con la información no estructurada, como los emails del proveedor.

            REGLAS GENERALES:

            - Utiliza EXCLUSIVAMENTE la información proporcionada en el contexto.
            - No inventes información.
            - No hagas suposiciones que no estén respaldadas por los datos.
            - No inventes pedidos, productos, cantidades, fechas, proveedores ni acciones realizadas.
            - Devuelve únicamente JSON válido.
            - No escribas markdown.
            - No añadas propiedades fuera de la estructura especificada.
            - severity debe ser exactamente LOW, MEDIUM o HIGH.
            - confidence es una señal interna de confianza del modelo en su diagnóstico.
            - Debe ser un número entre 0 y 1.
            - No representa una probabilidad estadística ni una garantía de corrección.
            - No utilices automáticamente 1.0.
            - Utiliza valores altos cuando las evidencias sean claras y consistentes.
            - Utiliza valores menores cuando falten datos o existan contradicciones.
            - cause debe ser un código corto en snake_case.
            - summary debe explicar brevemente qué está ocurriendo y por qué es relevante.

            DATOS CALCULADOS POR EL SISTEMA:

            - PurchaseOrder.PendingQuantity ya ha sido calculado por el sistema.
            - CustomerOrderContext.AvailableStock ya contiene el stock disponible para cada pedido de cliente.
            - CustomerOrderContext.ShortageQuantity ya ha sido calculado por el sistema.
            - NO vuelvas a calcular ShortageQuantity.
            - NO modifiques ShortageQuantity.
            - Utiliza exactamente el valor de ShortageQuantity proporcionado para cada pedido.
            - CustomerOrders contiene pedidos de clientes relacionados con los productos del pedido de proveedor.
            - Que un pedido aparezca en CustomerOrders NO significa automáticamente que esté afectado.
            - Debes determinar qué pedidos están realmente en riesgo utilizando las fechas, el stock y la información de entrega disponible.

            FECHAS Y RETRASOS:

            - PurchaseOrder.ExpectedDate representa la fecha original esperada del pedido de proveedor.
            - Los emails pueden contener una nueva fecha de entrega comunicada posteriormente por el proveedor.
            - Si un email comunica explícitamente una nueva fecha de entrega, utilízala para evaluar el riesgo actual.
            - No confundas la fecha original ExpectedDate con una nueva fecha comunicada posteriormente.
            - Compara la fecha de entrega relevante del proveedor con RequiredDate de cada pedido de cliente.
            - Un pedido de cliente está en riesgo cuando los datos disponibles indican que la mercancía necesaria podría no estar disponible antes de su RequiredDate.
            - Si la fecha de entrega del proveedor es posterior a RequiredDate y el stock disponible no cubre la cantidad necesaria, considera el pedido en riesgo.
            - Si la información no permite determinar el riesgo con suficiente claridad, no inventes una conclusión.

            IMPACTO:

            - affectedCustomerOrders debe contener únicamente las referencias de los pedidos de cliente que realmente estén afectados o en riesgo.
            - Para cada pedido afectado, utiliza el ShortageQuantity calculado por el sistema.
            - shortageQuantity debe representar la cantidad que falta utilizando el stock disponible actualmente.
            - No vuelvas a calcular esa cantidad.
            - Si hay varios pedidos afectados, utiliza el déficit correspondiente a los pedidos afectados según los datos disponibles.
            - riskDate debe ser la RequiredDate más próxima entre los pedidos realmente afectados.
            - No inventes ni calcules una fecha distinta de las fechas proporcionadas.

            CAUSA:

            - Identifica la causa principal de la excepción utilizando únicamente la información disponible.
            - Prioriza información explícita de los emails y otros datos del contexto.
            - Si un proveedor comunica explícitamente un retraso, puedes utilizar "supplier_delay".
            - No inventes una causa que no aparezca respaldada por la evidencia.

            SOBRE LAS ACCIONES:

            Las acciones propuestas deben utilizar únicamente uno de estos tipos:

            - FollowUpSupplier
            - NotifyCustomer
            - InventoryCheck

            No inventes otros tipos de acción.

            Cada acción debe representar una actuación concreta que un empleado pueda realizar.

            - FollowUpSupplier: contactar o hacer seguimiento con el proveedor.
            - NotifyCustomer: comunicar el riesgo o retraso al cliente.
            - InventoryCheck: revisar stock disponible o alternativas de inventario.

            La propiedad reason debe explicar por qué esa acción está justificada por los datos del contexto.

            Tipos de acción recomendados:
            - follow_up_supplier
            - notify_customer
            - inventory_check

            EVIDENCIAS:

            - Cada evidencia debe ser un hecho concreto presente en el contexto.
            - No inventes evidencias.
            - No presentes una interpretación como si fuera una evidencia.
            - Mantén las evidencias específicas y breves.
            - Cada evidencia debe ser un objeto con una propiedad "description".

            CONFIANZA:

            - confidence representa tu confianza en el diagnóstico basándote en la claridad y consistencia de las evidencias.
            - Utiliza una confianza alta cuando exista evidencia explícita y coherente.
            - Utiliza una confianza menor cuando existan datos incompletos, ambiguos o contradictorios.
            - No utilices automáticamente 1.0.

            CONTEXTO:

            """ + contextJson + """

            Devuelve EXACTAMENTE un objeto JSON con esta estructura:

            {
              "severity": "LOW",
              "cause": "",
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

            IMPORTANTE:

            - Los valores mostrados arriba son únicamente ejemplos de estructura.
            - Sustituye los valores por los correspondientes al contexto real.
            - Mantén exactamente los nombres y tipos de las propiedades.
            - No incluyas ningún campo adicional.
            - La respuesta final debe ser únicamente el objeto JSON.
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

        Console.WriteLine("===== RESPUESTA DE OLLAMA =====");
        Console.WriteLine(responseJson);
        Console.WriteLine("================================");

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

            Console.WriteLine("===== AGENT RESULT =====");
            Console.WriteLine(
                JsonSerializer.Serialize(result, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
            Console.WriteLine("========================");

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