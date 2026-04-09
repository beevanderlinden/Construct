using Eurocode.BetonConstructies;
using Eurocode.Belastingen;
using Eurocode.Grondslagen;
using FluentAssertions;
using Profielen.Beton;
using Xunit;

namespace Construct.Tests.Eurocode.BetonConstructies.H7_BGT;

/// <summary>
/// Unit tests voor <see cref="ScheurwijdteContext"/> — karakteristieke scheurwijdte.
/// Ref: NEN-EN 1992-1-1, §7.3.4.
/// </summary>
public class ScheurwijdteContextTests
{
    private static ScheurwijdteContext CreateCtx(
        double my = 8,
        string wap = "8-150",
        MilieuklasseEnum mk = MilieuklasseEnum.XC1)
    {
        var beton = new BetonContext(BetonsterkteklasseEnum.C30_37);
        var dekking = new BetonDekkingContext();
        dekking.SelectedMilieuklassen = [mk];

        var ctx = new ScheurwijdteContext(
            new SectionForces { My = my },
            beton,
            dekking,
            new BetonProfiel(1000, 150),
            new WapeningContext(wap, 25),
            NationaleBijlageEnum.NL);

        ctx.BerekenEnValideer();
        return ctx;
    }

    // ── Scheurwijdte wk ───────────────────────────────────────────────────────

    [Fact]
    public void Wk_PositiefBijPositiefMoment()
    {
        // Positief BGT-moment → trekspanning onderzijde → wk > 0
        var ctx = CreateCtx(my: 8);
        ctx.Wk.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Wk_NulBijGeenMoment()
    {
        // MEd = 0 → σs = 0 → εsm–εcm = 0 → wk = 0
        var ctx = CreateCtx(my: 0);
        ctx.Wk.Should().BeApproximately(0, 0.001);
    }

    // ── Grenswaarde wmax (statische tabelopzoekfunctie) ───────────────────────

    [Theory]
    [InlineData(MilieuklasseEnum.XC1, 0.40)]   // NL bijlage, standaard elementen
    [InlineData(MilieuklasseEnum.XC4, 0.30)]   // buitenomgeving
    public void Wmax_CorrecteWaardePerMilieuklasse(MilieuklasseEnum mk, double expected)
    {
        Scheurbeheersing.ScheurwijdteGrenswaarde
            .GetScheurwijdteMax(mk, Scheurbeheersing.ElementType.Standaard, NationaleBijlageEnum.NL)
            .Should().Be(expected);
    }

    // ── Validatie ─────────────────────────────────────────────────────────────

    [Fact]
    public void IsValidated_True_BijWk_KleinerDanWmax()
    {
        // My=8 kNm → wk ≈ 0.12 mm < wmax=0.40 mm (XC1/NL) → akkoord
        var ctx = CreateCtx(my: 8);
        ctx.IsValidated.Should().BeTrue();
    }

    [Fact]
    public void IsValidated_False_BijWk_GroterDanWmax()
    {
        // My=22 geeft wk≈0.22 mm; lineaire schaal → My=60 geeft wk≈0.60 mm > wmax=0.40 mm (XC1/NL)
        var ctx = CreateCtx(my: 60);
        ctx.IsValidated.Should().BeFalse();
    }

    // ── Nauwdere wapening ─────────────────────────────────────────────────────

    [Fact]
    public void NauwdereWapening_VerlaagdWk()
    {
        // Kleiner hoh (8-100 vs 8-150): meer As → lagere σs én kleinere SrMax → kleinere wk
        var ctx150 = CreateCtx(my: 8, wap: "8-150");
        var ctx100 = CreateCtx(my: 8, wap: "8-100");
        ctx100.Wk.Should().BeLessThan(ctx150.Wk);
    }
}
