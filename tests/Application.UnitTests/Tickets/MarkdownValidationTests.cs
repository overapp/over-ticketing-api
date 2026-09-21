using Application.Common.Validation;
using FluentValidation;
using FluentValidation.TestHelper;

namespace Application.UnitTests.Tickets;

public sealed class MarkdownValidationTests
{
    private readonly InlineMarkdownValidator _validator = new();

    private sealed class MarkdownModel
    {
        public string Content { get; init; } = string.Empty;
    }

    private sealed class InlineMarkdownValidator : AbstractValidator<MarkdownModel>
    {
        public InlineMarkdownValidator()
        {
            RuleFor(x => x.Content).ValidMarkdown();
        }
    }

    [Theory]
    [InlineData("Just plain text")]
    [InlineData("Paragraph 1\n\nParagraph 2 with **bold** and *italic* and ~~strikethrough~~")]
    [InlineData("# Heading 1\n## Heading 2\n### Heading 3")]
    [InlineData("- Item 1\n- Item 2\n  - Subitem 2.1")]
    [InlineData("1. First\n2. Second\n3. Third")]
    [InlineData("- [ ] Task item\n- [x] Completed task")]
    [InlineData("> This is a blockquote\n> spanning multiple lines")]
    [InlineData("| Col 1 | Col 2 |\n|---|---|\n| Val 1 | Val 2 |")]
    [InlineData("```csharp\npublic class Foo { }\n```")]
    [InlineData("```html\n<script>alert('inside code block is safe')</script>\n<img src=x onerror=alert(1)>\n```")]
    [InlineData("Inline code: `<script>alert('safe')</script>` and `<div>hello</div>`")]
    [InlineData("Here is a link: [Google](https://google.com) and [Secure](http://example.com)")]
    [InlineData("Contact us at [Support](mailto:support@example.com)")]
    [InlineData("Autolink: <https://example.com> and email autolink: <support@example.com>")]
    [InlineData("Image: ![Screenshot](https://example.com/image.png) or ![Http](http://example.com/pic.jpg)")]
    [InlineData("[Reference link][ref]\n\n[ref]: https://example.com")]
    [InlineData("```\nUnclosed code block at end of text")]
    public void ValidMarkdown_Should_PassValidation(string markdown)
    {
        var model = new MarkdownModel { Content = markdown };

        TestValidationResult<MarkdownModel> result = _validator.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("Some text <script src=\"https://evil.com/payload.js\"></script> more text")]
    [InlineData("<img src=\"x\" onerror=\"alert(1)\">")]
    [InlineData("<svg onload=\"alert(1)\">")]
    [InlineData("<iframe src=\"https://evil.com\"></iframe>")]
    [InlineData("<a href=\"https://example.com\">HTML link</a>")]
    [InlineData("<div class=\"container\">text</div>")]
    [InlineData("<b>raw HTML bold</b>")]
    [InlineData("<!-- HTML comment -->")]
    [InlineData("<style>body { display: none; }</style>")]
    [InlineData("<object data=\"payload.swf\"></object>")]
    [InlineData("<embed src=\"payload.swf\">")]
    [InlineData("<link rel=\"stylesheet\" href=\"http://evil.com/style.css\">")]
    [InlineData("<!DOCTYPE html>")]
    [InlineData("<?xml version=\"1.0\"?>")]
    public void ValidMarkdown_Should_Fail_WhenRawHtmlIsPresent(string markdown)
    {
        var model = new MarkdownModel { Content = markdown };

        TestValidationResult<MarkdownModel> result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorCode(MarkdownValidationExtensions.RawHtmlErrorCode)
            .WithErrorMessage(MarkdownValidationExtensions.RawHtmlErrorMessage);
    }

    [Theory]
    [InlineData("[Click me](javascript:alert(1))")]
    [InlineData("[Click me](JAVASCRIPT:alert(1))")]
    [InlineData("[Click me](jav&#x61;script:alert(1))")]
    [InlineData("[Click me](data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==)")]
    [InlineData("[Click me](vbscript:msgbox(1))")]
    [InlineData("[Click me](file:///etc/passwd)")]
    [InlineData("[Click me](/relative/path)")]
    [InlineData("[Click me]()")]
    [InlineData("![Image](javascript:alert(1))")]
    [InlineData("![Image](data:image/png;base64,iVBORw0KGgo=)")]
    [InlineData("![Image](mailto:someone@example.com)")]
    [InlineData("![Image](/relative/img.png)")]
    [InlineData("![Image]()")]
    [InlineData("[Bad Ref][ref]\n\n[ref]: javascript:alert(1)")]
    [InlineData("<ftp://ftp.example.com/file.zip>")]
    public void ValidMarkdown_Should_Fail_WhenUnsafeUrlIsPresent(string markdown)
    {
        var model = new MarkdownModel { Content = markdown };

        TestValidationResult<MarkdownModel> result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorCode(MarkdownValidationExtensions.InvalidUrlErrorCode)
            .WithErrorMessage(MarkdownValidationExtensions.InvalidUrlErrorMessage);
    }
}
