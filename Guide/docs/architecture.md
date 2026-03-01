# RuleForge Agent – Architecture Overview

RuleForge Agent is a policy-driven enforcement framework integrating SonarQube analysis, deterministic remediation, and optional Azure AI Foundry correction into CI/CD pipelines.

---

# 1. High-Level Flow

Developer Code
      |
Build + Test Pipeline
      |
SonarQube Analysis
      |
RuleForge Agent
      |
  - Policy Engine
  - Enforcement Engine
  - Deterministic Autofix
  - Azure AI Foundry Remediation (Optional)
      |
Governance Report
      |
CI Quality Gate Decision

---

# 2. Core Components

## CLI Layer (.NET Tool)

Command: ruleforge  
Modes: check, fix, autofix  

---

## Policy Engine

Reads:
- .qualityagent/policy.yml
- enforce-rules.txt

Defines:
- Severity thresholds
- Enforced rule keys
- AI remediation allowlist
- Reporting paths

---

## SonarQube Integration

Uses REST API:

/api/issues/search

Parses:
- Rule key
- Severity
- Issue count

---

## Enforcement Engine

Evaluates rule compliance and determines PASS or FAIL.

Exit Code:
- 0 = PASS
- 1 = FAIL

---

## Deterministic Autofix

Executes:

dotnet format

Used for safe, style-related corrections.

---

## Azure AI Foundry Remediation (Optional)

When enabled:
- Sends bounded file content
- Applies allowlisted rule corrections
- Applies patch safely

Safety Controls:
- Allowlisted rules
- File size limits
- Explicit policy toggle

---

# 3. Reporting

Outputs:
- artifacts/quality-report.md
- artifacts/quality-report.json

Supports:
- Governance review
- Dashboard integration
- CI enforcement

---

# 4. Enterprise Design Principles

- Policy-driven enforcement
- Deterministic default behavior
- Controlled AI augmentation
- Transparent governance reporting
