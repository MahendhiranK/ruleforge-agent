using QualityAgent.Core.Pipeline;

static int Usage()
{
    Console.WriteLine("ruleforge (RuleForge Agent)\n");
    Console.WriteLine("Commands:");
    Console.WriteLine("  fix      Applies safe automatic fixes (dotnet format)");
    Console.WriteLine("  check    Runs build/test and evaluates SonarQube issues per policy, writes reports");
    Console.WriteLine("  autofix  Runs check, then applies safe fixes and optional AI fixes for allowed rules, writes before/after report\n");
    Console.WriteLine("Examples:");
    Console.WriteLine("  ruleforge fix --policy .qualityagent/policy.yml");
    Console.WriteLine("  ruleforge check --policy .qualityagent/policy.yml --pr 123 --branch feature/my-branch");
    Console.WriteLine("  ruleforge autofix --policy .qualityagent/policy.yml --pr 123 --branch feature/my-branch\n");
    Console.WriteLine("Options:");
    Console.WriteLine("  --policy <path>        Path to policy.yml (default: .qualityagent/policy.yml)");
    Console.WriteLine("  --pr <number>          Pull Request number (GitHub)");
    Console.WriteLine("  --branch <name>        Branch name (for reporting context)");
    Console.WriteLine("  --skip-tests           Skip dotnet test");
    Console.WriteLine("  --skip-build           Skip dotnet build");
    Console.WriteLine("  --no-sonar             Do not query SonarQube API (still runs local checks)");
    Console.WriteLine("  --workspace <path>     Repo root (default: current directory)");
    return 2;
}

if (args.Length == 0) return Usage();

var command = args[0].Trim().ToLowerInvariant();
var opts = CommandLine.Parse(args.Skip(1).ToArray());

try
{
    var runner = new QualityPipelineRunner();
    switch (command)
    {
        case "fix":
            return await runner.RunFixAsync(opts);
        case "check":
            return await runner.RunCheckAsync(opts);
        case "autofix":
            return await runner.RunAutoFixAsync(opts);
        case "-h":
        case "--help":
        case "help":
            return Usage();
        default:
            Console.Error.WriteLine($"Unknown command: {command}");
            return Usage();
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine("QualityAgent failed:");
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine(ex.ToString());
    return 1;
}

internal sealed class CommandLine
{
    public static Dictionary<string, string?> Parse(string[] args)
    {
        var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (!a.StartsWith("--")) continue;

            var key = a[2..];
            string? val = null;

            if (key is "skip-tests" or "skip-build" or "no-sonar")
            {
                dict[key] = "true";
                continue;
            }

            if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
            {
                val = args[i + 1];
                i++;
            }
            dict[key] = val;
        }
        return dict;
    }
}
