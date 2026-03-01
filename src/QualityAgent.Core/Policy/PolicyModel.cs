namespace QualityAgent.Core.Policy;

public sealed class PolicyModel
{
    public int Version { get; set; } = 1;
    public QualityGatePolicy QualityGate { get; set; } = new();
    public AutoFixPolicy AutoFix { get; set; } = new();
    public SonarQubePolicy SonarQube { get; set; } = new();
    public AiFoundryPolicy AiFoundry { get; set; } = new();
    public ReportingPolicy Reporting { get; set; } = new();
}

public sealed class QualityGatePolicy
{
    public FailOnPolicy FailOn { get; set; } = new();
    public bool OnlyNewCode { get; set; } = true;
}

public sealed class FailOnPolicy
{
    public int Blocker { get; set; } = 0;
    public int Critical { get; set; } = 0;
    public int Major { get; set; } = 10;
    public int Minor { get; set; } = int.MaxValue;
    public int Info { get; set; } = int.MaxValue;
}

public sealed class AutoFixPolicy
{
    public bool EnableDotnetFormat { get; set; } = true;
    public int MaxChangedLines { get; set; } = 500;
}

public sealed class SonarQubePolicy
{
    public string ServerUrlEnv { get; set; } = "SONAR_HOST_URL";
    public string TokenEnv { get; set; } = "SONAR_TOKEN";
    public string ProjectKeyEnv { get; set; } = "SONAR_PROJECT_KEY";

    // Optional: instead of listing 450 rules in YAML, store them in a text file (one rule per line).
    public string? EnforceRulesFile { get; set; } = ".qualityagent/enforce-rules.txt";
}

public sealed class AiFoundryPolicy
{
    public bool Enable { get; set; } = false;
    public string EndpointEnv { get; set; } = "AZURE_FOUNDRY_ENDPOINT";
    public string ApiKeyEnv { get; set; } = "AZURE_FOUNDRY_API_KEY";
    public string ModelEnv { get; set; } = "AZURE_FOUNDRY_MODEL";

    // Only these rules are eligible for AI-based fixes.
    public List<string> AllowRules { get; set; } = new();

    public int MaxFilesPerRun { get; set; } = 10;
    public int MaxFileChars { get; set; } = 45000;
}

public sealed class ReportingPolicy
{
    public string MarkdownPath { get; set; } = "artifacts/quality-report.md";
    public string JsonPath { get; set; } = "artifacts/quality-report.json";
}
