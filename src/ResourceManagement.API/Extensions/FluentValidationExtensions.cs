using FluentValidation;

namespace ResourceManagement.API;

/// <summary>Extension method alias for FluentValidation ASP.NET Core integration.</summary>
public static class FluentValidationExtensions
{
    public static IServiceCollection AddFluentValidationAutoValidation(this IServiceCollection services)
    {
        // Wire up FluentValidation with ASP.NET Core model validation
        services.AddValidatorsFromAssemblyContaining<Program>();
        return services;
    }
}
