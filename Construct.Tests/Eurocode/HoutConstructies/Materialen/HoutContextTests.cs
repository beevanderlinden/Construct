using CommonLibrary.Models;
using Eurocode.HoutConstructies;
using FluentAssertions;
using Xunit;

namespace Construct.Tests.Eurocode.HoutConstructies.Materialen;

/// <summary>
/// Unit tests voor <see cref="HoutContext"/> — karakteristieke materiaaleigenschappen hout.
/// Ref: NEN-EN 1995-1-1, tabel 1 (C klassen), tabel 2 (D klassen), tabel 3 (GL klassen).
/// </summary>
public class HoutContextTests
{
    // ── Standaard kwaliteit en materiaaltype ──────────────────────────────────

    [Fact]
    public void DefaultKwaliteit_IsC24()
    {
        var hout = new HoutContext();
        hout.Kwaliteit.Should().Be(Houtkwaliteit.C24);
    }

    [Fact]
    public void MateriaalType_IsHout()
    {
        var hout = new HoutContext();
        hout.Type.Should().Be(MateriaalType.Hout);
    }

    // ── C24 — naaldhout standaard ─────────────────────────────────────────────

    [Fact]
    public void C24_HeeftCorrecteFmk()
    {
        var hout = new HoutContext(Houtkwaliteit.C24);
        hout.Fmk.Should().Be(24);
    }

    [Fact]
    public void C24_HeeftCorrecteFvk()
    {
        var hout = new HoutContext(Houtkwaliteit.C24);
        hout.Fvk.Should().BeApproximately(4.0, 0.01);
    }

    [Fact]
    public void C24_HeeftCorrecteE()
    {
        var hout = new HoutContext(Houtkwaliteit.C24);
        hout.E.Should().Be(11000);
    }

    [Fact]
    public void C24_HeeftCorrecteRhoK()
    {
        // ρk = 350 kg/m³ (EC5 tabel 1)
        var hout = new HoutContext(Houtkwaliteit.C24);
        hout.SoortelijkGewicht.Should().Be(350);
    }

    [Fact]
    public void C24_HeeftGammaM_130()
    {
        // EC5 tabel 2.3: γM = 1.30 voor gezaagd naaldhout
        var hout = new HoutContext(Houtkwaliteit.C24);
        hout.GammaM.Should().BeApproximately(1.3, 0.01);
    }

    [Fact]
    public void C24_HeeftFabricageGezaagd()
    {
        var hout = new HoutContext(Houtkwaliteit.C24);
        hout.Fabricage.Should().Be(FabricageType.Gezaagd);
    }

    // ── C18 — lagere C klasse ─────────────────────────────────────────────────

    [Fact]
    public void C18_HeeftCorrecteFmk()
    {
        var hout = new HoutContext(Houtkwaliteit.C18);
        hout.Fmk.Should().Be(18);
    }

    [Fact]
    public void C18_HeeftCorrecteFvk()
    {
        // C18: fv,k = 3.2 N/mm² (EC5 tabel 1)
        var hout = new HoutContext(Houtkwaliteit.C18);
        hout.Fvk.Should().BeApproximately(3.2, 0.01);
    }

    [Fact]
    public void C18_HeeftLagereEModulus_DanC24()
    {
        // C18: E = 9000 N/mm² < C24: E = 11000 N/mm²
        var c18 = new HoutContext(Houtkwaliteit.C18);
        var c24 = new HoutContext(Houtkwaliteit.C24);
        c18.E.Should().BeLessThan(c24.E);
    }

    // ── C klassen — vergelijkende tests ──────────────────────────────────────

    [Theory]
    [InlineData(Houtkwaliteit.C18, 18)]
    [InlineData(Houtkwaliteit.C24, 24)]
    [InlineData(Houtkwaliteit.C30, 30)]
    [InlineData(Houtkwaliteit.C40, 40)]
    public void CKlasse_FmkGelijkAanKlasseGetal(Houtkwaliteit kwaliteit, double verwachteFmk)
    {
        var hout = new HoutContext(kwaliteit);
        hout.Fmk.Should().Be(verwachteFmk);
    }

    [Fact]
    public void HogereKlasse_HeeftHogereFmk()
    {
        // C30 > C18 in buigsterkte
        var c18 = new HoutContext(Houtkwaliteit.C18);
        var c30 = new HoutContext(Houtkwaliteit.C30);
        c30.Fmk.Should().BeGreaterThan(c18.Fmk);
    }

    // ── D klassen — loofhout ──────────────────────────────────────────────────

