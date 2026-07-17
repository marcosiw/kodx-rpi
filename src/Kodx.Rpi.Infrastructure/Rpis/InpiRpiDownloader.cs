using Kodx.Rpi.Application.Rpis;
using Kodx.Rpi.Domain.Rpis;

namespace Kodx.Rpi.Infrastructure.Rpis;

public sealed class InpiRpiDownloader(HttpClient httpClient) : IRpiDownloader
{
    public async Task<byte[]> DownloadAsync(RpiTipo tipo, int edicao, CancellationToken cancellationToken)
    {
        var fileName = RpiFileNaming.PdfFileName(tipo, edicao);

        try
        {
            using var response = await httpClient.GetAsync(fileName, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new RpiDownloadException(
                    $"INPI respondeu {(int)response.StatusCode} ({response.StatusCode}) para '{fileName}'.");
            }

            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new RpiDownloadException($"Falha de rede ao baixar '{fileName}' do INPI.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new RpiDownloadException($"Timeout ao baixar '{fileName}' do INPI.", ex);
        }
    }
}
