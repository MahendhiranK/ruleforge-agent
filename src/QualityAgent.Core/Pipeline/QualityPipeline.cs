using System.Diagnostics;
using QualityAgent.Core.AiFoundry;
using QualityAgent.Core.Policy;
using QualityAgent.Core.Reports;
using QualityAgent.Core.Sonar;

namespace QualityAgent.Core.Pipeline;

public sealed class QualityPipelineRunner
{
    public async Task<int> RunFixAsync(Dictionary<string, string?> opts)
    {
        var workspace = GetOpt(opts, "workspace") ?? Directory.GetCurrentDirectory();
        var policyPath = GetOpt(opts, "policy") ?? ".qualityagent/policy.yml";
        if (!Path.IsPathRooted(policyPath)) policyPath = Path.Combine(workspace, policyPath);

        var policy = PolicyLoader.Load(policyPath);

        if (!policy.AutoFix.EnableDotnetFormat)
        {
            Console.WriteLine("Auto-fix is disabled by policy.");
            return 0;
        }

        Console.WriteLine("Running: dotnet format");
        var code = await RunProcessAsync("dotnet", "format", CancellationToken.None);
        if (code != 0)
        {
            Console.Error.WriteLine("dotnet format failed.");
            return code;
        }

        Console.WriteLine("Auto-fix complete.");
        return 0;
    }

    public async Task<int> RunCheckAsync(Dictionary<string, string?> opts)
        => await RunInternalAsync(opts, runAutoFix: false);

    public async Task<int> RunAutoFixAsync(Dictionary<string, string?> opts)
        => await RunInternalAsync(opts, runAutoFix: true);

    private async Task<int> RunInternalAsync(Dictionary<string, string?> opts, bool runAutoFix)
    {
        var workspace = GetOpt(opts, "workspace") ?? Directory.GetCurrentDirectory();
        var policyPath = GetOpt(opts, "policy") ?? ".qualityagent/policy.yml";
        if (!Path.IsPathRooted(policyPath)) policyPath = Path.Combine(workspace, policyPath);

        var policy = PolicyLoader.Load(policyPath);

        var skipBuild = opts.ContainsKey("skip-build");
        var skipTests = opts.ContainsKey("skip-tests");
        var noSonar = opts.ContainsKey("no-sonar");

        int? pr = null;
        if (int.TryParse(GetOpt(opts, "pr"), out var prNum)) pr = prNum;
        var branch = GetOpt(opts, "branch");

        var enforcedRules = EnforcedRulesLoader.LoadRules(policy.SonarQube.EnforceRulesFile, workspace);

        var report = new QualityReport
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            Branch = branch,
            PullRequest = pr
        };

        if (!skipBuild)
        {
            Console.WriteLine("Running: dotnet build");
            var code = await RunProcessAsync("dotnet", "build -c Release", CancellationToken.None);
            report.Steps.Add(new StepResult("dotnet build", code == 0, code));
            if (code != 0)
            {
                report.Outcome = "FAIL";
                await ReportWriter.WriteAsync(report, policy.Reporting.MarkdownPath, policy.Reporting.JsonPath);
                return 1;
            }
        }

        if (!skipTests)
        {
            Console.WriteLine("Running: dotnet test");
            var code = await RunProcessAsync("dotnet", "test -c Release --no-build", CancellationToken.None);
            report.Steps.Add(new StepResult("dotnet test", code == 0, code));
            if (code != 0)
            {
                report.Outcome = "FAIL";
                await ReportWriter.WriteAsync(report, policy.Reporting.MarkdownPath, policy.Reporting.JsonPath);
                return 1;
            }
        }

        List<SonarIssue> issuesBefore = new();
        if (!noSonar)
            issuesBefore = await QuerySonarAsync(policy, pr, branch);

        if (issuesBefore.Count > 0 || runAutoFix)
        {
            report.SonarBefore = SonarSummary.FromIssues(issuesBefore);
            report.SonarBefore.Skipped = issuesBefore.Count == 0 && noSonar;
        }

