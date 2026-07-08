# How to Run the RuleForge Agent Review

## Option 1: Documentation-Based Review

Use this option if the reviewer does not have a SonarQube server.

1. Review the public GitHub repository:
   https://github.com/MahendhiranK/ruleforge-agent

2. Review:
   - README.md
   - docs/architecture.md
   - docs/sample-input-output.md
   - .qualityagent/policy.yml
   - .qualityagent/enforce-rules.txt

3. Compare:
   - sample-before/LoanService.cs
   - sample-after/LoanService.cs

4. Review:
   - reports/expected-quality-report.md

## Option 2: Hands-On Review With RuleForge Source

If the reviewer has .NET 9 installed:

1. Clone RuleForge Agent:
   git clone https://github.com/MahendhiranK/ruleforge-agent.git

2. Build:
   dotnet build

3. Run in demo mode:
   dotnet run --project src/QualityAgent.Cli -- check --policy .qualityagent/policy.yml --no-sonar --skip-tests

4. Review generated outputs:
   artifacts/quality-report.md
   artifacts/quality-report.json

## Recommended Letter Framing

Use accurate language:

'The framework was independently reviewed and evaluated against a realistic code quality governance scenario.'

Avoid inaccurate language:

'The framework was adopted in production.'

unless production deployment actually occurred.
