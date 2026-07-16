using Microsoft.Extensions.Options;

namespace Kodx.Rpi.Api.Security;

public sealed class ApiKeyMiddleware(RequestDelegate next, ILogger<ApiKeyMiddleware> logger)
{
    private static readonly string[] ExemptPathPrefixes = ["/health", "/swagger"];

    public async Task InvokeAsync(HttpContext context, IOptions<ApiKeyOptions> options)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (ExemptPathPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        var apiKeyOptions = options.Value;

        if (!context.Request.Headers.TryGetValue(apiKeyOptions.HeaderName, out var providedKey) ||
            string.IsNullOrEmpty(providedKey))
        {
            logger.LogWarning("Requisição rejeitada: header {HeaderName} ausente em {Path}", apiKeyOptions.HeaderName, path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("API key ausente.");
            return;
        }

        if (!string.Equals(providedKey, apiKeyOptions.Value, StringComparison.Ordinal))
        {
            logger.LogWarning("Requisição rejeitada: API key inválida em {Path}", path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("API key inválida.");
            return;
        }

        await next(context);
    }
}
