using System.Net.Http.Headers;
using System.Text.Json;

namespace QualityAgent.Core.Sonar;

public sealed class SonarClient
{
    private readonly HttpClient _http;

    public SonarClient(string serverUrl, string token)
    {
        if (string.IsNullOrWhiteSpace(serverUrl)) throw new ArgumentException("serverUrl is required");
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("token is required");

        _http = new HttpClient
        {
            BaseAddress = new Uri(serverUrl.TrimEnd('/') + "/")
        };

        var basic = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{token}:"));
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);
    }

    public async Task<List<SonarIssue>> GetIssuesAsync(string projectKey, int? prNumber, string? branch, bool onlyNewCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(projectKey)) throw new ArgumentException("projectKey is required");

        var query = new List<string>
        {
            $"componentKeys={Uri.EscapeDataString(projectKey)}",
            "ps=500",
            "resolved=false"
        };

        if (prNumber.HasValue)
            query.Add($"pullRequest={prNumber.Value}");
        else if (!string.IsNullOrWhiteSpace(branch))
            query.Add($"branch={Uri.EscapeDataString(branch)}");

        if (onlyNewCode)
            query.Add("sinceLeakPeriod=true");

        var all = new List<SonarIssue>();
        int page = 1;

        while (true)
        {
            var url = "api/issues/search?" + string.Join("&", query) + $"&p={page}";
            using var resp = await _http.GetAsync(url, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"SonarQube API call failed: {resp.StatusCode}. Body: {body}");

            using var doc = JsonDocument.Parse(body);
            var issuesElem = doc.RootElement.GetProperty("issues");
            foreach (var i in issuesElem.EnumerateArray())
            {
                var issue = new SonarIssue
                {
                    Key = i.GetProperty("key").GetString() ?? "",
                    Rule = i.GetProperty("rule").GetString() ?? "",
                    Severity = i.GetProperty("severity").GetString() ?? "",
                    Component = i.GetProperty("component").GetString() ?? "",
                    Message = i.GetProperty("message").GetString() ?? "",
                    Type = i.TryGetProperty("type", out var t) ? (t.GetString() ?? "") : ""
                };

                if (i.TryGetProperty("textRange", out var tr) && tr.ValueKind == JsonValueKind.Object)
                {
                    issue.TextRange = new SonarTextRange
                    {
                        StartLine = tr.TryGetProperty("startLine", out var sl) ? sl.GetInt32() : 0,
                        EndLine = tr.TryGetProperty("endLine", out var el) ? el.GetInt32() : 0
                    };
                }

                all.Add(issue);
            }

            var paging = doc.RootElement.GetProperty("paging");
            var total = paging.GetProperty("total").GetInt32();
            var pageSize = paging.GetProperty("pageSize").GetInt32();
            var current = paging.GetProperty("pageIndex").GetInt32();

            if (current * pageSize >= total) break;
            page++;
        }

        return all;
    }
}
