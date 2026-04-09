using Eurocode.BetonConstructies;
using Eurocode.Belastingen;
using FluentAssertions;
using Profielen.Beton;
using Xunit;

namespace Construct.Tests.Eurocode.BetonConstructies.H7_BGT;

/// <summary>
/// Unit tests voor <see cref="DoorbuigingValidatieContext"/> — validatie doorbuiging.
/// Ref: NEN-EN 1992-1-1, §7.4.
/// </summary>
public class DoorbuigingValidatieContextTests
{
    private static DoorbuigingValidatieContext CreateCtx(
        double h = 150, string wap = "10-100", double lMM = 4000,
        double qBlijvend = -3.0, double qQuasiBlijvend = -4.0, double qFrequent = -5.0)
    {
        // 5-param ctor roept BerekenEnValideer() automatisch aan
        return new DoorbuigingValidatieContext(
            new BetonContext(BetonsterkteklasseEnum.C30_37),
            new BetonProfiel(1000, h),
            new WapeningContext(wap, 25),
            lMM,
            [
                new() { CombinatieType = BelastingCombinatieTypeEnum.Blijvend,      Lijnlast = qBlijvend      },
                new() { CombinatieType = BelastingCombinatieTypeEnum.QuasiBlijvend, Lijnlast = qQuasiBlijvend },
                new() { CombinatieType = BelastingCombinatieTypeEnum.Frequent,      Lijnlast = qFrequent      },
            ]);
    }

    // ── Wbijk — bijkomende doorbuiging ────────────────────────────────────────

    [Fact]
    public void Wbijk_NegatiefBijNeerwaardseBelasting()
    {
        var ctx = CreateCtx();
        
        ctx.Wbijk.Should().BeLessThan(0);
    }

    // ── Wmax — maximale doorbuiging ───────────────────────────────────────────



    // ── IsValidated ───────────────────────────────────────────────────────────

    [Fact]
    public void IsValidated_True_BijVoldoendeSchilDikte()
    {
        // h=200, L=3000, lichte belasting → doorbuiging ver binnen grenswaarde
        // GrenswaardeBijkomend = min(0.002 × 3000, 15) = 6 mm
        var ctx = CreateCtx(h: 200, lMM: 3000, qBlijvend: -1.0, qQuasiBlijvend: -1.5, qFrequent: -2.0);
        ctx.IsValidated.Should().BeTrue();
    }

    [Fact]
    public void IsValidated_False_BijTeDunneSchil()
    {
        // h=80, L=6000, zware belasting → doorbuiging overschrijdt grenswaarde
        // GrenswaardeBijkomend = min(0.002 × 6000, 15) = 12 mm
        var ctx = CreateCtx(h: 80, lMM: 6000, qBlijvend: -5.0, qQuasiBlijvend: -7.0, qFrequent: -8.0);
        ctx.IsValidated.Should().BeFalse();
    }

    // ── Relatieve vergelijkingen ──────────────────────────────────────────────

    [Fact]
    public void DikkereSchil_GeeftMindereDeflectie()
    {
        // Hogere schil → grotere stijfheid EI → kleinere doorbuiging
        var ctxDun = CreateCtx(h: 150);
        var ctxDik = CreateCtx(h: 170);
        Math.Abs(ctxDik.Wbijk).Should().BeLessThan(Math.Abs(ctxDun.Wbijk));
    }

    [Fact]
    public void LangereSpanning_GeeftMeerDeflectie()
    {
        // Grotere overspanning → w ~ L⁴ → sterk toenemende doorbuiging
        var ctxKort = CreateCtx(lMM: 4000);
        var ctxLang = CreateCtx(lMM: 5000);
        Math.Abs(ctxLang.Wbijk).Should().BeGreaterThan(Math.Abs(ctxKort.Wbijk));
    }

    // ── UcBijk ────────────────────────────────────────────────────────────────

    [Fact]
    public void UcBijk_KleinerDanEen_WanneerValidated()
    {
        var ctx = CreateCtx(h: 200, lMM: 3000, qBlijvend: -1.0, qQuasiBlijvend: -1.5, qFrequent: -2.0);
        ctx.IsValidated.Should().BeTrue();
        ctx.UcBijk.Should().BeLessThan(1.0);
    }
}
