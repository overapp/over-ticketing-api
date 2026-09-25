using System.Globalization;
using Microsoft.OpenApi;

namespace Web.Api.Extensions;

internal static class OpenApiResponseExtensions
{
    private const string ValidationErrorDescription =
        "One or more validation errors occurred. The response body includes an `errors` array of `{ code, description }` objects.";

    /// <summary>
    /// Documents a possible <see cref="SharedKernel.Error"/> response for this endpoint: registers the
    /// status code as a ProblemDetails response (if not already registered) and appends the specific
    /// error code and its meaning to that response's description, so multiple distinct errors sharing
    /// the same status code all show up in Scalar.
    /// </summary>
    public static RouteHandlerBuilder ProducesError(
        this RouteHandlerBuilder builder,
        int statusCode,
        string errorCode,
        string description)
    {
        return builder
            .ProducesProblem(statusCode)
            .AddOpenApiOperationTransformer((operation, _, _) =>
            {
                string key = statusCode.ToString(CultureInfo.InvariantCulture);

                if (operation.Responses is not null &&
                    operation.Responses.TryGetValue(key, out IOpenApiResponse? existingResponse) &&
                    existingResponse is OpenApiResponse response)
                {
                    string entry = $"`{errorCode}` — {description}";
                    response.Description = string.IsNullOrEmpty(response.Description)
                        ? entry
                        : $"{response.Description}\n\n{entry}";
                }

                return Task.CompletedTask;
            });
    }

    /// <summary>
    /// Documents the generic 400 validation response raised by <c>ValidationDecorator</c> for any
    /// command with a registered <c>FluentValidation</c> validator.
    /// </summary>
    public static RouteHandlerBuilder ProducesValidationError(this RouteHandlerBuilder builder) =>
        builder.ProducesError(StatusCodes.Status400BadRequest, "Validation.General", ValidationErrorDescription);
}
