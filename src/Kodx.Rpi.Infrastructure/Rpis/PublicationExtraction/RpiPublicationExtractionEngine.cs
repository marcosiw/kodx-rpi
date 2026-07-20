using System.Globalization;
using System.Text.RegularExpressions;
using Kodx.Rpi.Application.Rpis;
using Kodx.Rpi.Domain.Rpis;

namespace Kodx.Rpi.Infrastructure.Rpis.PublicationExtraction;

/// <summary>
/// Motor genérico de quebra de RPI em publicações (porta de RegrasDeNegocio/Index.cs +
/// RegrasDeNegocio/ProcessamentoDados.cs do legado), guiado pelas regras por tipo em
/// <see cref="PublicationTypeRules"/>. Não porta o cálculo de dados fixos (Revista/Caderno/
/// Edição/Data), pois esses já vêm do <see cref="RpiEdition"/> desta aplicação, nem o
/// identificador de tarefa (específico de Marcas, fora do escopo do payload de publicação
/// desta fase) — ambos irrelevantes pra reconstruir <see cref="PublicationPayload"/>.
/// </summary>
internal static class RpiPublicationExtractionEngine
{
    public static IReadOnlyList<ExtractedPublication> Extract(string texto, PublicationTypeRules rules)
    {
        // As regras (portadas do legado) usam "\r\n" como quebra de linha nos termos literais
        // de cabeçalho, mas o PdfTextExtractor desta aplicação (fase 5) gera texto só com "\n"
        // — descoberto testando contra um TXT real (edição 2891/Patentes), onde os cabeçalhos
        // vinham sempre vazios apesar do texto batendo visualmente. Normaliza pra CRLF aqui,
        // sem alterar os termos portados, pra eles continuarem batendo com o texto real.
        texto = texto.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");

        var continuityMarkers = new List<PublicationMarker>();
        foreach (var term in rules.SearchTerms.Where(t => t.BreaksContinuity))
        {
            continuityMarkers.AddRange(FindAllOccurrences(texto, term));
        }

        continuityMarkers.AddRange(FindSyntheticMarkers(texto, rules));
        continuityMarkers = [.. continuityMarkers.OrderBy(m => m.Start)];
        AssignEnds(continuityMarkers, texto.Length);

        var pageMarkers = rules.SearchTerms
            .Where(t => !t.BreaksContinuity)
            .SelectMany(t => FindAllOccurrences(texto, t))
            .OrderBy(m => m.Start)
            .ToList();

        var pageCleanupPatterns = rules.SearchTerms.Where(t => !t.BreaksContinuity).Select(t => t.Pattern).ToList();

        var publications = continuityMarkers
            .Where(m => m.Field == PublicationFieldType.Publicacao)
            .OrderBy(m => m.Start)
            .Select(m => new WorkingPublication
            {
                Start = m.Start,
                End = m.End,
                Conteudo = CleanContent(texto[m.Start..m.End], pageCleanupPatterns)
            })
            .ToList();

        AssignHeader(publications, continuityMarkers, PublicationFieldType.Cabecalho1, texto, pageCleanupPatterns);
        AssignHeader(publications, continuityMarkers, PublicationFieldType.Cabecalho2, texto, pageCleanupPatterns);
        AssignHeader(publications, continuityMarkers, PublicationFieldType.Cabecalho3, texto, pageCleanupPatterns);

        AssignNumbers(publications, rules.ProcessNumberPatterns);
        AssignPages(publications, pageMarkers, texto);

        return publications.Select(p => new ExtractedPublication(
            p.Numero,
            new PublicationPayload
            {
                Cabecalho1 = p.Cabecalho1,
                Cabecalho2 = p.Cabecalho2,
                Cabecalho3 = p.Cabecalho3,
                Conteudo = p.Conteudo,
                TodosNumeros = p.TodosNumeros,
                IndexInicio = p.Start,
                IndexFim = p.End,
                Pagina = p.Pagina
            })).ToList();
    }