        if (runAutoFix)
        {
            report.AutoFix = new AutoFixSummary();

            // Tier 1: dotnet format
            if (policy.AutoFix.EnableDotnetFormat)
            {
                Console.WriteLine("AutoFix: Running dotnet format");
                var code = await RunProcessAsync("dotnet", "format", CancellationToken.None);
                report.AutoFix.DotnetFormatApplied = (code == 0);
            }

            // Tier 2/3: AI fixes for allowlisted rules, only if enabled and sonar data available
            if (policy.AiFoundry.Enable && issuesBefore.Count > 0)
            {
                var endpoint = Environment.GetEnvironmentVariable(policy.AiFoundry.EndpointEnv) ?? "";
                var apiKey = Environment.GetEnvironmentVariable(policy.AiFoundry.ApiKeyEnv) ?? "";
                var model = Environment.GetEnvironmentVariable(policy.AiFoundry.ModelEnv) ?? "";

                if (!string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(model))
                {
                    var client = new AiFoundryClient(endpoint, apiKey, model);
                    await ApplyAiFixesAsync(client, policy, issuesBefore, workspace, report.AutoFix);
                }
                else
                {
                    Console.WriteLine("AI Foundry is enabled in policy, but AZURE_FOUNDRY_ENDPOINT / AZURE_FOUNDRY_API_KEY / AZURE_FOUNDRY_MODEL are missing.");
                }
            }

            // Rebuild to ensure changes compile
            if (!skipBuild)
            {
                Console.WriteLine("AutoFix: Verifying build after fixes");
                var code = await RunProcessAsync("dotnet", "build -c Release", CancellationToken.None);
                report.Steps.Add(new StepResult("dotnet build (post-fix)", code == 0, code));
                if (code != 0)
                {
                    report.Outcome = "FAIL";
                    await ReportWriter.WriteAsync(report, policy.Reporting.MarkdownPath, policy.Reporting.JsonPath);
                    return 1;
                }
            }
        }

        // Sonar after requires that a scan was executed externally (CI). We can query again if CI has already published results.
        List<SonarIssue> issuesAfter = new();
        if (!noSonar)
            issuesAfter = await QuerySonarAsync(policy, pr, branch);

        if (issuesAfter.Count > 0 || runAutoFix)
            report.SonarAfter = SonarSummary.FromIssues(issuesAfter);

        // Enforced rules pass/fail based on "after" if available, else "before"
        var usedIssues = issuesAfter.Count > 0 ? issuesAfter : issuesBefore;
        report.EnforcedRules = BuildEnforcedRulesStatus(enforcedRules, usedIssues);

        // Gate is computed from "after" if available, else "before"
        var sonarForGate = SonarSummary.FromIssues(usedIssues);
        report.QualityGate = QualityGateEvaluator.Evaluate(policy, sonarForGate);
        report.Outcome = report.QualityGate.Passed ? "PASS" : "FAIL";

        await ReportWriter.WriteAsync(report, policy.Reporting.MarkdownPath, policy.Reporting.JsonPath);
        Console.WriteLine($"QualityAgent outcome: {report.Outcome}");
        return report.Outcome == "PASS" ? 0 : 1;
    }

    private static List<EnforcedRuleStatus> BuildEnforcedRulesStatus(IReadOnlyList<string> rules, List<SonarIssue> issues)
    {
        if (rules.Count == 0) return new();

        var failed = new HashSet<string>(
            issues.Select(i => i.Rule).Where(r => !string.IsNullOrWhiteSpace(r)),
            StringComparer.OrdinalIgnoreCase);

        var list = new List<EnforcedRuleStatus>(rules.Count);
        foreach (var r in rules)
            list.Add(new EnforcedRuleStatus { Rule = r, Status = failed.Contains(r) ? "FAIL" : "PASS" });

        return list;
    }

    private static async Task<List<SonarIssue>> QuerySonarAsync(PolicyModel policy, int? pr, string? branch)
    {
        var serverUrl = Environment.GetEnvironmentVariable(policy.SonarQube.ServerUrlEnv) ?? "";
        var token = Environment.GetEnvironmentVariable(policy.SonarQube.TokenEnv) ?? "";
        var projectKey = Environment.GetEnvironmentVariable(policy.SonarQube.ProjectKeyEnv) ?? "";

        if (string.IsNullOrWhiteSpace(serverUrl) || string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(projectKey))
        {
            Console.WriteLine("SonarQube env vars not set. Skipping SonarQube query.");
            return new List<SonarIssue>();
        }

        var client = new SonarClient(serverUrl, token);
        return await client.GetIssuesAsync(projectKey, pr, branch, policy.QualityGate.OnlyNewCode, CancellationToken.None);
    }

    private static async Task ApplyAiFixesAsync(AiFoundryClient client, PolicyModel policy, List<SonarIssue> issues, string workspace, AutoFixSummary summary)
    {
        var allow = new HashSet<string>(policy.AiFoundry.AllowRules ?? new(), StringComparer.OrdinalIgnoreCase);
        if (allow.Count == 0) return;

        // Group by file
        var fileToIssues = issues
            .Where(i => allow.Contains(i.Rule))
            .GroupBy(i => TryResolveLocalPath(i.Component, workspace))
            .Where(g => !string.IsNullOrWhiteSpace(g.Key))
            .ToDictionary(g => g.Key!, g => g.ToList());

        foreach (var kv in fileToIssues.Take(policy.AiFoundry.MaxFilesPerRun))
        {
            var filePath = kv.Key!;
            if (!File.Exists(filePath)) continue;

            var content = await File.ReadAllTextAsync(filePath);
            if (content.Length > policy.AiFoundry.MaxFileChars)
            {
                Console.WriteLine($"Skipping AI fix for large file: {filePath}");
                continue;
            }

            // Apply issues one by one, updating content each time
            foreach (var issue in kv.Value)
            {
                var tr = issue.TextRange;
                var startLine = tr?.StartLine ?? 0;
                var endLine = tr?.EndLine ?? 0;

                var updated = await client.ProposeUpdatedFileAsync(filePath, content, issue.Rule, issue.Message, startLine, endLine, CancellationToken.None);

                if (!string.Equals(updated, content, StringComparison.Ordinal))
                {
                    content = updated;
                    summary.AiFixesApplied++;
                }
            }

            await File.WriteAllTextAsync(filePath, content);
            summary.FilesTouched.Add(filePath);
        }
    }

    private static string? TryResolveLocalPath(string component, string workspace)
    {
        // SonarQube component often looks like "projectKey:src/MyFile.cs"
        var idx = component.IndexOf(':');
        if (idx >= 0 && idx + 1 < component.Length)
        {
            var rel = component[(idx + 1)..];
            rel = rel.Replace('/', Path.DirectorySeparatorChar);
            var full = Path.Combine(workspace, rel);
            return full;
        }

        return null;
    }

    private static string? GetOpt(Dictionary<string, string?> opts, string key)
        => opts.TryGetValue(key, out var v) ? v : null;

    private static async Task<int> RunProcessAsync(string fileName, string arguments, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var p = new Process { StartInfo = psi };
        p.OutputDataReceived += (_, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
        p.ErrorDataReceived += (_, e) => { if (e.Data != null) Console.Error.WriteLine(e.Data); };

        p.Start();
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        await p.WaitForExitAsync(ct);
        return p.ExitCode;
    }
}

