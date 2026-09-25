using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Web.Api.OpenApi;

internal sealed class BearerSecurityRequirementOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        IList<object> endpointMetadata = context.Description.ActionDescriptor.EndpointMetadata;

        bool requiresAuthentication = endpointMetadata.OfType<IAuthorizeData>().Any()
            && !endpointMetadata.OfType<IAllowAnonymous>().Any();

        if (!requiresAuthentication)
        {
            return Task.CompletedTask;
        }

        var requirement = new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, context.Document, externalResource: null)] = []
        };

        operation.Security ??= [];
        operation.Security.Add(requirement);

        return Task.CompletedTask;
    }
}
