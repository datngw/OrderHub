using System.Net;
using System.Text.RegularExpressions;
using Ganss.Xss;

namespace OrderHub.Application.Common.Security;

public static class HtmlInputSanitizer
{
    private static readonly HtmlSanitizer Sanitizer = CreateSanitizer();
    private static readonly Regex TagPattern = new(@"<[^>]*>", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    private static HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = new HtmlSanitizer();

        // Keep default allowed tags so text content inside tags is preserved.
        // Dangerous tags (script, iframe, etc.) are removed with their content.
        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedCssProperties.Clear();
        sanitizer.AllowedAtRules.Clear();

        sanitizer.AllowDataAttributes = false;

        return sanitizer;
    }

    public static string? Sanitize(string? input)
    {
        if (input is null)
            return null;

        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Remove dangerous HTML (scripts, events, malicious attributes)
        var sanitized = Sanitizer.Sanitize(input);

        // Strip remaining safe tags, preserve text content
        sanitized = TagPattern.Replace(sanitized, string.Empty);

        // Decode HTML entities (&amp; → &, &lt; → <)
        sanitized = WebUtility.HtmlDecode(sanitized);

        return string.IsNullOrWhiteSpace(sanitized) ? string.Empty : sanitized.Trim();
    }
}
