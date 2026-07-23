using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace Kodx.Rpi.Api.Security;

public static class GrpcMtlsSetup
{
    public static void Configure(KestrelServerOptions serverOptions, GrpcMtlsOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ServerCertificatePath) || string.IsNullOrWhiteSpace(options.ServerKeyPath))
        {
            throw new InvalidOperationException(
                $"{GrpcMtlsOptions.SectionName}:ServerCertificatePath e {GrpcMtlsOptions.SectionName}:ServerKeyPath são obrigatórios " +
                "(TLS é sempre exigido no endpoint gRPC, com ou sem mTLS). Gere os certificados com scripts/generate-mtls-certs.sh.");
        }

        var serverCertificate = X509Certificate2.CreateFromPemFile(options.ServerCertificatePath, options.ServerKeyPath);

        X509Certificate2? trustedClientCa = null;
        if (options.Enabled)
        {
            if (string.IsNullOrWhiteSpace(options.ClientCaCertificatePath))
            {
                throw new InvalidOperationException(
                    $"{GrpcMtlsOptions.SectionName}:ClientCaCertificatePath é obrigatório quando {GrpcMtlsOptions.SectionName}:Enabled=true.");
            }

            // X509Certificate2.CreateFromPemFile(path) sem keyPemFilePath tenta extrair uma chave
            // privada do próprio arquivo (keyContents = certContents por padrão) - falha sempre
            // pra um certificado de CA, que não tem chave. X509CertificateLoader é a API certa
            // pra carregar só o certificado, sem chave.
            trustedClientCa = X509CertificateLoader.LoadCertificateFromFile(options.ClientCaCertificatePath);
        }

        serverOptions.ConfigureHttpsDefaults(https =>
        {
            https.ServerCertificate = serverCertificate;
            https.ClientCertificateMode = options.Enabled
                ? ClientCertificateMode.RequireCertificate
                : ClientCertificateMode.AllowCertificate;

            https.ClientCertificateValidation = (clientCertificate, _, _) =>
                !options.Enabled || IsSignedByTrustedCa(clientCertificate, trustedClientCa!);
        });
    }

    private static bool IsSignedByTrustedCa(X509Certificate2 clientCertificate, X509Certificate2 trustedCa)
    {
        using var chain = new X509Chain
        {
            ChainPolicy = new X509ChainPolicy
            {
                TrustMode = X509ChainTrustMode.CustomRootTrust,
                RevocationMode = X509RevocationMode.NoCheck,
            },
        };
        chain.ChainPolicy.CustomTrustStore.Add(trustedCa);

        return chain.Build(clientCertificate);
    }
}
