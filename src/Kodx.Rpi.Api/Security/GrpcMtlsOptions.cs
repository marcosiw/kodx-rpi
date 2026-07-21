namespace Kodx.Rpi.Api.Security;

public sealed class GrpcMtlsOptions
{
    public const string SectionName = "Grpc:Mtls";

    public bool Enabled { get; set; } = true;

    public string ServerCertificatePath { get; set; } = string.Empty;

    public string ServerKeyPath { get; set; } = string.Empty;

    public string ClientCaCertificatePath { get; set; } = string.Empty;
}