    private static IEnumerable<PublicationMarker> FindAllOccurrences(string texto, PublicationSearchTerm term)
    {
        if (term.IsRegex)
        {
            foreach (Match match in Regex.Matches(texto, term.Pattern))
            {
                yield return new PublicationMarker { Start = match.Index, End = match.Index + match.Length, Field = term.Field };
            }
        }
        else
        {
            var start = 0;
            while (true)
            {
                var index = texto.IndexOf(term.Pattern, start, StringComparison.Ordinal);
                if (index < 0)
                {
                    yield break;
                }

                yield return new PublicationMarker { Start = index, End = index + term.Pattern.Length, Field = term.Field };
                start = index + 1;
            }
        }
    }

    /// <summary>
    /// Porta de Index.InserirAoFinalDe: insere um marcador logo após cada ocorrência de um
    /// termo do campo de referência (ex: cada cabecalho2 gera um cabecalho3 sintético logo
    /// depois dele). Não porta o ajuste de conflito de posição do legado (deslocar o marcador
    /// quando cai adjacente a outro já existente) — é uma heurística rara e obscura mesmo lá;
    /// se aparecer divergência na validação cruzada, é o primeiro lugar a olhar.
    /// </summary>
    private static IEnumerable<PublicationMarker> FindSyntheticMarkers(string texto, PublicationTypeRules rules)
    {
        var referenceTerms = rules.SearchTerms.Where(t => t.Field == rules.SyntheticReferenceField);
        foreach (var term in referenceTerms)
        {
            foreach (var occurrence in FindAllOccurrences(texto, term))
            {
                yield return new PublicationMarker { Start = occurrence.End, Field = rules.SyntheticField };
            }
        }
    }

    /// <summary>O fim de cada marcador de continuidade é o início do próximo (ou o fim do documento, pro último).</summary>
    private static void AssignEnds(List<PublicationMarker> sortedContinuityMarkers, int textLength)
    {
        for (var i = 0; i < sortedContinuityMarkers.Count; i++)
        {
            sortedContinuityMarkers[i].End = i + 1 < sortedContinuityMarkers.Count
                ? sortedContinuityMarkers[i + 1].Start
                : textLength;
        }
    }

    private static string CleanContent(string recorte, IReadOnlyList<string> pageCleanupPatterns)
    {
        foreach (var pattern in pageCleanupPatterns)
        {
            recorte = Regex.Replace(recorte, pattern, "");
        }

        return recorte;
    }

    private static string CleanHeaderText(string recorte, IReadOnlyList<string> pageCleanupPatterns)
    {
        foreach (var pattern in pageCleanupPatterns)
        {
            // O legado limpa cabeçalhos com o mesmo padrão da página, mas sem a exigência de
            // quebra de linha (o recorte do cabeçalho já não inclui as bordas de linha do jeito
            // que o conteúdo principal inclui).
            recorte = Regex.Replace(recorte, pattern.Replace("[\\r\\n]", ""), "");
        }

        return LimparParagrafos(recorte);
    }

    /// <summary>
    /// Porta fiel de ProcessamentoDados.LimparParagrafos, peculiaridades incluídas: as duas
    /// chamadas de Replace no legado descartam o resultado (string imutável) e viram no-op, e o
    /// trim de espaço final usa Substring(1, ...) em vez de Substring(0, ...) — removendo o
    /// primeiro caractere do recorte em vez do espaço no fim. Mantido assim de propósito pra
    /// bater com o comportamento real de produção na validação cruzada.
    /// </summary>
    private static string LimparParagrafos(string recorte)
    {
        if (recorte.Length > 2 && recorte[..2] == "\r\n")
        {
            recorte = recorte[2..];
        }

        if (recorte.Length > 2 && recorte[..1] == "\n")
        {
            recorte = recorte[1..];
        }

        if (recorte.Length > 2 && recorte[..1] == " ")
        {
            recorte = recorte[1..];
        }

        if (recorte.Length > 2 && recorte[^2..] == "\r\n")
        {
            recorte = recorte[..^2];
        }

        if (recorte.Length > 2 && recorte[^1..] == "\r")
        {
            recorte = recorte[..^1];
        }

        if (recorte.Length > 2 && recorte[^1..] == "\r")
        {
            recorte = recorte[..^1];
        }

        if (recorte.Length > 2 && recorte[^1..] == " ")
        {
            recorte = recorte[1..];
        }

        return recorte;
    }

