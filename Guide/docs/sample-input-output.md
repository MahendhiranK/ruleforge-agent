# RuleForge Agent – Sample Input and Output

This document demonstrates how RuleForge Agent evaluates SonarQube findings, applies deterministic and AI-assisted remediation, and produces governance-ready reports.

---

# 1. Scenario Overview

Project: LoanProcessing.Api  
Platform: .NET 9  
SonarQube: Self-hosted instance  
Execution Mode: autofix  

Enforced Rules:

- csharpsquid:S1481  Unused local variables
- csharpsquid:S125   Sections of code should not be commented out
- csharpsquid:S1066  Collapsible if statements

---

# 2. Developer Code (Before Check-in)

```csharp
public class LoanService
{
    public decimal CalculateLoan(decimal amount)
    {
        int unused = 100;

        // if (amount > 0) return amount;

        if (amount > 0)
        {
            if (amount > 1000)
            {
                return amount * 1.05m;
            }
        }

        return amount;
    }
}
```

---

# 3. SonarQube Raw Issue Response

```json
{
  "issues": [
    { "rule": "csharpsquid:S1481", "severity": "MAJOR" },
    { "rule": "csharpsquid:S125", "severity": "MAJOR" },
    { "rule": "csharpsquid:S1066", "severity": "MINOR" }
  ]
}
```

---

# 4. RuleForge Agent Processing

## Enforcement Result (Initial)

| Rule | Severity | Status |
|------|----------|--------|
| S1481 | MAJOR | FAIL |
| S125  | MAJOR | FAIL |
| S1066 | MINOR | FAIL |

Exit Code: 1

---

## Deterministic Autofix

dotnet format is executed for safe formatting corrections.

---

## Azure AI Foundry Remediation (Allowlisted)

If enabled via policy, the agent submits bounded file content with rule-specific remediation instructions and applies returned corrections.

---

# 5. Code After Remediation

```csharp
public class LoanService
{
    public decimal CalculateLoan(decimal amount)
    {
        if (amount > 1000)
        {
            return amount * 1.05m;
        }

        return amount;
    }
}
```

---

# 6. Final Report Example

```
Rule Enforcement Summary
------------------------
S1481: PASS
S125:  PASS
S1066: PASS

Quality Gate: PASSED
Total Issues Before: 3
Total Issues After: 0
AI Remediation Applied: Yes
```

Exit Code: 0

---

# 7. Governance Impact

- Automated enforcement of architectural standards
- AI-assisted safe remediation
- Deterministic CI gate behavior
- Transparent reporting for technical leads
