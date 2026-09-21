using FluentValidation;
using FluentValidation.Results;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Application.Common.Validation;

public static class MarkdownValidationExtensions
{
    public const string RawHtmlErrorCode = "Markdown.RawHtmlNotAllowed";
    public const string RawHtmlErrorMessage = "The message contains raw HTML tags, which are not allowed. Use code blocks to share code or markup.";

    public const string InvalidUrlErrorCode = "Markdown.InvalidUrl";
    public const string InvalidUrlErrorMessage = "Links and images must use valid and secure URLs (http or https for images; http, https, or mailto for links).";

    public const string InvalidFormatErrorCode = "Markdown.InvalidFormat";
    public const string InvalidFormatErrorMessage = "The message is not a valid markdown format.";

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public static IRuleBuilderOptionsConditions<T, string> ValidMarkdown<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.Custom((markdown, context) =>
        {
            if (string.IsNullOrWhiteSpace(markdown))
            {
                return;
            }

            ValidateMarkdown(markdown, context);
        });
    }

    private static void ValidateMarkdown<T>(string markdown, ValidationContext<T> context)
    {
        MarkdownDocument document;
        try
        {
            document = Markdown.Parse(markdown, Pipeline);
        }
        catch (Exception)
        {
            context.AddFailure(new ValidationFailure(context.PropertyPath, InvalidFormatErrorMessage)
            {
                ErrorCode = InvalidFormatErrorCode
            });

            return;
        }

        bool hasHtmlBlock = document.Descendants<HtmlBlock>().Any();
        bool hasHtmlInline = document.Descendants<HtmlInline>().Any();

        if (hasHtmlBlock || hasHtmlInline)
        {
            context.AddFailure(new ValidationFailure(context.PropertyPath, RawHtmlErrorMessage)
            {
                ErrorCode = RawHtmlErrorCode
            });
        }

        bool hasInvalidUrl = false;

        foreach (LinkInline link in document.Descendants<LinkInline>())
        {
            if (!IsValidUrl(link.Url, isImage: link.IsImage))
            {
                hasInvalidUrl = true;
                break;
            }
        }

        if (!hasInvalidUrl)
        {
            foreach (AutolinkInline autolink in document.Descendants<AutolinkInline>())
            {
                if (autolink.IsEmail)
                {
                    continue;
                }

                if (!IsValidUrl(autolink.Url, isImage: false))
                {
                    hasInvalidUrl = true;
                    break;
                }
            }
        }

        if (!hasInvalidUrl)
        {
            foreach (LinkReferenceDefinition linkRef in document.Descendants<LinkReferenceDefinition>())
            {
                if (!IsValidUrl(linkRef.Url, isImage: false))
                {
                    hasInvalidUrl = true;
                    break;
                }
            }
        }

        if (hasInvalidUrl)
        {
            context.AddFailure(new ValidationFailure(context.PropertyPath, InvalidUrlErrorMessage)
            {
                ErrorCode = InvalidUrlErrorCode
            });
        }
    }

    private static bool IsValidUrl(string? url, bool isImage)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        string trimmedUrl = url.Trim();

        if (!Uri.TryCreate(trimmedUrl, UriKind.Absolute, out Uri? uri))
        {
            return false;
        }

        if (isImage)
        {
            return string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(uri.Scheme, Uri.UriSchemeMailto, StringComparison.OrdinalIgnoreCase);
    }
}
