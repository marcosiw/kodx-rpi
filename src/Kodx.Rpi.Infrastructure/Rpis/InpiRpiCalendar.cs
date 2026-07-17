using System.Globalization;
using System.Text.RegularExpressions;
using Kodx.Rpi.Application.Rpis;
using Microsoft.Extensions.Logging;

namespace Kodx.Rpi.Infrastructure.Rpis;

public sealed partial class InpiRpiCalendar(HttpClient httpClient, ILogger<InpiRpiCalendar> logger) : IRpiCalendar
{
    public async Task<RpiCalendarEntry?> GetMostRecentEditionAsync(CancellationToken cancellationToken)
    {
        var entries = await FetchEntriesAsync(cancellationToken);
        return entries.Count > 0 ? entries[0] : null;
    }

    public async Task<DateOnly?> GetPublicationDateAsync(int edicao, CancellationToken cancellationToken)
    {
        var entries = await FetchEntriesAsync(cancellationToken);
        return entries.FirstOrDefault(e => e.Edicao == edicao)?.DataPublicacao;
    }

    /// <summary>Entradas na ordem em que a página as lista (mais recente primeiro). Lista vazia em caso de falha.</summary>
    private async Task<IReadOnlyList<RpiCalendarEntry>> FetchEntriesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var html = await httpClient.GetStringAsync(string.Empty, cancellationToken);
            var matches = TableRowPattern().Matches(html);

            var entries = new List<RpiCalendarEntry>(matches.Count);
            foreach (Match match in matches)
            {
                var edicao = int.Parse(match.Groups["edicao"].Value, CultureInfo.InvariantCulture);
                var data = DateOnly.ParseExact(match.Groups["data"].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                entries.Add(new RpiCalendarEntry(edicao, data));
            }

            if (entries.Count == 0)
            {
                logger.LogWarning("Calendário do INPI não retornou nenhuma linha reconhecível; o formato da página pode ter mudado.");
            }

            return entries;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao consultar o calendário de edições do INPI; quem chamou deve cair no cálculo por âncora.");
            return [];
        }
    }

    [GeneratedRegex(@"<td>(?<edicao>\d+)</td>\s*<td>\s*(?<data>\d{4}-\d{2}-\d{2})\s*</td>")]
    private static partial Regex TableRowPattern();
}
