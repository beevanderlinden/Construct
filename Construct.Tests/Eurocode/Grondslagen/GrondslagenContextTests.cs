using Eurocode.Grondslagen;
using FluentAssertions;
using Xunit;

namespace Construct.Tests.Eurocode.Grondslagen;

/// <summary>
/// Unit tests voor <see cref="GrondslagenContext"/> — grondslagen van het constructief ontwerp.
/// Ref: NEN-EN 1990 (EC0), Bijlage B.
/// </summary>
public class GrondslagenContextTests
{
    // ── Gevolgklasse → Betrouwbaarheidsklasse (tabel B.1) ────────────────────

    [Theory]
    [InlineData(GevolgklasseEnum.CC1,  BetrouwbaarheidsklasseEnum.RC1)]
    [InlineData(GevolgklasseEnum.CC1a, BetrouwbaarheidsklasseEnum.RC1)]
    [InlineData(GevolgklasseEnum.CC1b, BetrouwbaarheidsklasseEnum.RC1)]
    [InlineData(GevolgklasseEnum.CC2,  BetrouwbaarheidsklasseEnum.RC2)]
    [InlineData(GevolgklasseEnum.CC2a, BetrouwbaarheidsklasseEnum.RC2)]
    [InlineData(GevolgklasseEnum.CC2b, BetrouwbaarheidsklasseEnum.RC2)]
    [InlineData(GevolgklasseEnum.CC3,  BetrouwbaarheidsklasseEnum.RC3)]
    public void Gevolgklasse_GeeftCorrecteBetrouwbaarheidsklasse(GevolgklasseEnum cc, BetrouwbaarheidsklasseEnum verwacht)
    {
        var ctx = new GrondslagenContext { Gevolgklasse = cc };
        ctx.Betrouwbaarheidsklasse.Should().Be(verwacht);
    }

    // ── Kfi — betrouwbaarheidsdifferentiatiefactor (tabel B.3) ───────────────

    [Theory]
    [InlineData(GevolgklasseEnum.CC1, 0.9)]
    [InlineData(GevolgklasseEnum.CC2, 1.0)]
    [InlineData(GevolgklasseEnum.CC3, 1.1)]
    public void Kfi_CorrectPerGevolgklasse(GevolgklasseEnum cc, double verwacht)
    {
        var ctx = new GrondslagenContext { Gevolgklasse = cc };
        ctx.Kfi.Should().BeApproximately(verwacht, 0.001);
    }

    [Fact]
    public void Kfi_NeeemtToe_BijHogereGevolgklasse()
    {
        var cc1 = new GrondslagenContext { Gevolgklasse = GevolgklasseEnum.CC1 };
        var cc3 = new GrondslagenContext { Gevolgklasse = GevolgklasseEnum.CC3 };
        cc3.Kfi.Should().BeGreaterThan(cc1.Kfi);
    }

    // ── Xi — reductiefactor nationale bijlage (tabel NB.4) ───────────────────

    [Theory]
    [InlineData(NationaleBijlageEnum.NL, 1.2 / 1.35)]
    [InlineData(NationaleBijlageEnum.EU, 1.15 / 1.35)]
    public void Xi_CorrectPerNationaleBijlage(NationaleBijlageEnum nb, double verwacht)
    {
        var ctx = new GrondslagenContext { NationaleBijlage = nb };
        ctx.Xi.Should().BeApproximately(verwacht, 0.001);
    }

    // ── Partiële factoren — fundamentele combinatie (EC0 eq. 6.10a / 6.10b) ──

    [Fact]
    public void FactorFundamenteelA_CC2_NL_CorrectGammaGEnGammaQ()
    {
        // CC2 → Kfi = 1.0; γG = 1.35, γQ = 1.50 (EC0 eq. 6.10a)
        var ctx = new GrondslagenContext
        {
            Gevolgklasse = GevolgklasseEnum.CC2,
            NationaleBijlage = NationaleBijlageEnum.NL
        };
        var (gammaG, gammaQ) = ctx.GetFactorFundamenteelA();
        gammaG.Should().BeApproximately(1.35, 0.001);
        gammaQ.Should().BeApproximately(1.50, 0.001);
    }

    [Fact]
    public void FactorFundamenteelB_CC2_NL_CorrectGammaGgereduceerd()
    {
        // CC2 → Kfi = 1.0; γG = Kfi × ξ × 1.35 = 1.0 × (1.2/1.35) × 1.35 = 1.20 (EC0 eq. 6.10b)
        var ctx = new GrondslagenContext
        {
            Gevolgklasse = GevolgklasseEnum.CC2,
            NationaleBijlage = NationaleBijlageEnum.NL
        };
        var (gammaG, _) = ctx.GetFactorenFundamenteelB(mom1: 1.0);
        gammaG.Should().BeApproximately(1.20, 0.001);
    }

    [Fact]
    public void FactorFundamenteelB_GammaG_KleinerDan_FactorFundamenteelA()
    {
        // EC0 eq. 6.10b geeft een lagere γG dan 6.10a door de ξ-reductie
        var ctx = new GrondslagenContext
        {
            Gevolgklasse = GevolgklasseEnum.CC2,
            NationaleBijlage = NationaleBijlageEnum.NL
        };
        var (gammaGA, _) = ctx.GetFactorFundamenteelA();
        var (gammaGB, _) = ctx.GetFactorenFundamenteelB(mom1: 1.0);
        gammaGB.Should().BeLessThan(gammaGA);
    }

    // ── IsValidated ───────────────────────────────────────────────────────────

    [Fact]
    public void IsValidated_True_MetNationaleBijlage()
    {
        var ctx = new GrondslagenContext { NationaleBijlage = NationaleBijlageEnum.NL };
        ctx.BerekenEnValideer();
        ctx.IsValidated.Should().BeTrue();
    }

    [Fact]
    public void IsValidated_False_ZonderNationaleBijlage()
    {
        var ctx = new GrondslagenContext { NationaleBijlage = null };
        ctx.BerekenEnValideer();
        ctx.IsValidated.Should().BeFalse();
    }
}
