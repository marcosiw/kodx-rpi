using System.Runtime.CompilerServices;
using Kodx.Rpi.Application.Rpis;
using Kodx.Rpi.Domain.Rpis;
using Microsoft.EntityFrameworkCore;

namespace Kodx.Rpi.Infrastructure.Persistence;

public sealed class PublicationRepository(KodxRpiDbContext dbContext) : IPublicationRepository
{
    public async Task ReplaceForEditionAsync(int rpiEditionId, IReadOnlyCollection<Publication> publications, CancellationToken cancellationToken)
    {
        var existingIds = await dbContext.Publications
            .Where(p => p.RpiEditionId == rpiEditionId)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (existingIds.Count > 0)
        {
            var existingNumeros = dbContext.PublicationNumeros.Where(n => existingIds.Contains(n.PublicationId));
            dbContext.PublicationNumeros.RemoveRange(existingNumeros);

            var existing = dbContext.Publications.Where(p => existingIds.Contains(p.Id));
            dbContext.Publications.RemoveRange(existing);
        }

        await dbContext.Publications.AddRangeAsync(publications, cancellationToken);

        // Precisa persistir agora pra obter os Ids gerados pelo banco antes de gravar os
        // PublicationNumero (FK) — mesmo padrão já usado em DownloadRpiEditionUseCase pra
        // RpiEdition/RpiProcessingAttempt. O SaveChanges final do use case (via IUnitOfWork)
        // ainda vai persistir os PublicationNumero adicionados logo abaixo, junto da
        // RpiProcessingAttempt da extração.
        await dbContext.SaveChangesAsync(cancellationToken);

        var numeroRows = publications.SelectMany(p => p.Numeros.Select(numero => new PublicationNumero(p.Id, numero)));
        await dbContext.PublicationNumeros.AddRangeAsync(numeroRows, cancellationToken);
    }

    public async IAsyncEnumerable<PublicationSearchResult> SearchByNumerosAsync(
        IReadOnlyCollection<string> numeros, RpiTipo? tipo, int? edicao, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var query =
            from p in dbContext.Publications
            join e in dbContext.RpiEditions on p.RpiEditionId equals e.Id
            where dbContext.PublicationNumeros.Any(n => n.PublicationId == p.Id && numeros.Contains(n.Numero))
            select new { Publication = p, e.Tipo, e.Edicao };

        if (tipo is { } t)
        {
            query = query.Where(x => x.Tipo == t);
        }

        if (edicao is { } ed)
        {
            query = query.Where(x => x.Edicao == ed);
        }

        var results = query.OrderBy(x => x.Edicao).ThenBy(x => x.Publication.Id).AsAsyncEnumerable();

        await foreach (var row in results.WithCancellation(cancellationToken))
        {
            yield return new PublicationSearchResult(row.Publication, row.Tipo, row.Edicao);
        }
    }
}
