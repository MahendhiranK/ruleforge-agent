namespace QualityAgent.Core.Policy;

public static class EnforcedRulesLoader
{
    public static IReadOnlyList<string> LoadRules(string? rulesFilePath, string workspace)
    {
        if (string.IsNullOrWhiteSpace(rulesFilePath))
            return Array.Empty<string>();

        var path = rulesFilePath!;
        if (!Path.IsPathRooted(path))
            path = Path.Combine(workspace, path);

        if (!File.Exists(path))
            return Array.Empty<string>();

        var lines = File.ReadAllLines(path)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("#"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return lines;
    }
}
