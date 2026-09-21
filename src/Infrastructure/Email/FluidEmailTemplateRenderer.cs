using System.Collections.Concurrent;
using System.Reflection;
using Application.Abstractions.Emails;
using Fluid;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Email;

internal sealed class FluidEmailTemplateRenderer : IEmailTemplateRenderer
{
    private static readonly FluidParser Parser = new();
    private static readonly ConcurrentDictionary<string, IFluidTemplate> TemplateCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Assembly CurrentAssembly = typeof(FluidEmailTemplateRenderer).Assembly;

    public async Task<string> RenderAsync(string templateName, object model, CancellationToken cancellationToken = default)
    {
        IFluidTemplate contentTemplate = GetOrLoadTemplate(templateName);

        var context = new TemplateContext(model);
        context.Options.MemberAccessStrategy.Register(model.GetType());

        string body = await contentTemplate.RenderAsync(context);

        // Check if layout exists
        if (!string.Equals(templateName, "_layout", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                IFluidTemplate layoutTemplate = GetOrLoadTemplate("_layout");
                context.SetValue("content", body);
                return await layoutTemplate.RenderAsync(context);
            }
            catch (FileNotFoundException)
            {
                // Layout is optional, return body as is if _layout is not found
                return body;
            }
        }

        return body;
    }

    private static IFluidTemplate GetOrLoadTemplate(string templateName)
    {
        return TemplateCache.GetOrAdd(templateName, name =>
        {
            string templateContent = LoadTemplateContent(name);
            if (!Parser.TryParse(templateContent, out IFluidTemplate? template, out string? error))
            {
                throw new InvalidOperationException($"Failed to parse Liquid template '{name}': {error}");
            }

            return template;
        });
    }

    private static string LoadTemplateContent(string templateName)
    {
        string normalizedName = templateName.EndsWith(".liquid", StringComparison.OrdinalIgnoreCase)
            ? templateName
            : $"{templateName}.liquid";

        string[] resourceNames = CurrentAssembly.GetManifestResourceNames();
        string targetResource = resourceNames.FirstOrDefault(r =>
            r.EndsWith($".{normalizedName}", StringComparison.OrdinalIgnoreCase) ||
            r.EndsWith($".Templates.{normalizedName}", StringComparison.OrdinalIgnoreCase))
            ?? throw new FileNotFoundException($"Embedded email template '{templateName}' was not found.");

        using Stream stream = CurrentAssembly.GetManifestResourceStream(targetResource)
            ?? throw new FileNotFoundException($"Unable to open stream for embedded template '{targetResource}'.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
