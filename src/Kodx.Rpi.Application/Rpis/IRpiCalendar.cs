namespace Kodx.Rpi.Application.Rpis;

/// <summary>
/// Consulta o calendário oficial de edições da RPI publicado pelo INPI
/// (https://revistas.inpi.gov.br/rpi/), que já reflete o deslocamento real da data de
/// publicação quando cai em feriado — evita termos que calcular isso por conta própria.
/// Retorna null em caso de falha (rede, mudança de formato da página); quem consome deve
/// cair de volta no cálculo por âncora semanal (<see cref="RpiEditionCalculator"/>) nesse caso.
/// </summary>
public interface IRpiCalendar
{
    Task<RpiCalendarEntry?> GetMostRecentEditionAsync(CancellationToken cancellationToken);

    Task<DateOnly?> GetPublicationDateAsync(int edicao, CancellationToken cancellationToken);
}