    /// <summary>Cada publicação herda o último marcador daquele campo visto antes dela no texto (vazio se nenhum).</summary>
    private static void AssignHeader(
        List<WorkingPublication> publications,
        List<PublicationMarker> continuityMarkers,
        PublicationFieldType field,
        string texto,
        IReadOnlyList<string> pageCleanupPatterns)
    {
        var headers = continuityMarkers
            .Where(m => m.Field == field)
            .OrderBy(m => m.Start)
            .Select(m => (m.Start, Text: CleanHeaderText(texto[m.Start..m.End], pageCleanupPatterns)))
            .ToList();

        headers.Insert(0, (int.MinValue, ""));

        foreach (var publication in publications)
        {
            var current = headers.Last(h => h.Start <= publication.Start).Text;
            switch (field)
            {
                case PublicationFieldType.Cabecalho1:
                    publication.Cabecalho1 = current;
                    break;
                case PublicationFieldType.Cabecalho2:
                    publication.Cabecalho2 = current;
                    break;
                case PublicationFieldType.Cabecalho3:
                    publication.Cabecalho3 = current;
                    break;
            }
        }
    }

    /// <summary>
    /// Porta fiel de Leitura.Rpi*.AdicionarNumeroProcesso: Numero fica com o primeiro match do
    /// ÚLTIMO padrão (na ordem da lista) que casou no conteúdo — não do primeiro padrão a casar
    /// — porque cada padrão sobrescreve Numero sem checar se já tinha sido preenchido antes.
    /// TodosNumeros acumula todos os matches de todos os padrões, nessa ordem.
    /// </summary>
    private static void AssignNumbers(List<WorkingPublication> publications, IReadOnlyList<string> processNumberPatterns)
    {
        foreach (var publication in publications)
        {
            string? numero = null;
            var todosNumeros = new List<string>();

            foreach (var pattern in processNumberPatterns)
            {
                var matches = Regex.Matches(publication.Conteudo, pattern);
                if (matches.Count > 0)
                {
                    numero = matches[0].Value;
                }

                foreach (Match match in matches)
                {
                    todosNumeros.Add(match.Value);
                }
            }

            publication.Numero = numero ?? "";
            publication.TodosNumeros = todosNumeros;
        }
    }

    /// <summary>Cada publicação recebe o número da página do marcador de página mais próximo antes dela (0 se nenhum).</summary>
    private static void AssignPages(List<WorkingPublication> publications, List<PublicationMarker> pageMarkers, string texto)
    {
        if (pageMarkers.Count == 0)
        {
            return;
        }

        var pages = pageMarkers
            .OrderBy(m => m.Start)
            .Select(m => (m.Start, Page: ParsePageNumber(texto[m.Start..m.End])))
            .ToList();

        foreach (var publication in publications)
        {
            var candidates = pages.Where(p => p.Start < publication.Start).ToList();
            if (candidates.Count > 0)
            {
                publication.Pagina = candidates[^1].Page;
            }
        }
    }

    /// <summary>
    /// Unifica os formatos de rodapé de página dos 4 tipos: "... NNN\r\n" (Marcas) ou
    /// "... NNN/MMM\r\n" (Patentes/Desenhos/Programas, página corrente/total).
    /// </summary>
    private static int ParsePageNumber(string markerText)
    {
        var afterLastSpace = markerText[(markerText.LastIndexOf(' ') + 1)..].Trim();
        var slashIndex = afterLastSpace.IndexOf('/');
        var pageText = slashIndex >= 0 ? afterLastSpace[..slashIndex] : afterLastSpace;
        return int.Parse(pageText, CultureInfo.InvariantCulture);
    }

    private sealed class WorkingPublication
    {
        public required int Start { get; init; }
        public required int End { get; init; }
        public required string Conteudo { get; set; }
        public string? Cabecalho1 { get; set; }
        public string? Cabecalho2 { get; set; }
        public string? Cabecalho3 { get; set; }
        public string Numero { get; set; } = "";
        public IReadOnlyCollection<string> TodosNumeros { get; set; } = [];
        public int Pagina { get; set; }
    }
}
