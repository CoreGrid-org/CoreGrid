using System.Text.RegularExpressions;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Assets.DTOs;

namespace CoreGrid.Api.Features.Assets.Helpers;

/// <summary>
/// Evaluates the <see cref="AssetAttributeDefinition.ValidationRule"/> string
/// against a submitted attribute value.
///
/// Rule syntax — one or more comma-separated key:value pairs, e.g.:
///   "min:0,max:100"
///   "maxLength:255"
///   "regex:^\d{5}$"
///   "minDate:2020-01-01,maxDate:2099-12-31"
///
/// Supported rules by DataType:
///   NUMBER  → min, max
///   TEXT    → minLength, maxLength, regex
///   DATE    → minDate, maxDate
///   SELECT, BOOLEAN — ValidationRule is silently ignored (no useful constraints).
/// </summary>
internal static class AttributeValidationRuleEngine
{
    public static void Enforce(
        AssetAttributeDefinition definition,
        AssetAttributeValueRequest value)
    {
        if (string.IsNullOrWhiteSpace(definition.ValidationRule))
        {
            return;
        }

        var rules = ParseRules(definition.ValidationRule);

        switch (definition.DataType)
        {
            case "NUMBER":
                EnforceNumberRules(definition.Name, value.ValueNumber!.Value, rules);
                break;

            case "TEXT":
            case "SELECT":
                EnforceTextRules(definition.Name, value.ValueText!, rules);
                break;

            case "DATE":
                EnforceDateRules(definition.Name, value.ValueDate!.Value, rules);
                break;

            // BOOLEAN has no useful constraints.
        }
    }

    // -------------------------------------------------------------------------
    // NUMBER rules: min, max
    // -------------------------------------------------------------------------

    private static void EnforceNumberRules(
        string attributeName,
        decimal number,
        Dictionary<string, string> rules)
    {
        if (rules.TryGetValue("min", out var minStr))
        {
            if (!decimal.TryParse(minStr, out var min))
            {
                throw new InvalidOperationException(
                    $"Attribute '{attributeName}' has an invalid 'min' rule value '{minStr}'.");
            }

            if (number < min)
            {
                throw new InvalidOperationException(
                    $"Value for '{attributeName}' must be at least {min}.");
            }
        }

        if (rules.TryGetValue("max", out var maxStr))
        {
            if (!decimal.TryParse(maxStr, out var max))
            {
                throw new InvalidOperationException(
                    $"Attribute '{attributeName}' has an invalid 'max' rule value '{maxStr}'.");
            }

            if (number > max)
            {
                throw new InvalidOperationException(
                    $"Value for '{attributeName}' must be at most {max}.");
            }
        }
    }

    // -------------------------------------------------------------------------
    // TEXT rules: minLength, maxLength, regex
    // -------------------------------------------------------------------------

    private static void EnforceTextRules(
        string attributeName,
        string text,
        Dictionary<string, string> rules)
    {
        if (rules.TryGetValue("minlength", out var minLenStr))
        {
            if (!int.TryParse(minLenStr, out var minLen) || minLen < 0)
            {
                throw new InvalidOperationException(
                    $"Attribute '{attributeName}' has an invalid 'minLength' rule value '{minLenStr}'.");
            }

            if (text.Length < minLen)
            {
                throw new InvalidOperationException(
                    $"Value for '{attributeName}' must be at least {minLen} character(s) long.");
            }
        }

        if (rules.TryGetValue("maxlength", out var maxLenStr))
        {
            if (!int.TryParse(maxLenStr, out var maxLen) || maxLen < 0)
            {
                throw new InvalidOperationException(
                    $"Attribute '{attributeName}' has an invalid 'maxLength' rule value '{maxLenStr}'.");
            }

            if (text.Length > maxLen)
            {
                throw new InvalidOperationException(
                    $"Value for '{attributeName}' must be at most {maxLen} character(s) long.");
            }
        }

        if (rules.TryGetValue("regex", out var pattern))
        {
            bool matches;

            try
            {
                // Cap execution time to protect against catastrophic backtracking.
                matches = Regex.IsMatch(
                    text,
                    pattern,
                    RegexOptions.None,
                    TimeSpan.FromSeconds(1));
            }
            catch (RegexMatchTimeoutException)
            {
                throw new InvalidOperationException(
                    $"Validation for attribute '{attributeName}' timed out. Check the regex rule.");
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException(
                    $"Attribute '{attributeName}' has an invalid regex rule: {ex.Message}");
            }

            if (!matches)
            {
                throw new InvalidOperationException(
                    $"Value for '{attributeName}' does not match the required format.");
            }
        }
    }

    // -------------------------------------------------------------------------
    // DATE rules: maxDate only
    // (minDate is intentionally not supported — dates have a ceiling, not a floor)
    // -------------------------------------------------------------------------

    private static void EnforceDateRules(
        string attributeName,
        DateOnly date,
        Dictionary<string, string> rules)
    {
        if (rules.TryGetValue("maxdate", out var maxDateStr))
        {
            if (!DateOnly.TryParse(maxDateStr, out var maxDate))
            {
                throw new InvalidOperationException(
                    $"Attribute '{attributeName}' has an invalid 'maxDate' rule value '{maxDateStr}'.");
            }

            if (date > maxDate)
            {
                throw new InvalidOperationException(
                    $"Value for '{attributeName}' must be on or before {maxDate:yyyy-MM-dd}.");
            }
        }
    }

    // -------------------------------------------------------------------------
    // Rule string parser
    //
    // Splits "min:0,max:100" into { "min" -> "0", "max" -> "100" }.
    // Keys are lower-cased for case-insensitive matching.
    // Values may contain colons (e.g. regex patterns like "^\d{2}:\d{2}$"), so
    // only the first colon in each segment is used as the key-value separator.
    // -------------------------------------------------------------------------

    private static Dictionary<string, string> ParseRules(string ruleString)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var segments = ruleString.Split(',');

        foreach (var segment in segments)
        {
            var trimmed = segment.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            var colonIndex = trimmed.IndexOf(':');

            if (colonIndex <= 0)
            {
                // Malformed segment — skip silently to stay lenient.
                continue;
            }

            var key = trimmed[..colonIndex].Trim().ToLowerInvariant();
            var val = trimmed[(colonIndex + 1)..].Trim();

            result[key] = val;
        }

        return result;
    }
}
