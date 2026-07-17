namespace Kodx.Rpi.Domain.Rpis;

/// <summary>
/// Calcula a edição corrente da RPI a partir de uma edição/data âncora conhecida,
/// assumindo publicação semanal. Não existe fonte oficial de "edição atual" no INPI
/// nem lógica equivalente no sistema legado — isso é construído do zero.
///
/// Risco conhecido: se o INPI pular uma semana (feriado), o cálculo desalinha da
/// realidade até o anchor ser corrigido manualmente na configuração.
/// </summary>
public sealed class RpiEditionCalculator(TimeProvider timeProvider, int anchorEdition, DateOnly anchorPublicationDate)
{
    public int CurrentEdition()
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var daysElapsed = today.DayNumber - anchorPublicationDate.DayNumber;
        var weeksElapsed = daysElapsed / 7;

        return anchorEdition + weeksElapsed;
    }

    public DateOnly PublicationDateFor(int edicao)
    {
        var weeksDiff = edicao - anchorEdition;
        return anchorPublicationDate.AddDays(weeksDiff * 7);
    }
}
