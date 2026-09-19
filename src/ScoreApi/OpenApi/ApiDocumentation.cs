using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ScoreApi.OpenApi;

public static class ApiDocumentation
{
    public const string BearerScheme = "Bearer";
}

// Defines the document title and the JWT (Bearer) authentication scheme.
internal sealed class ApiInfoTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title = "Unity Test Score API",
            Version = "v1",
            Description = "API that receives scores for the memory game. Set the token obtained from `POST /api/login` as the Bearer Token under Authentication below to try the authenticated endpoints.",
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[ApiDocumentation.BearerScheme] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter the token returned by `/api/login` as is (no `Bearer ` prefix).",
        };
        return Task.CompletedTask;
    }
}

// Adds the lock icon only to endpoints that have RequireAuthorization().
internal sealed class BearerSecurityTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var requiresAuth = context.Description.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>().Any();
        if (requiresAuth)
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(ApiDocumentation.BearerScheme, context.Document)] = [],
            });
        }
        return Task.CompletedTask;
    }
}
