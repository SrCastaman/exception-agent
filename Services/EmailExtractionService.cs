using System.Text;
using System.Text.Json;
using ExceptionAgent.Models;

namespace ExceptionAgent.Services;

public class EmailExtractionService
{
    private readonly HttpClient _httpClient;

    public EmailExtractionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<SupplierEmailExtractionResult?> ExtractAsync(
        Email email)
    {
        var emailJson = JsonSerializer.Serialize(
            new
            {
                email.Id,
                email.Date,
                email.Sender,
                email.Subject,
                email.Body
            },
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        var prompt = """
            Eres un extractor de información de emails de proveedores.

            Tu tarea es extraer únicamente hechos explícitos del email.

            NO debes:
            - analizar el impacto en clientes;
            - realizar un diagnóstico;
            - recomendar acciones;
            - inventar información.

            Debes identificar si el email contiene información sobre:
            - retrasos;
            - cambios de fecha de entrega;
            - cantidades afectadas;
            - referencias de pedidos de compra.

            REGLAS:

            - Utiliza únicamente la información presente en el email.
            - purchaseOrderReference debe ser la referencia del pedido de compra
              mencionada explícitamente en el email.
            - eventType debe ser "delivery_delay" si el email informa de un retraso
              o cambio de fecha de entrega.
            - newExpectedDate debe ser la nueva fecha de entrega indicada por el proveedor.
            - Si la fecha está expresada de forma relativa y puede resolverse usando
              la fecha del email, resuélvela.
            - Si no puede resolverse con seguridad, utiliza null.
            - affectedQuantity debe ser la cantidad explícitamente afectada.
            - sourceEmailId debe ser exactamente el Id proporcionado del email.
            - evidence debe contener el texto relevante del email que respalda
              la extracción.
            - Si el email no contiene una actualización de entrega relevante,
              devuelve null.
            - Devuelve únicamente JSON válido.

            EMAIL:

            """ + emailJson + """

            Devuelve exactamente esta estructura:

            {
              "sourceEmailId": 0,
              "purchaseOrderReference": "",
              "eventType": "",
              "newExpectedDate": null,
              "affectedQuantity": null,
              "evidence": ""
            }

            Sustituye los valores de ejemplo por los valores reales del email.
            """;

        var request = new
        {
            model = "qwen3:8b",
            prompt,
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
                PropertyNameCaseInsensitive = true
            });

        if (ollamaResponse == null ||
            string.IsNullOrWhiteSpace(ollamaResponse.Response))
        {
            return null;
        }

        return JsonSerializer.Deserialize<SupplierEmailExtractionResult>(
            ollamaResponse.Response,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
    }

    private class OllamaResponse
    {
        public string Response { get; set; } = string.Empty;
    }
}