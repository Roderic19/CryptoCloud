using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace CryptoCloud.Backend;

public class ChatFunction
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    
    public ChatFunction(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory)
    {
       _logger = loggerFactory.CreateLogger<ChatFunction>();
       _httpClient = httpClientFactory.CreateClient();
    }

    [Function("Chat")]
    [Authorize]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
        HttpRequestData req
    )
    {
        var apiUrl = Environment.GetEnvironmentVariable("OLLAMA_API_URL")!;
        var apiKey = Environment.GetEnvironmentVariable("OLLAMA_API_KEY")!;
        var model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "llama3";

        var chatUrl = $"{apiUrl.TrimEnd('/')}/chat";
        _logger.LogInformation("Calling Ollama chat endpoint: {ChatUrl}", chatUrl);

        var payload = new
        {
            model,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = "Hello! How are you?"
                }
            },
            stream = false
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, chatUrl)
        {
            Content = JsonContent.Create(payload)
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        try
        {
            using var ollamaResponse = await _httpClient.SendAsync(request);
            var responseBody = await ollamaResponse.Content.ReadAsStringAsync();

            if (!ollamaResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Ollama Cloud returned {StatusCode}: {ResponseBody}",
                    (int)ollamaResponse.StatusCode,
                    responseBody);

                return await CreateJsonResponseAsync(
                    req,
                    ollamaResponse.StatusCode,
                    new
                    {
                        error = "Ollama Cloud returned an error.",
                        statusCode = (int)ollamaResponse.StatusCode,
                        response = responseBody
                    });
            }

            var message = ExtractAssistantMessage(responseBody);

            return await CreateJsonResponseAsync(
                req,
                HttpStatusCode.OK,
                new { message });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error while calling Ollama chat endpoint.");

            return await CreateJsonResponseAsync(
                req,
                HttpStatusCode.BadGateway,
                new { error = "Unable to reach Ollama chat endpoint." });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Unable to parse Ollama chat response.");

            return await CreateJsonResponseAsync(
                req,
                HttpStatusCode.BadGateway,
                new { error = "Unable to parse Ollama chat response." });
        }
    }

    private static string ExtractAssistantMessage(string responseBody)
    {
        using var json = JsonDocument.Parse(responseBody);
        return json.RootElement
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }

    private static async Task<HttpResponseData> CreateJsonResponseAsync(
        HttpRequestData req,
        HttpStatusCode statusCode,
        object body)
    {
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(body));

        return response;
    }
}
