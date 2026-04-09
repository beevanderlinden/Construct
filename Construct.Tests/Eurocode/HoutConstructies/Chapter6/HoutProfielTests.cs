using CommonLibrary.Models;
using Eurocode.HoutConstructies;
using FluentAssertions;
using Profielen.Parametrisch;
using Xunit;

namespace Construct.Tests.Eurocode.HoutConstructies.Chapter6;

/// <summary>
/// Unit tests voor rechthoekige houtprofielen en sterktechecks EC5 H6.
/// Stap 2: doorsnede-eigenschappen van <see cref="ParametrischProfielContext"/>.
/// Stap 3: buiging- en dwarskrachttoetsen via <see cref="BendingMyToets"/> en <see cref="ShearVzCheck"/>.
/// Ref: NEN-EN 1995-1-1, §6.1.6 (buiging) en §6.1.7 (dwarskracht).
/// </summary>
public class HoutProfielTests
{
    private static ParametrischProfielContext Profiel(double b = 100, double h = 200)
        => new(b, h);

    private static HoutContext HoutC24()
        => new(Houtkwaliteit.C24);

    // ── Stap 2 — Doorsnede-eigenschappen ─────────────────────────────────────

    [Fact]
    public void Oppervlak_IsBreedteKeerHoogte()
    {
        // A = b × h = 100 × 200 = 20 000 mm²
        var p = Profiel(100, 200);
        p.A.Should().BeApproximately(100 * 200, 1.0);
    }

    [Fact]
    public void Iy_CorrectVoorRechthoek()
    {
        // Iy = b × h³ / 12
        var p = Profiel(100, 200);
        double verwacht = 100.0 * Math.Pow(200, 3) / 12.0;
        p.Iy.Should().BeApproximately(verwacht, 1.0);
    }

    [Fact]
    public void Iz_CorrectVoorRechthoek()
    {
        // Iz = h × b³ / 12
        var p = Profiel(100, 200);
        double verwacht = 200.0 * Math.Pow(100, 3) / 12.0;
        p.Iz.Should().BeApproximately(verwacht, 1.0);
    }

    [Fact]
    public void Wy_CorrectVoorRechthoek()
    {
        // Wy = b × h² / 6
        var p = Profiel(100, 200);
        double verwacht = 100.0 * Math.Pow(200, 2) / 6.0;
        p.Wy.Should().BeApproximately(verwacht, 1.0);
    }

    [Fact]
    public void Iy_GroterDanIz_VoorStaandProfiel()
    {
        // Staand profiel (h > b): Iy > Iz
        var p = Profiel(b: 100, h: 200);
        p.Iy.Should().BeGreaterThan(p.Iz);
    }

    [Fact]
    public void GrotereHoogte_GeeftHogereIy()
    {
        // Iy ~ h³: verdubbeling h geeft 8× hogere stijfheid
        var pLaag = Profiel(100, 200);
        var pHoog = Profiel(100, 300);
        pHoog.Iy.Should().BeGreaterThan(pLaag.Iy);
    }

    // ── BendingResistance.MRd — statische formulecontrole ────────────────────

    [Fact]
    public void MRd_CorrectFormule()
    {
        // MRd = W × fmd × 1e-6 (omzetting van Nmm naar kNm)
        double W = 666_667;
        double fmd = 14.769;
        BendingResistance.MRd(W, fmd).Should().BeApproximately(W * fmd * 1e-6, 0.001);
    }

    [Fact]
    public void MRd_NeeemtToeMetGroterW()
    {
        double fmd = 14.769;
        BendingResistance.MRd(1_000_000, fmd).Should().BeGreaterThan(
            BendingResistance.MRd(500_000, fmd));
    }

    // ── Stap 3 — BendingMyToets ───────────────────────────────────────────────

    [Fact]
    public void BendingMyToets_Voldoet_BijKleinMoment()
    {
        // My = 2 kNm << MRd ≈ 9.8 kNm → doorsnede voldoet
        var toets = new BendingMyToets(BelastingsduurKlasse.Middellang);
        var resultaat = toets.Check(
            new InternalForces(My: 2.0),
            Profiel(100, 200),
            HoutC24());
        resultaat.Voldoet.Should().BeTrue();
    }

    [Fact]
    public void BendingMyToets_VoldoetNiet_BijGrootMoment()
    {
        // My = 50 kNm >> MRd ≈ 9.8 kNm → doorsnede voldoet niet
        var toets = new BendingMyToets(BelastingsduurKlasse.Middellang);
        var resultaat = toets.Check(
            new InternalForces(My: 50.0),
            Profiel(100, 200),
            HoutC24());
        resultaat.Voldoet.Should().BeFalse();
    }

    [Fact]
    public void BendingMyToets_GrotereHoogte_GeeftHogereMRd()
    {
        // Hogere doorsnede → groter Wy → hogere MRd
        var toets = new BendingMyToets(BelastingsduurKlasse.Middellang);
        double mRdLaag = toets.Check(new InternalForces(My: 0.01), Profiel(100, 200), HoutC24()).Toelaatbaar;
        double mRdHoog = toets.Check(new InternalForces(My: 0.01), Profiel(100, 300), HoutC24()).Toelaatbaar;
        mRdHoog.Should().BeGreaterThan(mRdLaag);
    }

    [Fact]
    public void BendingMyToets_HogereKwaliteit_GeeftHogereMRd()
    {
        // C30 heeft hogere Fmk dan C24 → hogere Fmd → hogere MRd
        var toets = new BendingMyToets(BelastingsduurKlasse.Middellang);
        double mRdC24 = toets.Check(new InternalForces(My: 0.01), Profiel(), HoutC24()).Toelaatbaar;
        double mRdC30 = toets.Check(new InternalForces(My: 0.01), Profiel(), new HoutContext(Houtkwaliteit.C30)).Toelaatbaar;
        mRdC30.Should().BeGreaterThan(mRdC24);
    }

    // ── Stap 3 — ShearVzCheck ─────────────────────────────────────────────────

    [Fact]
    public void ShearVzCheck_Voldoet_BijKleineKracht()
    {
        // Vz = 10 kN << VRd ≈ 49 kN → doorsnede voldoet
        var toets = new ShearVzCheck(BelastingsduurKlasse.Middellang);
        var resultaat = toets.Check(
            new InternalForces(Vz: 10.0),
            Profiel(100, 200),
            HoutC24());
        resultaat.Voldoet.Should().BeTrue();
    }

    [Fact]
    public void ShearVzCheck_VoldoetNiet_BijGroteKracht()
    {
        // Vz = 200 kN >> VRd ≈ 49 kN → doorsnede voldoet niet
        var toets = new ShearVzCheck(BelastingsduurKlasse.Middellang);
        var resultaat = toets.Check(
            new InternalForces(Vz: 200.0),
            Profiel(100, 200),
            HoutC24());
        resultaat.Voldoet.Should().BeFalse();
    }

    [Fact]
    public void ShearVzCheck_BrederProfiel_GeeftHogereVRd()
    {
        // Breder profiel → groter Aef → hogere VRd
        var toets = new ShearVzCheck(BelastingsduurKlasse.Middellang);
        double vRdSmal = toets.Check(new InternalForces(Vz: 0.01), Profiel(100, 200), HoutC24()).Toelaatbaar;
        double vRdBreed = toets.Check(new InternalForces(Vz: 0.01), Profiel(200, 200), HoutC24()).Toelaatbaar;
        vRdBreed.Should().BeGreaterThan(vRdSmal);
    }
}
