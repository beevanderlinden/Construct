using Eurocode.StaalConstructies;
using FluentAssertions;
using Profielen.Staal;
using Xunit;

namespace Construct.Tests.Eurocode.StaalConstructies;

/// <summary>
/// Unit tests voor doorsnedeweerstand EC3 — buiging en dwarskracht.
/// Ref: NEN-EN 1993-1-1, §6.2.5 (buiging) en §6.2.6 (dwarskracht).
/// </summary>
public class WeerstandTests
{
    // ── BendingResistance.MRd — buigingsweerstand (§6.2.5) ───────────────────

    [Fact]
    public void MRd_CorrectFormule()
    {
        // MRd = W × fy / γM0 × 10⁻⁶  →  1.000.000 mm³ × 235 / 1.0 × 10⁻⁶ = 235 kNm
        double mRd = BendingResistance.MRd(W: 1_000_000, fy: 235, gammaM: 1.0);
        mRd.Should().BeApproximately(235.0, 0.01);
    }

    [Fact]
    public void MRd_HogerBijHogereFy()
    {
        // S355 heeft hogere fy → hogere MRd bij gelijk W en γM
        double mRdS235 = BendingResistance.MRd(1_000_000, 235, 1.0);
        double mRdS355 = BendingResistance.MRd(1_000_000, 355, 1.0);
        mRdS355.Should().BeGreaterThan(mRdS235);
    }

    [Fact]
    public void MRd_HogerBijLagerGammaM()
    {
        // Lagere materiaalfactor → hogere berekende weerstand
        double mRd10 = BendingResistance.MRd(1_000_000, 235, 1.0);
        double mRd09 = BendingResistance.MRd(1_000_000, 235, 0.9);
        mRd09.Should().BeGreaterThan(mRd10);
    }

    // ── ShearResistance.VRd — dwarskrachtweerstand (§6.2.6) ──────────────────

    [Fact]
    public void VRd_CorrectFormule()
    {
        // VRd = Av × fy / (√3 × γM0) × 10⁻³  →  1000 mm² × 235 / √3 ≈ 135.7 kN
        double vRd = ShearResistance.VRd(Av: 1000, fy: 235, gammaM: 1.0);
        vRd.Should().BeApproximately(235.0 / Math.Sqrt(3), 0.1);
    }

    [Fact]
    public void VRd_HogerBijGroterAfschuifOppervlak()
    {
        double vRd1 = ShearResistance.VRd(Av: 1000, fy: 235, gammaM: 1.0);
        double vRd2 = ShearResistance.VRd(Av: 2000, fy: 235, gammaM: 1.0);
        vRd2.Should().BeApproximately(2 * vRd1, 0.01);
    }

    // ── ProfielIH — doorsnede-eigenschappen ──────────────────────────────────

    [Fact]
    public void HEB200_WplY_GroterDanWelY()
    {
        // Plastisch weerstandsmoment ≥ elastisch weerstandsmoment (voor elk I-profiel)
        var p = Doorsneden.HEB200;
        p.WplY.Should().BeGreaterThan(p.WelY);
    }

    [Fact]
    public void GroterProfiel_HeeftHogereWplY()
    {
        // HE240B is groter dan HE200B → hogere buigingsweerstand
        var p200 = Doorsneden.HEB200;
        var p240 = Doorsneden.HEB240;
        p240.WplY.Should().BeGreaterThan(p200.WplY);
    }

    [Fact]
    public void HEB200_IsKlasse1_VoorS235()
    {
        // D = H − 2(Tf + R) = 200 − 2×33 = 134 mm  →  c/t = 134/9 = 14.9 ≤ 72ε → klasse 1
        Doorsneden.HEB200.KlasseBuiging.Should().Be(1);
    }

    // ── Gecombineerd: profiel + materiaal ────────────────────────────────────

    [Fact]
    public void HEB200_S235_MRdPlastisch_GroterDanMRdElastisch()
    {
        // Klasse 1/2: gebruik WplY → hogere MRd dan klasse 3 (WelY)
        var p = Doorsneden.HEB200;
        var staal = new StaalContext { StaalKwaliteit = StaalKwaliteitEnum.S235 };

        double mRdKlasse1 = BendingResistance.MRd(p.WplY, staal.Fy, staal.GammaM0);
        double mRdKlasse3 = BendingResistance.MRd(p.WelY, staal.Fy, staal.GammaM0);

        mRdKlasse1.Should().BeGreaterThan(mRdKlasse3);
    }

    [Fact]
    public void HEB200_S355_HeeftHogereMRd_DanS235()
    {
        // Hogere staalsterkte bij zelfde profiel → hogere buigingsweerstand
        var p = Doorsneden.HEB200;
        double mRdS235 = BendingResistance.MRd(p.WplY, 235, 1.0);
        double mRdS355 = BendingResistance.MRd(p.WplY, 355, 1.0);
        mRdS355.Should().BeGreaterThan(mRdS235);
    }

    // ── ProfielEigenschappen HE200-reeks — controle tegen tabelwaarden ────────
    // Bron: ArcelorMittal Sections & Merchant Bars, tabel HEA/HEB/HEM.
    // Tolerantie ≈ 1 % (formule ArcelorMittal is benaderend; tabellen zijn afgerond).

