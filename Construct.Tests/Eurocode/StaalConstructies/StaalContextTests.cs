using Eurocode.StaalConstructies;
using FluentAssertions;
using Mechanica.LiggerSB;
using Profielen.Staal;
using Xunit;

namespace Construct.Tests.Eurocode.StaalConstructies;

/// <summary>
/// Unit tests voor <see cref="StaalContext"/> — staalmateriaal eigenschappen.
/// Ref: NEN-EN 1993-1-1 (EC3), §3.2 en §6.1.
/// </summary>
public class StaalContextTests
{
    // ── Fy — vloeigrens (tabel 3.1) ──────────────────────────────────────────

    [Theory]
    [InlineData(StaalKwaliteitEnum.S235, 235)]
    [InlineData(StaalKwaliteitEnum.S275, 275)]
    [InlineData(StaalKwaliteitEnum.S355, 355)]
    [InlineData(StaalKwaliteitEnum.S450, 450)]
    [InlineData(StaalKwaliteitEnum.S500, 500)]
    public void Fy_CorrectPerKwaliteit(StaalKwaliteitEnum kwaliteit, double verwacht)
    {
        var ctx = new StaalContext { StaalKwaliteit = kwaliteit };
        ctx.Fy.Should().BeApproximately(verwacht, 0.01);
    }

    // ── Fu — treksterkte (tabel 3.1) ─────────────────────────────────────────

    [Theory]
    [InlineData(StaalKwaliteitEnum.S235, 360)]
    [InlineData(StaalKwaliteitEnum.S275, 430)]
    [InlineData(StaalKwaliteitEnum.S355, 510)]
    [InlineData(StaalKwaliteitEnum.S450, 570)]
    [InlineData(StaalKwaliteitEnum.S500, 620)]
    public void Fu_CorrectPerKwaliteit(StaalKwaliteitEnum kwaliteit, double verwacht)
    {
        var ctx = new StaalContext { StaalKwaliteit = kwaliteit };
        ctx.Fu.Should().BeApproximately(verwacht, 0.01);
    }

    [Theory]
    [InlineData(StaalKwaliteitEnum.S235)]
    [InlineData(StaalKwaliteitEnum.S275)]
    [InlineData(StaalKwaliteitEnum.S355)]
    public void Fu_AltijdGroterDanFy(StaalKwaliteitEnum kwaliteit)
    {
        var ctx = new StaalContext { StaalKwaliteit = kwaliteit };
        ctx.Fu.Should().BeGreaterThan(ctx.Fy);
    }

    [Fact]
    public void Fy_NeeemtToe_BijHogereKwaliteit()
    {
        var s235 = new StaalContext { StaalKwaliteit = StaalKwaliteitEnum.S235 };
        var s355 = new StaalContext { StaalKwaliteit = StaalKwaliteitEnum.S355 };
        s355.Fy.Should().BeGreaterThan(s235.Fy);
    }

    // ── Elasticiteitsmodulus E en glijdingsmodulus G (§3.2.6) ────────────────

    [Fact]
    public void E_Is210000()
    {
        // EC3 §3.2.6(1): E = 210 000 N/mm²
        var ctx = new StaalContext();
        ctx.E.Should().BeApproximately(210_000, 1);
    }

    [Fact]
    public void G_AfgeleidVanEEnPoisson()
    {
        // EC3 §3.2.6(1): G = E / [2(1 + ν)] ≈ 80 769 N/mm²
        var ctx = new StaalContext();
        var verwacht = ctx.E / (2 * (1 + ctx.Poisson));
        ctx.G.Should().BeApproximately(verwacht, 1);
    }

    // ── Soortelijk gewicht ────────────────────────────────────────────────────

    [Fact]
    public void SoortelijkGewicht_Is7850()
    {
        // EC1: ρ = 7850 kg/m³
        var ctx = new StaalContext();
        ctx.SoortelijkGewicht.Should().BeApproximately(7850, 1);
    }

    // ── Partiële materiaalfactoren γM (§6.1 en NB) ───────────────────────────

    [Fact]
    public void GammaM0_Default_Is1_0()
    {
        // EC3 §6.1 (NL): γM0 = 1.0
        var ctx = new StaalContext();
        ctx.GammaM0.Should().BeApproximately(1.0, 0.001);
    }

    [Fact]
    public void GammaM1_Default_Is1_0()
    {
        // EC3 §6.1 (NL): γM1 = 1.0
        var ctx = new StaalContext();
        ctx.GammaM1.Should().BeApproximately(1.0, 0.001);
    }

    [Fact]
    public void GammaM2_Default_Is1_25()
    {
        // EC3 §6.1: γM2 = 1.25 (trekbreuk)
        var ctx = new StaalContext();
        ctx.GammaM2.Should().BeApproximately(1.25, 0.001);
    }

    // ── VgmVrijLijnlast — doorbuiging/rotatie HEB200, L=8 m, q=10 kN/m ──────

    
    // ── VgmInklemmingLijnlast — doorbuiging/rotatie HEB200, L=4 m, q=10 kN/m ─

