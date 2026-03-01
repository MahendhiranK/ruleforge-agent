# Policy Reference

Policy file: `.qualityagent/policy.yml`

## qualityGate
Thresholds for severity counts.

Example:
```yaml
qualityGate:
  failOn:
    blocker: 0
    critical: 0
    major: 10
  onlyNewCode: true
```

## sonarqube
Defines environment variable names used by the agent.

```yaml
sonarqube:
  serverUrlEnv: SONAR_HOST_URL
  tokenEnv: SONAR_TOKEN
  projectKeyEnv: SONAR_PROJECT_KEY
  enforceRulesFile: .qualityagent/enforce-rules.txt
```

## autofix
Safe deterministic fixes.

```yaml
autofix:
  enableDotnetFormat: true
  maxChangedLines: 500
```

## aiFoundry
Optional, allowlisted remediation via Azure AI Foundry.

```yaml
aiFoundry:
  enable: false
  endpointEnv: AZURE_FOUNDRY_ENDPOINT
  apiKeyEnv: AZURE_FOUNDRY_API_KEY
  modelEnv: AZURE_FOUNDRY_MODEL
  allowRules:
    - csharpsquid:S1481
  maxFilesPerRun: 10
  maxFileChars: 45000
```

## reporting
Output paths.

```yaml
reporting:
  markdownPath: artifacts/quality-report.md
  jsonPath: artifacts/quality-report.json
```
