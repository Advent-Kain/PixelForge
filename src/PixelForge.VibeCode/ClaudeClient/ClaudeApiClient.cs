using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PixelForge.VibeCode.ClaudeClient;

/// <summary>
/// Client for interacting with the Claude API.
/// </summary>
public class ClaudeApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string DefaultModel = "claude-sonnet-4-5-20250929";

    public ClaudeApiClient(string apiKey)
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    /// <summary>
    /// Send a message to Claude and get a response.
    /// </summary>
    public async Task<ClaudeResponse?> SendMessageAsync(
        string userMessage,
        string? systemPrompt = null,
        List<Message>? conversationHistory = null,
        int maxTokens = 4096,
        string? model = null)
    {
        var messages = new List<Message>();

        // Add conversation history
        if (conversationHistory != null)
        {
            messages.AddRange(conversationHistory);
        }

        // Add user message
        messages.Add(new Message
        {
            Role = "user",
            Content = userMessage
        });

        var request = new ClaudeRequest
        {
            Model = model ?? DefaultModel,
            MaxTokens = maxTokens,
            Messages = messages
        };

        if (!string.IsNullOrEmpty(systemPrompt))
        {
            request.System = systemPrompt;
        }

        try
        {
            var response = await _httpClient.PostAsJsonAsync(ApiUrl, request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<ClaudeResponse>();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Failed to communicate with Claude API: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Generate code based on a natural language description.
    /// </summary>
    public async Task<string?> GenerateCodeAsync(
        string description,
        string context,
        string language = "C#")
    {
        string systemPrompt = $@"You are an expert {language} programmer helping to build a 2D game engine.
You have access to the following engine API and context:

{context}

Generate clean, well-documented {language} code that follows best practices.
Include XML documentation comments for public members.
Only return the code without additional explanations.";

        var response = await SendMessageAsync(description, systemPrompt);

        return response?.Content.FirstOrDefault()?.Text;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

/// <summary>
/// Claude API request model.
/// </summary>
public class ClaudeRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "claude-sonnet-4-5-20250929";

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 4096;

    [JsonPropertyName("messages")]
    public List<Message> Messages { get; set; } = new();

    [JsonPropertyName("system")]
    public string? System { get; set; }

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 1.0;
}

/// <summary>
/// Claude API response model.
/// </summary>
public class ClaudeResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public List<ContentBlock> Content { get; set; } = new();

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; set; }

    [JsonPropertyName("usage")]
    public Usage? Usage { get; set; }
}

/// <summary>
/// Message in a conversation.
/// </summary>
public class Message
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Content block in a response.
/// </summary>
public class ContentBlock
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Token usage information.
/// </summary>
public class Usage
{
    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; set; }

    [JsonPropertyName("output_tokens")]
    public int OutputTokens { get; set; }
}
