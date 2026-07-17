using Kodx.Rpi.Domain.Rpis;
using Microsoft.Extensions.Time.Testing;

namespace Kodx.Rpi.Application.Tests.Rpis;

public sealed class RpiEditionCalculatorTests
{
    private static readonly DateOnly AnchorDate = new(2026, 6, 2); // terça-feira
    private const int AnchorEdition = 2891;

    [Theory]
    [InlineData("2026-06-02", 2891)] // a própria semana do anchor
    [InlineData("2026-06-08", 2891)] // ainda dentro da mesma semana (segunda seguinte)
    [InlineData("2026-06-09", 2892)] // completou 1 semana (terça seguinte)
    [InlineData("2026-07-14", 2897)] // 6 semanas depois
    [InlineData("2026-07-16", 2897)] // 6 semanas e 2 dias depois, ainda a mesma edição
    public void CurrentEdition_calcula_a_partir_do_anchor(string today, int expectedEdition)
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse(today + "T12:00:00Z"));
        var calculator = new RpiEditionCalculator(timeProvider, AnchorEdition, AnchorDate);

        Assert.Equal(expectedEdition, calculator.CurrentEdition());
    }

    [Fact]
    public void PublicationDateFor_calcula_data_de_edicoes_passadas_e_futuras()
    {
        var calculator = new RpiEditionCalculator(TimeProvider.System, AnchorEdition, AnchorDate);

        Assert.Equal(AnchorDate, calculator.PublicationDateFor(AnchorEdition));
        Assert.Equal(new DateOnly(2026, 7, 14), calculator.PublicationDateFor(2897));
        Assert.Equal(new DateOnly(2026, 5, 26), calculator.PublicationDateFor(2890));
    }
}