    [Fact]
    public void D24_HeeftHogerSoortelijkGewicht_DanC24()
    {
        // Loofhout (D) is zwaarder dan naaldhout (C) bij vergelijkbare sterkteklasse
        // D24: ρk = 485 kg/m³ vs C24: ρk = 350 kg/m³
        var d24 = new HoutContext(Houtkwaliteit.D24);
        var c24 = new HoutContext(Houtkwaliteit.C24);
        d24.SoortelijkGewicht.Should().BeGreaterThan(c24.SoortelijkGewicht);
    }

    // ── GL klassen — gelamineerd hout ─────────────────────────────────────────

    [Fact]
    public void GL24h_HeeftCorrecteFmk()
    {
        var gl = new HoutContext(Houtkwaliteit.GL24h);
        gl.Fmk.Should().Be(24);
    }

    [Fact]
    public void GL24h_HeeftLagerePartieleFactor_DanGezaagd()
    {
        // EC5 tabel 2.3: gelamineerd γM = 1.25 < gezaagd γM = 1.30
        var gl = new HoutContext(Houtkwaliteit.GL24h);
        var c24 = new HoutContext(Houtkwaliteit.C24);
        gl.PartieleFactor.Should().BeLessThan(c24.PartieleFactor);
    }

    [Fact]
    public void GL24h_HeeftGammaM_125()
    {
        // EC5 tabel 2.3: γM = 1.25 voor gelamineerd hout
        var gl = new HoutContext(Houtkwaliteit.GL24h);
        gl.PartieleFactor.Should().BeApproximately(1.25, 0.01);
    }

    [Fact]
    public void GL24h_HeeftFabricageGelamineeerd()
    {
        var gl = new HoutContext(Houtkwaliteit.GL24h);
        gl.Fabricage.Should().Be(FabricageType.Gelamineeerd);
    }

    // ── GetKmod — modificatiefactor voor belastingsduur ──────────────────────

    [Fact]
    public void GetKmod_Gezaagd_Klasse2_Middellang_Is080()
    {
        // EC5 Tabel 3.1: kmod = 0.80 (Gezaagd, Klasse 2, Middellang)
        var hout = new HoutContext(Houtkwaliteit.C24);
        hout.GetKmod(BelastingsduurKlasse.Middellang).Should().BeApproximately(0.80, 0.01);
    }

    [Fact]
    public void GetKmod_Gezaagd_Klasse2_Blijvend_Is060()
    {
        // EC5 Tabel 3.1: kmod = 0.60 (Gezaagd, Klasse 2, Blijvend)
        var hout = new HoutContext(Houtkwaliteit.C24);
        hout.GetKmod(BelastingsduurKlasse.Blijvend).Should().BeApproximately(0.60, 0.01);
    }

    [Fact]
    public void GetKmod_NeeemtToeMetKortereBelasting()
    {
        // Kmod is hoger bij kortere belastingsduur (gunstiger houtgedrag)
        var hout = new HoutContext(Houtkwaliteit.C24);
        hout.GetKmod(BelastingsduurKlasse.Blijvend).Should().BeLessThan(
            hout.GetKmod(BelastingsduurKlasse.Kort));
    }

    // ── GetFmd — rekenwaarde buigsterkte ─────────────────────────────────────

    [Fact]
    public void GetFmd_C24_Middellang_Klasse2_CorrectFormule()
    {
        // fmd = kmod × fmk / γM = 0.80 × 24 / 1.30 ≈ 14.77 N/mm²
        var hout = new HoutContext(Houtkwaliteit.C24);
        double verwacht = 0.80 * 24.0 / 1.30;
        hout.GetFmd(BelastingsduurKlasse.Middellang).Should().BeApproximately(verwacht, 0.01);
    }

    [Fact]
    public void GetFmd_NeeemtToeMetHogereKwaliteit()
    {
        // Hogere kwaliteit → hogere Fmk → hogere Fmd
        var c24 = new HoutContext(Houtkwaliteit.C24);
        var c30 = new HoutContext(Houtkwaliteit.C30);
        c30.GetFmd(BelastingsduurKlasse.Middellang).Should().BeGreaterThan(
            c24.GetFmd(BelastingsduurKlasse.Middellang));
    }

    // ── GetFvd — rekenwaarde dwarskrachtsterkte ───────────────────────────────

    [Fact]
    public void GetFvd_C24_Middellang_Klasse2_CorrectFormule()
    {
        // fvd = kmod × fvk / γM = 0.80 × 4.0 / 1.30 ≈ 2.46 N/mm²
        var hout = new HoutContext(Houtkwaliteit.C24);
        double verwacht = 0.80 * 4.0 / 1.30;
        hout.GetFvd(BelastingsduurKlasse.Middellang).Should().BeApproximately(verwacht, 0.01);
    }
}
