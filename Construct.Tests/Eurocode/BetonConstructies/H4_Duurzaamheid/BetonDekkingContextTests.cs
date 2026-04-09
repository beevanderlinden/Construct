using Eurocode.BetonConstructies;
using FluentAssertions;
using System.Xml.Linq;
using Xunit;

namespace Construct.Tests.Eurocode.BetonConstructies.H4_Duurzaamheid;

/// <summary>
/// Unit tests voor <see cref="BetonDekkingContext"/> — dekking en duurzaamheid.
/// Ref: NEN-EN 1992-1-1, §4.4.
/// </summary>
public class BetonDekkingContextTests
{
    // ── cmin,dur — minimale dekking duurzaamheid ──────────────────────────────

    [Fact]
    public void XC1_HeeftCorrecteMinimaleDekking()
    {
        var ctx = new BetonDekkingContext();
        ctx.SelectedMilieuklassen = [MilieuklasseEnum.XC1];

        ctx.DekkingMinDuurzaamheid.Should().Be(15); // EC2 tabel 4.4N, S4/XC1
    }

    [Fact]
    public void XC4_HeeftHogereMinimaleDekking_DanXC1()
    {
        var ctx1 = new BetonDekkingContext();
        ctx1.SelectedMilieuklassen = [MilieuklasseEnum.XC1];

        var ctx4 = new BetonDekkingContext();
        ctx4.SelectedMilieuklassen = [MilieuklasseEnum.XC4];

        ctx4.DekkingMinDuurzaamheid.Should().BeGreaterThan(ctx1.DekkingMinDuurzaamheid);
    }

    // ── cnom — nominale dekking ───────────────────────────────────────────────

    [Fact]
    public void DekkingNom_IsMinPlusDeltaDev()
    {
        var ctx = new BetonDekkingContext();
        
        ctx.SelectedMilieuklassen = [MilieuklasseEnum.XC1];

        // cmin,dur(XC1, S4) = 15; Δcdev(NL) = 5 → cnom = 20
        ctx.DekkingNom.Should().BeApproximately(20, 0.1);
    }

    // ── IsPlaatGeometrie ──────────────────────────────────────────────────────

    [Fact]
    public void PlaatGeometrie_BeeInvloedt_Constructieklasse()
    {
        var ctx = new BetonDekkingContext();
        var klasseVoor = ctx.Constructieklasse.Klasse;

        ctx.IsPlaatGeometrie = !ctx.IsPlaatGeometrie; // switch waarde

        ctx.Constructieklasse.Klasse.Should().NotBe(klasseVoor);
    }

    [Fact]
    public void KwaliteitsBeheersing_BeeInvloedt_Constructieklasse()
    {
        var ctx = new BetonDekkingContext();
        var klasseVoor = ctx.Constructieklasse.Klasse;

        ctx.IsKwaliteitsBeheersing = !ctx.IsKwaliteitsBeheersing; // switch waarde

        ctx.Constructieklasse.Klasse.Should().NotBe(klasseVoor);
    }


    // ── IsValidated ───────────────────────────────────────────────────────────

    [Fact]
    public void IsValidated_True_BijVoldoendeDekking()
    {
        var ctx = new BetonDekkingContext();
        ctx.SelectedMilieuklassen = [MilieuklasseEnum.XC1];
        // DekkingNom = 20, DekkingToe default = 20 → 20 ≤ 20 → akkoord

        ctx.IsValidated.Should().BeTrue();
    }

    [Fact]
    public void IsValidated_False_BijDekkingTeKlein()
    {
        var ctx = new BetonDekkingContext();
        ctx.SelectedMilieuklassen = [MilieuklasseEnum.XC4];
        // DekkingNom = 35, DekkingToe default = 20 → 35 > 20 → niet akkoord

        ctx.IsValidated.Should().BeFalse();
    }
}
