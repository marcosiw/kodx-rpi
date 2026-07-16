namespace Kodx.Rpi.Api.Security;

public sealed class ApiKeyOptions
{
    public const string SectionName = "ApiKey";

    public string HeaderName { get; set; } = "X-Api-Key";

    public string Value { get; set; } = string.Empty;
}
