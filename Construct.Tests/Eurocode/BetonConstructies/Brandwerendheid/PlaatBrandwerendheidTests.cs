using Eurocode.BetonConstructies;
using FluentAssertions;
using Xunit;

namespace Construct.Tests.Eurocode.BetonConstructies.Brandwerendheid;

/// <summary>
/// Unit tests voor <see cref="PlaatBrandwerendheid"/> — tabelopzoekingen brandwerendheid vrijdragende massieve plaat.
/// Ref: NEN-EN 1992-1-2:2004, §5.7, Tabel 5.8.
/// </summary>
public class PlaatBrandwerendheidTests
{
    // ── GetRei — Theory op tabelwaarden Tabel 5.8 ────────────────────────────

    [Theory]
    [InlineData( 60,  20,  30)]   // grenswaarde hs=60mm
    [InlineData( 80,  20,  60)]   // grenswaarde hs=80mm
    [InlineData(100,  30,  90)]
    [InlineData(120,  40, 120)]
    [InlineData(150,  55, 180)]
    [InlineData(175,  65, 240)]
    public void GetRei_ReturnsCorrectValue(double h, double a, int expectedRei)
    {
        PlaatBrandwerendheid.GetRei(h, a).Should().Be(expectedRei);
    }

    [Fact]
    public void GetRei_Returns0_BijTeDunneSchil()
    {
        PlaatBrandwerendheid.GetRei(50, 10).Should().Be(0);
    }

    [Fact]
    public void GetRei_Returns240_BijRuimBovenmaatsteTabelwaarden()
    {
        // Plaatdikte en hartafstand ruimschoots boven de maximale tabelrij (REI 240)
        PlaatBrandwerendheid.GetRei(200, 70).Should().Be(240);
    }

    // ── GetAfstandBenodigdVoorREI ─────────────────────────────────────────────

    [Fact]
    public void GetAfstand_BerekendCorrect_VoorRei60()
    {
        PlaatBrandwerendheid.GetAfstandBenodigdVoorREI(60, 80).Should().Be(20);
    }

    [Fact]
    public void GetAfstand_NaN_BijTeDunneSchil()
    {
        double result = PlaatBrandwerendheid.GetAfstandBenodigdVoorREI(60, 70);
        double.IsNaN(result).Should().BeTrue();
    }

    [Fact]
    public void GetAfstand_NaN_BijOnbekendRei()
    {
        double result = PlaatBrandwerendheid.GetAfstandBenodigdVoorREI(999, 100);
        double.IsNaN(result).Should().BeTrue();
    }

    // ── GetMinimaleDikteVoorREI ───────────────────────────────────────────────

    [Fact]
    public void GetMinimaleDikte_BerekendCorrect_VoorRei60()
    {
        PlaatBrandwerendheid.GetMinimaleDikteVoorREI(60, 20).Should().Be(80);
    }

    [Fact]
    public void GetMinimaleDikte_NaN_BijTeKleineHartafstand()
    {
        double result = PlaatBrandwerendheid.GetMinimaleDikteVoorREI(60, 10);
        double.IsNaN(result).Should().BeTrue();
    }

    [Fact]
    public void GetMinimaleDikte_NaN_BijOnbekendRei()
    {
        double result = PlaatBrandwerendheid.GetMinimaleDikteVoorREI(999, 25);
        double.IsNaN(result).Should().BeTrue();
    }

    // ── Tabel-eigenschap ─────────────────────────────────────────────────────

    [Fact]
    public void Tabel_HeeftZesRijen()
    {
        PlaatBrandwerendheid.Tabel.Should().HaveCount(6);
    }

    [Fact]
    public void Tabel_EersteRij_IsRei30()
    {
        PlaatBrandwerendheid.Tabel.First().Rei.Should().Be(30);
    }

    [Fact]
    public void Tabel_LaatstRij_IsRei240()
    {
        PlaatBrandwerendheid.Tabel.Last().Rei.Should().Be(240);
    }
}
