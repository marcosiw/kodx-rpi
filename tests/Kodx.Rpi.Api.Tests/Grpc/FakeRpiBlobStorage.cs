using Kodx.Rpi.Application.Rpis;
using Kodx.Rpi.Domain.Rpis;

namespace Kodx.Rpi.Api.Tests.Grpc;

public sealed class FakeRpiBlobStorage : IRpiBlobStorage
{
    public byte[] PdfContent { get; set; } = [];

    public Task UploadPdfAsync(RpiTipo tipo, int edicao, string localPdfPath, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Não usado nestes testes.");

    public Task UploadTxtAsync(RpiTipo tipo, int edicao, string localTxtPath, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Não usado nestes testes.");

    public Task<Stream> DownloadPdfAsync(RpiTipo tipo, int edicao, CancellationToken cancellationToken) =>
        Task.FromResult<Stream>(new MemoryStream(PdfContent));
}
