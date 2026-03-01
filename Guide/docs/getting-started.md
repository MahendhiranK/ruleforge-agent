# Getting Started

This guide shows how a project team enables RuleForge Agent in a .NET repository.

## 1. Prerequisites
- SonarQube self-hosted with a project created for your repository
- A Sonar token with permissions to run analysis and query issues
- GitHub Actions enabled for the repository
- .NET SDK 9 for local usage

## 2. Install (npm install equivalent)

### Option A: Install as a global .NET tool (recommended)
After the package is published:
```bash
dotnet tool install -g RuleForge.Agent
ruleforge --help
```

### Option B: Run from source (if you are evaluating)
```bash
dotnet run --project src/QualityAgent.Cli -- check --policy .qualityagent/policy.yml --no-sonar
```

## 3. Enable in a project repo (copy-in approach)
Copy these files from the main repo into your project:
- `.qualityagent/policy.yml`
- `.qualityagent/enforce-rules.txt`
- `.github/workflows/quality-agent.yml`

## 4. Configure GitHub Secrets
In GitHub repo settings → Secrets and variables → Actions → New repository secret:

- `SONAR_HOST_URL` (example: https://sonar.yourcompany.com)
- `SONAR_TOKEN`
- `SONAR_PROJECT_KEY`

## 5. Open a PR
When you open a PR, the workflow will:
- run `dotnet-sonarscanner` begin/build/test/end
- query SonarQube issues for that PR
- generate `artifacts/quality-report.md` and `artifacts/quality-report.json`
- upload the artifacts as a workflow artifact

## 6. Optional: Enable Azure AI Foundry remediation
In `.qualityagent/policy.yml`:
```yaml
aiFoundry:
  enable: true
  allowRules:
    - csharpsquid:S1481
    - csharpsquid:S125
```

Add GitHub secrets:
- `AZURE_FOUNDRY_ENDPOINT`
- `AZURE_FOUNDRY_API_KEY`
- `AZURE_FOUNDRY_MODEL`

Then run `autofix` mode (recommended in a separate workflow that creates a fix PR).
