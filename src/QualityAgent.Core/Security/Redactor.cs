namespace QualityAgent.Core.Security;

/// <summary>
/// Strips known secret values out of text before it is logged or included
/// in an exception message, so a misbehaving remote API can never cause a
/// token/key to end up in console output, CI logs, or report files.
/// </summary>
public static class Redactor
{
    private const string Mask = "***REDACTED***";
    private const int MaxBodyChars = 2000;

    public static string Scrub(string? text, params string?[] secrets)
    {
        if (string.IsNullOrEmpty(text)) return text ?? "";

        var result = text;
        foreach (var secret in secrets)
        {
            if (!string.IsNullOrWhiteSpace(secret) && result.Contains(secret, StringComparison.Ordinal))
                result = result.Replace(secret, Mask, StringComparison.Ordinal);
        }

        if (result.Length > MaxBodyChars)
            result = result[..MaxBodyChars] + $"... (truncated, {result.Length - MaxBodyChars} more chars)";

        return result;
    }
}
