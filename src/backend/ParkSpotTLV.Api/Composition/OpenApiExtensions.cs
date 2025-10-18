using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;

namespace ParkSpotTLV.Api.Composition {

    /* 
     * OpenAPI Extension
     */
    public static class OpenApiExtensions {
        public static IServiceCollection AddCustomOpenApi(this IServiceCollection services) {
            services.AddOpenApi(options => {
                // Add a global Bearer security scheme
                options.AddDocumentTransformer((document, context, cancellationToken) => {
                    document.Components ??= new();
                    document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();

                    document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme {
                        Name = "Authorization",
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        In = ParameterLocation.Header,
                        Description = "Enter: {your JWT}"
                    };

                    return Task.CompletedTask;
                });

                // Auto-apply Bearer requirement for authorized endpoints
                options.AddOperationTransformer((operation, context, cancellationToken) => {
                    var requiresAuth = context.Description?.ActionDescriptor?.EndpointMetadata?
                        .OfType<IAuthorizeData>()?.Any() == true;

                    if (requiresAuth) {
                        operation.Security ??= [];
                        operation.Security.Add(new OpenApiSecurityRequirement
                        {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            Array.Empty<string>()
                        }
                    });
                    }

                    return Task.CompletedTask;
                });
            });

            return services;
        }

        public static IEndpointRouteBuilder MapCustomOpenApi(this IEndpointRouteBuilder endpoints) {
            endpoints.MapOpenApi();
            endpoints.MapScalarApiReference(options =>
            {
                options.Title = "ParkSpotTLV API";
                options.Theme = ScalarTheme.BluePlanet;
                options.DarkMode = true;
            });

            return endpoints;
        }
    }
}