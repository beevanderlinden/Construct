using Eurocode.BetonConstructies;
using Eurocode.Belastingen;
using FluentAssertions;
using Profielen.Beton;
using Xunit;

namespace Construct.Tests.Eurocode.BetonConstructies.H6_UGT;

/// <summary>
/// Unit tests voor <see cref="DwarskrachtWapContext"/> — dwarskrachtweerstand zonder dwarskrachtwapening.
/// Ref: NEN-EN 1992-1-1, §6.2.2.
/// </summary>
public class DwarskrachtWapContextTests
{
    private static DwarskrachtWapContext CreateCtx(double vz = 0, double d = 120)
    {
        var ctx = new DwarskrachtWapContext(
            new BetonContext(BetonsterkteklasseEnum.C30_37),
            new BetonProfiel(1000, 150),
            new SectionForces { Vz = vz });
        ctx.LijstBeugelWap = [];   // 3-param ctor initialiseert dit niet
        ctx.NutHoogte = d;
        return ctx;
    }

    // ── VRd,c ─────────────────────────────────────────────────────────────────

    [Fact]
    public void VRdc_CorrectVoorBekendeInvoer()
    {
        // vmin = 0,035 × k^(3/2) × √fck; k = 2,0 (gecapt); vmin ≈ 0,542 N/mm²
        // VRd,c = vmin × bw × d / 1000 = 0,542 × 1000 × 120 / 1000 ≈ 65 kN
        var ctx = CreateCtx(vz: 0, d: 120);
        ctx.DwarskrachtWeerstandBeton.Should().BeApproximately(65, 5);
    }

    [Fact]
    public void VRdc_NeeemtToeMetGroterD()
    {
        // Grotere nuttige hoogte → hogere VRd,c
        var ctx90  = CreateCtx(d: 90);
        var ctx120 = CreateCtx(d: 120);
        ctx120.DwarskrachtWeerstandBeton.Should().BeGreaterThan(ctx90.DwarskrachtWeerstandBeton);
    }

    // ── Validatie ─────────────────────────────────────────────────────────────

    [Fact]
    public void IsValidated_True_BijVEd_KleinerDan_VRdc()
    {
        // VEd = 10 kN < VRd,c ≈ 65 kN → geen dwarskrachtwapening nodig
        var ctx = CreateCtx(vz: 10);
        ctx.Ved.Should().Be(10);
        ctx.Ved.Should().BeLessThan(ctx.DwarskrachtWeerstandBeton);
        ctx.BerekenEnValideer();
        ctx.IsValidated.Should().BeTrue();
    }

    [Fact]
    public void IsValidated_False_BijVEd_GroterDan_VRdc()
    {
        // VEd = 200 kN >> VRd,c ≈ 65 kN → niet akkoord
        var ctx = CreateCtx(vz: 200);
        ctx.IsValidated.Should().BeFalse();
    }
}
