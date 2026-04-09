using Eurocode.BetonConstructies;
using FluentAssertions;
using Xunit;

namespace Construct.Tests.Eurocode.BetonConstructies.H3_Materialen;

/// <summary>
/// Unit tests voor <see cref="BetonContext"/> — karakteristieke en rekenwaarden beton.
/// Ref: NEN-EN 1992-1-1, §3.1.
/// </summary>
public class BetonContextTests
{
    // ── Fck — karakteristieke cilinderdruksterkte ─────────────────────────────

    [Fact]
    public void C2025_HeeftCorrecteFck()
    {
        var beton = new BetonContext(BetonsterkteklasseEnum.C20_25);
        beton.Fck.Should().Be(20);
    }

    [Fact]
    public void C4555_HeeftCorrecteFck()
    {
        var beton = new BetonContext(BetonsterkteklasseEnum.C45_55);
        beton.Fck.Should().Be(45);
    }

    // ── Fcm — gemiddelde cilinderdruksterkte ──────────────────────────────────

    [Fact]
    public void C3037_HeeftCorrecteFcm()
    {
        // fcm = fck + 8 = 30 + 8 = 38 N/mm²  (EC2 tabel 3.1)
        var beton = new BetonContext(BetonsterkteklasseEnum.C30_37);
        beton.Fcm.Should().Be(38);
    }

    // ── Fctm — gemiddelde axiale treksterkte ──────────────────────────────────

    [Fact]
    public void C3037_HeeftCorrecteFctm()
    {
        // fctm = 0.30 × fck^(2/3) = 0.30 × 30^(2/3) ≈ 2.9 N/mm²  (EC2 tabel 3.1)
        var beton = new BetonContext(BetonsterkteklasseEnum.C30_37);
        beton.Fctm.Should().BeApproximately(2.9, 0.1);
    }

    // ── Ecm — elasticiteitsmodulus ────────────────────────────────────────────

    [Fact]
    public void C3037_HeeftCorrecteEcm()
    {
        // Ecm = 22 × (fcm/10)^0.3 × 1000 ≈ 32 837 N/mm²  (EC2 §3.1.3(2))
        var beton = new BetonContext(BetonsterkteklasseEnum.C30_37);
        beton.GetEcm().Should().BeApproximately(32837, 50);
    }

    [Fact]
    public void Ecm_NeeemtToeMetHogereBetonsterkteKlasse()
    {
        var betonC20 = new BetonContext(BetonsterkteklasseEnum.C20_25);
        var betonC45 = new BetonContext(BetonsterkteklasseEnum.C45_55);
        betonC45.GetEcm().Should().BeGreaterThan(betonC20.GetEcm());
    }

    // ── Partiële factor en alpha ──────────────────────────────────────────────

    [Fact]
    public void PartieleFactor_IsStandaard15()
    {
        var beton = new BetonContext(BetonsterkteklasseEnum.C30_37);
        beton.PartieleFactor.Should().BeApproximately(1.5, 0.01);
    }

    [Fact]
    public void AlphaCC_IsStandaard1()
    {
        // Implementatie gebruikt AlphaCC = 1.0 (EC2 §3.1.6(1); de NL bijlage 0.85 is hier niet toegepast)
        BetonContext.AlphaCC.Should().BeApproximately(1.0, 0.01);
    }

    // ── Fcd — rekenwaarde druksterkte ────────────────────────────────────────

    [Fact]
    public void Fcd_BerekendCorrect()
    {
        // fcd = AlphaCC × fck / γ_c = 1.0 × 30 / 1.5 = 20.0 N/mm²  (EC2 §3.1.6(1))
        var beton = new BetonContext(BetonsterkteklasseEnum.C30_37);
        beton.Fcd.Should().BeApproximately(20.0, 0.01);
    }

    [Fact]
    public void Fcd_NeeemtToeMetHogereBetonsterkteKlasse()
    {
        var betonC20 = new BetonContext(BetonsterkteklasseEnum.C20_25);
        var betonC45 = new BetonContext(BetonsterkteklasseEnum.C45_55);
        betonC45.Fcd.Should().BeGreaterThan(betonC20.Fcd);
    }
}
