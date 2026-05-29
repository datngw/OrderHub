using FluentAssertions;
using OrderHub.Application.Common.Security;

namespace OrderHub.UnitTests.Shared.Security;

public class HtmlInputSanitizerTests
{
    [Fact]
    public void Sanitize_WithNullInput_ReturnsNull()
    {
        HtmlInputSanitizer.Sanitize(null).Should().BeNull();
    }

    [Fact]
    public void Sanitize_WithEmptyString_ReturnsEmptyString()
    {
        HtmlInputSanitizer.Sanitize(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void Sanitize_WithPlainText_ReturnsUnchanged()
    {
        HtmlInputSanitizer.Sanitize("Widget Pro").Should().Be("Widget Pro");
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>", "")]
    [InlineData("<img src=x onerror=alert(1)>", "")]
    [InlineData("<iframe src=\"https://evil.com\"></iframe>", "")]
    [InlineData("<a href=\"javascript:alert(1)\">click</a>", "click")]
    [InlineData("<div onload=\"alert(1)\">hello</div>", "hello")]
    [InlineData("Hello <b>World</b>", "Hello World")]
    [InlineData("<p>Description text</p>", "Description text")]
    public void Sanitize_StripsHtmlTags(string input, string expected)
    {
        HtmlInputSanitizer.Sanitize(input).Should().Be(expected);
    }

    [Fact]
    public void Sanitize_WithScriptTagInName_StripsScript()
    {
        var input = "<script>document.cookie</script>Product Name";
        HtmlInputSanitizer.Sanitize(input).Should().Be("Product Name");
    }

    [Fact]
    public void Sanitize_WithEventAttribute_StripsAttribute()
    {
        var input = "\" onmouseover=\"alert(1)";
        HtmlInputSanitizer.Sanitize(input).Should().Be("\" onmouseover=\"alert(1)");
    }

    [Fact]
    public void Sanitize_WithSvgXss_StripsSvg()
    {
        var input = "<svg onload=alert(1)>";
        HtmlInputSanitizer.Sanitize(input).Should().BeEmpty();
    }

    [Fact]
    public void Sanitize_WithMixedContent_StripsOnlyHtml()
    {
        var input = "Use <script>alert('xss')</script> for testing the <b>Product</b> named Widget";
        HtmlInputSanitizer.Sanitize(input).Should().Be("Use  for testing the Product named Widget");
    }

    [Fact]
    public void Sanitize_WithWhitespaceOnly_ReturnsEmptyString()
    {
        HtmlInputSanitizer.Sanitize("   ").Should().BeEmpty();
    }

    [Fact]
    public void Sanitize_WithLeadingTrailingWhitespace_Trimmed()
    {
        HtmlInputSanitizer.Sanitize("  Widget  ").Should().Be("Widget");
    }
}
