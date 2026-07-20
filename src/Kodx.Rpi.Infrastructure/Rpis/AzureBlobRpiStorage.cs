using Azure.Storage.Blobs;
using Kodx.Rpi.Application.Rpis;
using Kodx.Rpi.Domain.Rpis;
using Microsoft.Extensions.Options;

namespace Kodx.Rpi.Infrastructure.Rpis;

/// <summary>
/// Reproduz o formato já existente de segregação e tags do legado (Kodx.Producao.Infra.Azure/StorageBlob.cs):
/// container "rpi", path "{edicao}/{nomeArquivo}", tags "edicao"/"rpi"/"extensao".
/// </summary>
public sealed class AzureBlobRpiStorage(BlobServiceClient blobServiceClient, IOptions<RpiBlobStorageOptions> options) : IRpiBlobStorage
{
    public Task UploadPdfAsync(RpiTipo tipo, int edicao, string localPdfPath, CancellationToken cancellationToken) =>
        UploadAsync(tipo, edicao, localPdfPath, RpiFileNaming.PdfFileName(tipo, edicao), extensao: "pdf", cancellationToken);

    public Task UploadTxtAsync(RpiTipo tipo, int edicao, string localTxtPath, CancellationToken cancellationToken) =>
        UploadAsync(tipo, edicao, localTxtPath, RpiFileNaming.TxtFileName(tipo, edicao), extensao: "txt", cancellationToken);

    public async Task<Stream> DownloadPdfAsync(RpiTipo tipo, int edicao, CancellationToken cancellationToken)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(options.Value.ContainerName);
        var blobClient = containerClient.GetBlobClient($"{edicao}/{RpiFileNaming.PdfFileName(tipo, edicao)}");

        return await blobClient.OpenReadAsync(cancellationToken: cancellationToken);
    }

    private async Task UploadAsync(RpiTipo tipo, int edicao, string localPath, string fileName, string extensao, CancellationToken cancellationToken)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(options.Value.ContainerName);
        var blobClient = containerClient.GetBlobClient($"{edicao}/{fileName}");

        await blobClient.UploadAsync(localPath, overwrite: true, cancellationToken);

        var tags = new Dictionary<string, string>
        {
            ["edicao"] = edicao.ToString(),
            // Nome do tipo (ex: "Patentes", "ContratosTecnologia"), não o valor numérico do enum —
            // confirmado contra os blobs reais já existentes em produção (não o numérico sugerido
            // pela leitura do código-fonte do legado).
            ["rpi"] = tipo.ToString(),
            ["extensao"] = extensao
        };
        await blobClient.SetTagsAsync(tags, cancellationToken: cancellationToken);
    }
}
