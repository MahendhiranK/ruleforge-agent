# RuleForge Agent Evaluation Prototype

This package helps an independent reviewer evaluate RuleForge Agent against a realistic .NET microservice code quality governance problem.

Repository under review:
https://github.com/MahendhiranK/ruleforge-agent

## Evaluation Goal

The goal is to demonstrate whether RuleForge Agent can help address a common enterprise engineering problem:

Static analysis tools such as SonarQube detect code quality violations, but remediation remains manual, inconsistent, and repetitive across development teams. RuleForge Agent introduces a policy-driven AI agent model for evaluating rule violations, generating governance reports, and supporting deterministic or AI-assisted remediation.

## Prototype Contents

- sample-before/LoanService.cs
- sample-after/LoanService.cs
- .qualityagent/policy.yml
- .qualityagent/enforce-rules.txt
- reports/expected-quality-report.md
- reviewer-context.md
- expert-letter-outline.md
- how-to-run-review.md
