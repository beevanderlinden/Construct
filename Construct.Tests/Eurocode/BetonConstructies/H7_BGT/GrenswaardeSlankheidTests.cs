using Eurocode.BetonConstructies;
using Eurocode.Belastingen;
using FluentAssertions;
using Profielen.Beton;
using Xunit;

namespace Construct.Tests.Eurocode.BetonConstructies.H7_BGT;

/// <summary>
/// Unit tests voor <see cref="GrenswaardeSlankheidContext"/> — grenswaarde slankheid voor doorbuiging.
/// Ref: NEN-EN 1992-1-1, §7.4.2.
/// </summary>
public class GrenswaardeSlankheidTests
{
    private static GrenswaardeSlankheidContext CreateCtx(double l = 2000, double my = 20, string wap = "8-150")
    {
        var br = new BendingResults(
            new BetonContext(BetonsterkteklasseEnum.C30_37),
            new BetonProfiel(1000, 150),
            new WapeningContext(wap, 25),
            new SectionForces { My = my });

        return new GrenswaardeSlankheidContext
        {
            BendingResults = br,
            LengteOverspanning = l
        };
    }

    // ── Slankheid λ = l / d ───────────────────────────────────────────────────

    [Fact]
    public void Slankheid_LambdaCorrectBerekend()
    {
        // λ = L / d = 2000 / 121 ≈ 16.5  (d = h - c - ø/2 = 150 - 25 - 4 = 121)
        var ctx = CreateCtx(l: 2000);
        ctx.Slankheid.Should().BeApproximately(16.5, 0.5);
    }

    // ── Grenswaarde slankheid λ_lim ───────────────────────────────────────────

    [Fact]
    public void GrenswaardeNeeemtAf_BijHogereWapeningsverhouding()
    {
        // EC2 §7.4.2(2) formule 7.16a: hogere ρ (zwaarder belast) → lagere λ_lim
        // My=5  → Rho ≈ 0.0020 → λ_lim ≈ 72
        // My=20 → Rho ≈ 0.0033 → λ_lim ≈ 34
        var ctxLicht = CreateCtx(my: 5);
        var ctxZwaar = CreateCtx(my: 20);
        ctxZwaar.GrenswaardeSlankheid.Should().BeLessThan(ctxLicht.GrenswaardeSlankheid);
    }

    // ── Validatie ─────────────────────────────────────────────────────────────

    [Fact]
    public void IsValidated_True_BijLambda_KleinerDanGrens()
    {
        // L=2000: λ ≈ 16.5 << λ_lim ≈ 34 → doorbuigingsberekening kan achterwege blijven
        var ctx = CreateCtx(l: 2000, my: 20);
        ctx.IsValidated.Should().BeTrue();
    }

    [Fact]
    public void IsValidated_False_BijLambda_GroterDanGrens()
    {
        // L=6000: λ ≈ 49.6 > λ_lim ≈ 34 → doorbuigingsberekening noodzakelijk
        var ctx = CreateCtx(l: 6000, my: 20);
        ctx.IsValidated.Should().BeFalse();
    }
}
