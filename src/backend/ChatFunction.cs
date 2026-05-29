using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

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
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
        HttpRequestData req
    )
    {
        var apiUrl = Environment.GetEnvironmentVariable("OLLAMA_API_URL")!;
        var apiKey = Environment.GetEnvironmentVariable("OLLAMA_API_KEY")!;

        var chatUrl = $"{apiUrl.TrimEnd('/')}/chat";
        var payload = new
        {
            model = "llama3",
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = "Hello! Reply with: Connectivity Test Successful."
                }
            },
            stream = false
        };

        var request = new HttpRequestMessage(HttpMethod.Post, chatUrl)
        {
            Content = JsonContent.Create(payload)
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        try
        {
            var ollamaResponse = await _httpClient.SendAsync(request);
            var responseBody = await ollamaResponse.Content.ReadAsStringAsync();

            var response = req.CreateResponse(ollamaResponse.StatusCode);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(responseBody);

            return response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error while calling Ollama chat endpoint.");

            return await CreateJsonResponseAsync(
                req,
                HttpStatusCode.BadGateway,
                new { error = "Unable to reach Ollama chat endpoint." });
        }
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
