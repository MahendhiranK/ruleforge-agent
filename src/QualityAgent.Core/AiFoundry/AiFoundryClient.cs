using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace QualityAgent.Core.AiFoundry;

public sealed class AiFoundryClient
{
    private readonly HttpClient _http;
    private readonly string _model;

    public AiFoundryClient(string endpoint, string apiKey, string model)
    {
        if (string.IsNullOrWhiteSpace(endpoint)) throw new ArgumentException("endpoint is required");
        if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("apiKey is required");
        if (string.IsNullOrWhiteSpace(model)) throw new ArgumentException("model is required");

        _model = model;

        _http = new HttpClient { BaseAddress = new Uri(endpoint.TrimEnd('/') + "/") };
        _http.DefaultRequestHeaders.Add("api-key", apiKey);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<string> ProposeUpdatedFileAsync(string filePath, string fileContent, string ruleKey, string message, int startLine, int endLine, CancellationToken ct)
    {
        // Uses Azure AI Foundry model inference API (chat completions).
        // We require JSON output to avoid ambiguous formatting.
        var system = "You are a code quality assistant. You receive a C# file and a single SonarQube issue. " +
                     "Your job is to modify the file minimally to fix the issue while preserving behavior. " +
                     "Return ONLY a JSON object with fields: updatedContent (string), summary (string).";

        var user = new StringBuilder();
        user.AppendLine("Fix this SonarQube issue in the given file.");
        user.AppendLine();
        user.AppendLine($"File: {filePath}");
        user.AppendLine($"Rule: {ruleKey}");
        user.AppendLine($"Message: {message}");
        user.AppendLine($"Lines: {startLine}-{endLine}");
        user.AppendLine();
        user.AppendLine("C# file content:");
        user.AppendLine(fileContent);

        var payload = new
        {
            messages = new object[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user.ToString() }
            },
            model = _model,
            temperature = 0.0,
            response_format = new { type = "json_object" }
        };

        var json = JsonSerializer.Serialize(payload);
        using var req = new HttpRequestMessage(HttpMethod.Post, "models/chat/completions?api-version=2024-05-01-preview")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        using var resp = await _http.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Foundry chat completions failed: {resp.StatusCode}. Body: {body}");

        using var doc = JsonDocument.Parse(body);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("Foundry returned empty content.");

        using var outDoc = JsonDocument.Parse(content);
        var updated = outDoc.RootElement.GetProperty("updatedContent").GetString();

        if (string.IsNullOrWhiteSpace(updated))
            throw new InvalidOperationException("Foundry output JSON did not include updatedContent.");

        return updated!;
    }
}