    [Fact]
    public void HEA200_ProfielEigenschappen_MatchenTabelwaarden()
    {
        // Tabelwaarden: A = 5383 mm²  Iy = 3692 cm⁴  Wel,y = 388,6 cm³  Wpl,y = 429,5 cm³
        var p = Doorsneden.HEA200;
        p.H.Should().Be(190);
        p.B.Should().Be(200);
        p.A.Should().BeApproximately(5_383, 5_383 * 0.01);
        p.Iy.Should().BeApproximately(36_920_000, 36_920_000 * 0.01);
        p.WelY.Should().BeApproximately(388_600, 388_600 * 0.01);
        p.WplY.Should().BeApproximately(429_500, 429_500 * 0.01);
    }

    [Fact]
    public void HEB200_ProfielEigenschappen_MatchenTabelwaarden()
    {
        // Tabelwaarden: A = 7808 mm²  Iy = 5696 cm⁴  Wel,y = 569,6 cm³  Wpl,y = 642,5 cm³
        var p = Doorsneden.HEB200;
        p.H.Should().Be(200);
        p.B.Should().Be(200);
        p.A.Should().BeApproximately(7_808, 7_808 * 0.01);
        p.Iy.Should().BeApproximately(56_960_000, 56_960_000 * 0.01);
        p.WelY.Should().BeApproximately(569_600, 569_600 * 0.01);
        p.WplY.Should().BeApproximately(642_500, 642_500 * 0.01);
    }

    [Fact]
    public void HEM200_ProfielEigenschappen_MatchenTabelwaarden()
    {
        // Tabelwaarden: A = 13130 mm²  Iy = 10640 cm⁴  Wel,y = 967,4 cm³  Wpl,y = 1135 cm³
        var p = Doorsneden.HEM200;
        p.H.Should().Be(220);
        p.B.Should().Be(206);
        p.A.Should().BeApproximately(13_130, 13_130 * 0.01);
        p.Iy.Should().BeApproximately(106_400_000, 106_400_000 * 0.01);
        p.WelY.Should().BeApproximately(967_000, 967_000 * 0.01);
        p.WplY.Should().BeApproximately(1_135_000, 1_135_000 * 0.01);
    }

    [Fact]
    public void IPE120_ProfielEigenschappen_MatchenTabelwaarden()
    {
        // Tabelwaarden: A = 1321 mm²  Iy = 317,8 cm⁴  Wel,y = 52,96 cm³  Wpl,y = 60,73 cm³
        var p = Doorsneden.IPE120;
        p.H.Should().Be(120);
        p.B.Should().Be(64);
        p.A.Should().BeApproximately(1_321, 1_321 * 0.01);
        p.Iy.Should().BeApproximately(3_178_000, 3_178_000 * 0.01);
        p.WelY.Should().BeApproximately(52_960, 52_960 * 0.01);
        p.WplY.Should().BeApproximately(60_730, 60_730 * 0.01);
    }

    [Fact]
    public void IPE750x137_ProfielEigenschappen_MatchenTabelwaarden()
    {
        // Tabelwaarden: A = 17090 mm²  Iy = 170200 cm⁴  Wel,y = 4539 cm³  Wpl,y = 5075 cm³
        var p = Doorsneden.IPE750x137;
        p.H.Should().Be(753.0);
        p.B.Should().Be(263.0);
        p.A.Should().BeApproximately(17460, 17460 * 0.01);
        p.Iy.Should().BeApproximately(1598780000, 1598780000 * 0.01);
        p.WelY.Should().BeApproximately(4246000, 4246000 * 0.01);
        p.WplY.Should().BeApproximately(4865000, 4865000 * 0.01);

        p.WelZ.Should().BeApproximately(392800, 392800 * 0.01);
        p.WplZ.Should().BeApproximately(614100, 614100 * 0.01);


    }

    [Fact]
    public void IPE750x147_ProfielEigenschappen_MatchenTabelwaarden()
    {
        // Tabelwaarden: A = 18760 mm²  Iy = 189600 cm⁴  Wel,y = 5039 cm³  Wpl,y = 5631 cm³
        var p = Doorsneden.IPE750x147;
        p.H.Should().Be(753);
        p.B.Should().Be(265);
        p.Tw.Should().Be(13.2);
        p.Tf.Should().Be(17.0);
        p.R.Should().Be(17.0);
        p.A.Should().BeApproximately(18_760, 18_760 * 0.01);
        p.Iy.Should().BeApproximately(1660640000, 1660640000 * 0.01);
        p.WelY.Should().BeApproximately(4411000, 4411000 * 0.01);
        p.WplY.Should().BeApproximately(5110000, 5110000 * 0.01);
    }

    [Fact]
    public void K80x6_ProfielEigenschappen_MatchenTabelwaarden()
    {
        // Tabelwaarden: A = 18760 mm²  Iy = 189600 cm⁴  Wel,y = 5039 cm³  Wpl,y = 5631 cm³
        var p = Doorsneden.SHS80x6;
        p.H.Should().Be(80);
        p.B.Should().Be(80);
        p.R.Should().Be(12);
        p.T.Should().Be(6);
        //p.A.Should().BeApproximately(162, 16.83e2 * 0.01);
        p.Iy.Should().BeApproximately(157e4, 157e4 * 0.01);
        p.Iz.Should().BeApproximately(p.Iy, 1);
        p.WelY.Should().BeApproximately(39.2e3, 39.2e3 * 0.01);
        p.WplY.Should().BeApproximately(53.7e3, 53.7e3 * 0.01);
    }

}