    [Fact]
    public void VgmInklemmingLijnlast_HEB200_PuntenABC_VMThetaW_Correct()
    {
        // Arrange — eenheden: kN en m
        // E: N/mm² → kN/m²  (× 1e3)   |   I: mm⁴ → m⁴  (× 1e-12)
        var staal = new StaalContext();
        var profiel = new ProfielIH(Doorsneden.HEB200);

        const double L = 4.0;          // m
        const double q = 10.0;         // kN/m
        double E = staal.E * 1e3;      // kN/m²
        double I = profiel.Iy * 1e-12; // m⁴
        double EI = E * I;             // kN·m²

        var vmn = new VmnInklemmingLijnlast(q, L, E, I);

        // ── Punt A (x = 0) — inklemming ──────────────────────────────────────
        vmn.PuntA.V.Should().BeApproximately(q * L, 0.001);                                 // RA = 40 kN
        vmn.PuntA.M.Should().BeApproximately(-q * L * L / 2.0, 0.001);                      // inklemmoment = −80 kNm
        vmn.PuntA.Theta.Should().BeApproximately(0, 1e-10);                     // θ = 0 (vaste inklemming)
        vmn.PuntA.W.Should().BeApproximately(0, 1e-10);                         // w = 0 (vaste inklemming)

        // ── Punt B (x = L) — vrij uiteinde ───────────────────────────────────
        vmn.PuntB.V.Should().BeApproximately(0, 1e-10);                         // geen dwarskracht
        vmn.PuntB.M.Should().BeApproximately(0, 1e-10);                         // geen moment
        vmn.PuntB.Theta.Should().BeApproximately(-q * Math.Pow(L, 3) / (6 * EI), 1e-9);  // max rotatie
        vmn.PuntB.W.Should().BeApproximately(-q * Math.Pow(L, 4) / (8 * EI), 1e-9);      // max doorbuiging = −qL⁴/(8EI)

        // ── Punt C (x = L/2) ─────────────────────────────────────────────────
        vmn.PuntC.V.Should().BeApproximately(q * L / 2.0, 0.001);                                   // V(L/2) = qL/2 = 20 kN
        vmn.PuntC.M.Should().BeApproximately(-q * L * L / 8.0, 0.001);                              // M(L/2) = −qL²/8 = −20 kNm
        vmn.PuntC.Theta.Should().BeApproximately(-7 * q * Math.Pow(L, 3) / (48 * EI), 1e-9);     // −7qL³/(48EI)
        vmn.PuntC.W.Should().BeApproximately(-17 * q * Math.Pow(L, 4) / (384 * EI), 1e-9);       // −17qL⁴/(384EI)
    }

    // ── VgmVrijPuntlast — doorbuiging/rotatie HEB200, F=20 kN midspan, L=8 m ─

    [Fact]
    public void VgmVrijPuntlast_HEB200_F20_Midspan_PuntenABC_VMThetaW_Correct()
    {
        // Arrange — eenheden: kN en m
        // E: N/mm² → kN/m²  (× 1e3)   |   I: mm⁴ → m⁴  (× 1e-12)
        var staal = new StaalContext();
        var profiel = new ProfielIH(Doorsneden.HEB200);

        const double L = 8.0;          // m
        const double p = 20.0;         // kN  (puntlast in midspan)
        const double a = L / 2;        // m   (positie puntlast = punt C)
        double E = staal.E * 1e3;      // kN/m²
        double I = profiel.Iy * 1e-12; // m⁴
        double EI = E * I;             // kN·m²

        var vmn = new VmnVrijPuntlast(p, a, L, E, I);

        // ── Punt A (x = 0) ───────────────────────────────────────────────────
        vmn.PuntA.V.Should().BeApproximately(p / 2.0, 0.001);                  // RA = 10 kN
        vmn.PuntA.M.Should().BeApproximately(0, 1e-10);                        // M(0) = 0
        vmn.PuntA.Theta.Should().BeApproximately(-p * L * L / (16 * EI), 1e-9); // −PL²/(16EI)
        vmn.PuntA.W.Should().BeApproximately(0, 1e-10);                        // w(0) = 0

        // ── Punt B (x = L) ───────────────────────────────────────────────────
        vmn.PuntB.V.Should().BeApproximately(-p / 2.0, 0.001);                 // −RB = −10 kN
        vmn.PuntB.M.Should().BeApproximately(0, 1e-10);                        // M(L) = 0
        vmn.PuntB.Theta.Should().BeApproximately(p * L * L / (16 * EI), 1e-9); // +PL²/(16EI)
        vmn.PuntB.W.Should().BeApproximately(0, 1e-10);                        // w(L) = 0

        // ── Punt C (x = L/2 = a) — max buiging en max doorbuiging ────────────
        // x=a valt in de else-tak (x < a is false): waarden gelden voor x=a⁺
        vmn.PuntC.V.Should().BeApproximately(-p / 2.0, 0.001);                // V(a⁺) = RA − P = −10 kN
        vmn.PuntC.M.Should().BeApproximately(p * L / 4.0, 0.001);             // M_max = PL/4 = 40 kNm
        vmn.PuntC.Theta.Should().BeApproximately(0, 1e-10);                    // θ = 0 (symmetrie)
        vmn.PuntC.W.Should().BeApproximately(-p * Math.Pow(L, 3) / (48 * EI), 1e-9); // −PL³/(48EI)
    }
}