public static class QualityGateEvaluator
{
    public static QualityGateResult Evaluate(PolicyModel policy, SonarSummary sonar)
    {
        var fail = policy.QualityGate.FailOn;

        bool passed =
            sonar.Blocker <= fail.Blocker &&
            sonar.Critical <= fail.Critical &&
            sonar.Major <= fail.Major &&
            sonar.Minor <= fail.Minor &&
            sonar.Info <= fail.Info;

        return new QualityGateResult
        {
            Passed = passed,
            Thresholds = new Dictionary<string, int>
            {
                ["BLOCKER"] = fail.Blocker,
                ["CRITICAL"] = fail.Critical,
                ["MAJOR"] = fail.Major,
                ["MINOR"] = fail.Minor,
                ["INFO"] = fail.Info
            }
        };
    }
}

public sealed class QualityReport
{
    public DateTimeOffset GeneratedAtUtc { get; set; }
    public string? Branch { get; set; }
    public int? PullRequest { get; set; }
    public List<StepResult> Steps { get; set; } = new();

    // For simple check
    public SonarSummary? Sonar { get; set; }

    // For autofix runs
    public SonarSummary? SonarBefore { get; set; }
    public SonarSummary? SonarAfter { get; set; }
    public AutoFixSummary? AutoFix { get; set; }

    public List<EnforcedRuleStatus>? EnforcedRules { get; set; }
    public QualityGateResult? QualityGate { get; set; }
    public string Outcome { get; set; } = "";
}

public sealed record StepResult(string Name, bool Succeeded, int ExitCode);

public sealed class AutoFixSummary
{
    public bool DotnetFormatApplied { get; set; }
    public int AiFixesApplied { get; set; }
    public List<string> FilesTouched { get; set; } = new();
}

public sealed class EnforcedRuleStatus
{
    public string Rule { get; set; } = "";
    public string Status { get; set; } = "PASS";
}

public sealed class QualityGateResult
{
    public bool Passed { get; set; }
    public Dictionary<string, int> Thresholds { get; set; } = new();
}

public sealed class SonarSummary
{
    public bool Skipped { get; set; }
    public string? SkipReason { get; set; }

    public int Blocker { get; set; }
    public int Critical { get; set; }
    public int Major { get; set; }
    public int Minor { get; set; }
    public int Info { get; set; }

    public Dictionary<string, int> TopRules { get; set; } = new();

    public static SonarSummary FromIssues(IEnumerable<SonarIssue> issues)
    {
        var s = new SonarSummary();
        var rules = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var i in issues)
        {
            switch ((i.Severity ?? "").ToUpperInvariant())
            {
                case "BLOCKER": s.Blocker++; break;
                case "CRITICAL": s.Critical++; break;
                case "MAJOR": s.Major++; break;
                case "MINOR": s.Minor++; break;
                case "INFO": s.Info++; break;
            }

            if (!string.IsNullOrWhiteSpace(i.Rule))
                rules[i.Rule] = rules.TryGetValue(i.Rule, out var c) ? c + 1 : 1;
        }

        s.TopRules = rules
            .OrderByDescending(kv => kv.Value)
            .Take(10)
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

        return s;
    }
}
