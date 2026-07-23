using System.Globalization;
using Google.Protobuf;
using Grpc.Core;
using Kodx.Rpi.Application.Rpis;
using DomainRpiTipo = Kodx.Rpi.Domain.Rpis.RpiTipo;

namespace Kodx.Rpi.Api.Grpc;

/// <summary>
/// Endpoints privados consumidos pelo Kodx API (e outros serviços consumidores) — substitui o
/// antigo RpiDownloadController (REST) e adiciona consulta de edição, download de PDF e busca
/// de publicações (item 8 do plano, ver ai/context.md).
/// </summary>
public sealed class RpiGrpcService(
    IBackgroundTaskQueue taskQueue,
    IRpiEditionRepository editionRepository,
    IRpiProcessingAttemptRepository attemptRepository,
    IPublicationRepository publicationRepository,
    IRpiBlobStorage blobStorage) : RpiService.RpiServiceBase
{
    private const int PdfChunkSizeBytes = 64 * 1024;

    public override Task<TriggerDownloadReply> TriggerDownload(TriggerDownloadRequest request, ServerCallContext context)
    {
        var tipo = (DomainRpiTipo)request.Tipo;
        int? edicao = request.HasEdicao ? request.Edicao : null;

        taskQueue.Enqueue((services, cancellationToken) =>
            services.GetRequiredService<RunRpiPipelineUseCase>().ExecuteAsync(tipo, edicao, cancellationToken));

        // A edição real só é conhecida depois de resolvida em background (calendário do INPI)
        // quando não vier explícita — mesmo comportamento de "202 Accepted" do endpoint REST anterior.
        var reply = new TriggerDownloadReply { Tipo = request.Tipo };
        if (edicao is { } explicitEdicao)
        {
            reply.Edicao = explicitEdicao;
        }

        return Task.FromResult(reply);
    }

    public override async Task<RpiHistoryReply> GetRpiHistory(GetRpiHistoryRequest request, ServerCallContext context)
    {
        var tipo = (DomainRpiTipo)request.Tipo;
        var edition = await editionRepository.FindAsync(tipo, request.Edicao, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Edição {request.Edicao} de {tipo} não encontrada."));

        var attempts = await attemptRepository.ListForEditionAsync(edition.Id, context.CancellationToken);

        var reply = new RpiHistoryReply
        {
            Edicao = edition.Edicao,
            Tipo = request.Tipo,
            DataPublicacao = edition.DataPublicacao.ToString("O", CultureInfo.InvariantCulture)
        };

        reply.Attempts.AddRange(attempts.Select(a => new ProcessingAttempt
        {
            Stage = (ProcessingStage)a.Stage,
            Status = (ProcessingStatus)a.Status,
            ErrorMessage = a.ErrorMessage ?? string.Empty,
            StartedAt = a.StartedAt.ToString("O", CultureInfo.InvariantCulture),
            FinishedAt = a.FinishedAt.ToString("O", CultureInfo.InvariantCulture)
        }));

        return reply;
    }

    public override async Task DownloadPdf(DownloadPdfRequest request, IServerStreamWriter<DownloadPdfChunk> responseStream, ServerCallContext context)
    {
        var tipo = (DomainRpiTipo)request.Tipo;
        _ = await editionRepository.FindAsync(tipo, request.Edicao, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Edição {request.Edicao} de {tipo} não encontrada."));

        Stream pdfStream;
        try
        {
            pdfStream = await blobStorage.DownloadPdfAsync(tipo, request.Edicao, context.CancellationToken);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == StatusCodes.Status404NotFound)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"PDF de {tipo}/{request.Edicao} não encontrado no Blob Storage."));
        }

        await using (pdfStream)
        {
            var buffer = new byte[PdfChunkSizeBytes];
            int bytesRead;
            while ((bytesRead = await pdfStream.ReadAsync(buffer, context.CancellationToken)) > 0)
            {
                await responseStream.WriteAsync(new DownloadPdfChunk { Data = ByteString.CopyFrom(buffer, 0, bytesRead) }, context.CancellationToken);
            }
        }
    }

    public override async Task SearchRpiPublications(SearchRpiPublicationsRequest request, IServerStreamWriter<PublicationReply> responseStream, ServerCallContext context)
    {
        if (request.Numeros.Count == 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Informe ao menos um número de processo."));
        }

        if (request.HasTipo != request.HasEdicao)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "tipo e edicao devem ser informados juntos, ou nenhum dos dois (busca em todo o histórico)."));
        }

        DomainRpiTipo? tipo = request.HasTipo ? (DomainRpiTipo)request.Tipo : null;
        int? edicao = request.HasEdicao ? request.Edicao : null;

        if (tipo is { } t && edicao is { } e)
        {
            _ = await editionRepository.FindAsync(t, e, context.CancellationToken)
                ?? throw new RpcException(new Status(StatusCode.NotFound, $"Edição {e} de {t} não encontrada."));
        }

        await foreach (var match in publicationRepository.SearchByNumerosAsync(request.Numeros, tipo, edicao, context.CancellationToken))
        {
            var reply = new PublicationReply
            {
                Numero = match.Publication.Numero,
                Cabecalho1 = match.Publication.Payload.Cabecalho1 ?? string.Empty,
                Cabecalho2 = match.Publication.Payload.Cabecalho2 ?? string.Empty,
                Cabecalho3 = match.Publication.Payload.Cabecalho3 ?? string.Empty,
                Conteudo = match.Publication.Payload.Conteudo ?? string.Empty,
                IndexInicio = match.Publication.Payload.IndexInicio,
                IndexFim = match.Publication.Payload.IndexFim,
                Pagina = match.Publication.Payload.Pagina,
                Tipo = (RpiTipo)match.Tipo,
                Edicao = match.Edicao
            };
            reply.TodosNumeros.AddRange(match.Publication.Payload.TodosNumeros);

            await responseStream.WriteAsync(reply, context.CancellationToken);
        }
    }
}
