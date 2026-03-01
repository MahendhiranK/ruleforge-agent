namespace QualityAgent.Core.Sonar;

public sealed class SonarIssue
{
    public string Key { get; set; } = "";
    public string Rule { get; set; } = "";
    public string Severity { get; set; } = "";
    public string Component { get; set; } = "";
    public string Message { get; set; } = "";
    public string Type { get; set; } = "";
    public SonarTextRange? TextRange { get; set; }
}

public sealed class SonarTextRange
{
    public int StartLine { get; set; }
    public int EndLine { get; set; }
}
