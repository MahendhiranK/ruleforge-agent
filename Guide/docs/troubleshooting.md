# Troubleshooting

## SonarQube query skipped
If the report says SonarQube was skipped, verify env vars or GitHub secrets:
- SONAR_HOST_URL
- SONAR_TOKEN
- SONAR_PROJECT_KEY

## PR analysis shows no issues
Confirm your workflow executed SonarScanner begin/build/test/end successfully.
Also confirm your SonarQube instance supports PR decoration and analysis.

## AutoFix does not reduce issues
Many SonarQube rules are not auto-fixable. Start with:
- dotnet format
- a small allowlist for AI remediation

## AI Foundry call fails
Confirm:
- endpoint is correct (ends with services.ai.azure.com)
- api key is valid
- model deployment name exists
