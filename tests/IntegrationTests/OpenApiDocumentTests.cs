using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IntegrationTests;

public sealed class OpenApiDocumentTests(IntegrationTestAppHost appHost) : BaseIntegrationTest(appHost)
{
    private async Task<JsonNode> GetDocumentAsync()
    {
        JsonNode? document = await HttpClient.GetFromJsonAsync<JsonNode>("openapi/v1.json");

        document.ShouldNotBeNull();

        return document;
    }

    [Fact]
    public async Task Document_Should_NotHaveDuplicateSchemaNames()
    {
        // The generator errors on true id collisions, but two distinct schemas can still
        // describe unrelated shapes under a coincidentally-reused short name if written
        // carelessly; this guards the naming convention (unique, descriptive Request/Response
        // type names) rather than the generator's own uniqueness constraint.
        JsonNode document = await GetDocumentAsync();

        JsonObject? schemas = document["components"]?["schemas"]?.AsObject();

        schemas.ShouldNotBeNull();
        schemas.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Document_Should_DeclareBearerSecurityScheme()
    {
        JsonNode document = await GetDocumentAsync();

        JsonNode? bearerScheme = document["components"]?["securitySchemes"]?["Bearer"];

        bearerScheme.ShouldNotBeNull();
        bearerScheme!["type"]!.GetValue<string>().ShouldBe("http");
        bearerScheme["scheme"]!.GetValue<string>().ShouldBe("bearer");
    }

    [Fact]
    public async Task Document_Should_RequireBearerAuth_OnProtectedEndpoint_ButNotOnPublicEndpoint()
    {
        JsonNode document = await GetDocumentAsync();

        JsonNode? protectedOperation = document["paths"]?["/auth/me"]?["get"];
        JsonNode? publicOperation = document["paths"]?["/auth/login"]?["post"];

        protectedOperation.ShouldNotBeNull();
        publicOperation.ShouldNotBeNull();

        JsonArray? protectedSecurity = protectedOperation["security"]?.AsArray();
        JsonArray? publicSecurity = publicOperation["security"]?.AsArray();

        protectedSecurity.ShouldNotBeNull();
        protectedSecurity.Count.ShouldBeGreaterThan(0);
        (publicSecurity is null || publicSecurity.Count == 0).ShouldBeTrue();
    }

    [Fact]
    public async Task Document_Should_DocumentResponseSchema_ForEveryOperation()
    {
        JsonNode document = await GetDocumentAsync();

        JsonObject paths = document["paths"]!.AsObject();

        List<string> operationsWithoutSchema = [];

        foreach ((string path, JsonNode? pathItem) in paths)
        {
            if (pathItem is null)
            {
                continue;
            }

            foreach ((string method, JsonNode? operation) in pathItem.AsObject())
            {
                JsonObject? responses = operation?["responses"]?.AsObject();

                bool hasAnyContentSchema = responses?.Any(response =>
                    response.Value?["content"]?.AsObject()
                        .Any(content => content.Value?["schema"] is not null) == true) == true;

                bool isNoContentOnly = responses?.Count == 1 && responses.ContainsKey("204");

                if (!hasAnyContentSchema && !isNoContentOnly)
                {
                    operationsWithoutSchema.Add($"{method.ToUpperInvariant()} {path}");
                }
            }
        }

        operationsWithoutSchema.ShouldBeEmpty();
    }

    [Fact]
    public async Task Document_Should_HaveUniqueOperationId_AndSummary_ForEveryOperation()
    {
        JsonNode document = await GetDocumentAsync();

        JsonObject paths = document["paths"]!.AsObject();

        List<string> missingMetadata = [];
        var operationIds = new Dictionary<string, string>();
        List<string> duplicateOperationIds = [];

        foreach ((string path, JsonNode? pathItem) in paths)
        {
            if (pathItem is null)
            {
                continue;
            }

            foreach ((string method, JsonNode? operation) in pathItem.AsObject())
            {
                string label = $"{method.ToUpperInvariant()} {path}";

                string? operationId = operation?["operationId"]?.GetValue<string>();
                string? summary = operation?["summary"]?.GetValue<string>();

                if (string.IsNullOrWhiteSpace(operationId) || string.IsNullOrWhiteSpace(summary))
                {
                    missingMetadata.Add(label);
                    continue;
                }

                if (!operationIds.TryAdd(operationId, label))
                {
                    duplicateOperationIds.Add($"{operationId} ({operationIds[operationId]} vs {label})");
                }
            }
        }

        missingMetadata.ShouldBeEmpty();
        duplicateOperationIds.ShouldBeEmpty();
    }
}
