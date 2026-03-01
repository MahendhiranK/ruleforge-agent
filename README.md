# RuleForge Agent

RuleForge Agent is a policy-driven quality agent for .NET microservices that integrates:

- **SonarQube (self-hosted)** issue evaluation and governance reporting
- **Enforced rule list** (PASS/FAIL per rule key)
- **Safe autofix** using `dotnet format`
- **Optional Azure AI Foundry remediation** for an allowlisted subset of rule keys
- **GitHub Action** and **.NET tool** packaging for easy adoption

This repository ships an enforced C# rule list at:
- `.qualityagent/enforce-rules.txt` (450 rule keys)

## Install (like npm install)

### Option A: Install as a global .NET tool (recommended)
After you publish the package `RuleForge.Agent` to NuGet or GitHub Packages:

```bash
dotnet tool install -g RuleForge.Agent
ruleforge --help
```

### Option B: Run from source (immediate)
```bash
dotnet build
dotnet run --project src/QualityAgent.Cli -- check --policy .qualityagent/policy.yml --no-sonar
```

## Commands

### 1) Check (report + gate)
```bash
ruleforge check --policy .qualityagent/policy.yml --pr 123 --branch feature/my-branch
```

### 2) Fix (safe formatting)
```bash
ruleforge fix --policy .qualityagent/policy.yml
```

### 3) AutoFix (before/after reporting)
```bash
ruleforge autofix --policy .qualityagent/policy.yml --pr 123 --branch feature/my-branch
```

## Inputs

### SonarQube env vars (required for Sonar query)
- `SONAR_HOST_URL`
- `SONAR_TOKEN`
- `SONAR_PROJECT_KEY`

### Azure AI Foundry env vars (optional, only if enabled)
- `AZURE_FOUNDRY_ENDPOINT`
- `AZURE_FOUNDRY_API_KEY`
- `AZURE_FOUNDRY_MODEL`

Enable Foundry in `.qualityagent/policy.yml`:
```yaml
aiFoundry:
  enable: true
  allowRules:
    - csharpsquid:S1481
    - csharpsquid:S125
```

## Outputs

Generated under `artifacts/`:
- `quality-report.md`
- `quality-report.json`

The Markdown report includes:
- build and test results
- SonarQube counts by severity
- top failing rule keys
- enforced rules PASS/FAIL table
- optional before/after summary for autofix runs

## GitHub Action

A workflow template is included at:
- `.github/workflows/quality-agent.yml`

For a reusable action wrapper, see:
- `action/action.yml`

## Security note
AI remediation is allowlisted and should be expanded cautiously. The framework is designed for governance-first adoption.

## License
MIT
