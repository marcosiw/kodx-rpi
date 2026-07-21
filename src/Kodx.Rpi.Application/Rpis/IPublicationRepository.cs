using Kodx.Rpi.Domain.Rpis;

namespace Kodx.Rpi.Application.Rpis;

public interface IPublicationRepository
{
    /// <summary>Substitui todas as publicações já persistidas para a edição (idempotente em reprocessamentos).</summary>
    Task ReplaceForEditionAsync(int rpiEditionId, IReadOnlyCollection<Publication> publications, CancellationToken cancellationToken);

    /// <summary>
    /// Busca publicações por um conjunto de números de processo, em streaming. <paramref name="tipo"/>
    /// e <paramref name="edicao"/> são opcionais: se omitidos, busca em todo o histórico já
    /// processado (numero é indexado, então isso vira uma única consulta "= ANY(...)").
    /// </summary>
    IAsyncEnumerable<PublicationSearchResult> SearchByNumerosAsync(
        IReadOnlyCollection<string> numeros, RpiTipo? tipo, int? edicao, CancellationToken cancellationToken);
}

/// <summary>Publicação encontrada + tipo/edição de origem — necessário quando a busca abrange várias edições.</summary>
public sealed record PublicationSearchResult(Publication Publication, RpiTipo Tipo, int Edicao);
