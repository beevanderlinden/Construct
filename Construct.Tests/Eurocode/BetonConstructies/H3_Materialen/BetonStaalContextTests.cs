using Eurocode.BetonConstructies;
using FluentAssertions;
using Xunit;

namespace Construct.Tests.Eurocode.BetonConstructies.H3_Materialen;

/// <summary>
/// Unit tests voor <see cref="BetonStaalContext"/> — materiaalparameters betonwapening.
/// Ref: NEN-EN 1992-1-1, §3.2.
/// </summary>
public class BetonStaalContextTests
{
    // ── Fyk — karakteristieke vloeisterkte ───────────────────────────────────

    [Fact]
    public void B500B_HeeftCorrecteFyk()
    {
        var staal = new BetonStaalContext(BetonStaalKwaliteitEnum.B500B);
        staal.Fyk.Should().Be(500);
    }

    [Fact]
    public void B400B_HeeftCorrecteFyk()
    {
        var staal = new BetonStaalContext(BetonStaalKwaliteitEnum.B400B);
        staal.Fyk.Should().Be(400);
    }

    [Fact]
    public void B600B_HeeftCorrecteFyk()
    {
        var staal = new BetonStaalContext(BetonStaalKwaliteitEnum.B600B);
        staal.Fyk.Should().Be(600);
    }

    // ── Fyd — rekenwaarde vloeisterkte ───────────────────────────────────────

    [Fact]
    public void B500B_HeeftCorrecteFyd()
    {
        // fyd = fyk / γ_s = 500 / 1.15 ≈ 434.78 N/mm²  (EC2 §3.2.7(2))
        var staal = new BetonStaalContext(BetonStaalKwaliteitEnum.B500B);
        staal.Fyd.Should().BeApproximately(434.78, 0.5);
    }

    [Fact]
    public void Fyd_NeeemtToeMetHogereKwaliteit()
    {
        var staalB400 = new BetonStaalContext(BetonStaalKwaliteitEnum.B400B);
        var staalB600 = new BetonStaalContext(BetonStaalKwaliteitEnum.B600B);
        staalB600.Fyd.Should().BeGreaterThan(staalB400.Fyd);
    }

    // ── Es — elasticiteitsmodulus ─────────────────────────────────────────────

    [Fact]
    public void B500B_HeeftCorrecteEs()
    {
        // Es = 200 000 N/mm²  (EC2 §3.2.7(4))
        var staal = new BetonStaalContext(BetonStaalKwaliteitEnum.B500B);
        staal.ElasticiteitsModulus.Should().Be(200000);
    }

    [Fact]
    public void Es_IsOnafhankelijkVanKwaliteit()
    {
        // De elasticiteitsmodulus is een constante, onafhankelijk van de staalklasse
        var staalB400 = new BetonStaalContext(BetonStaalKwaliteitEnum.B400B);
        var staalB600 = new BetonStaalContext(BetonStaalKwaliteitEnum.B600B);
        staalB400.ElasticiteitsModulus.Should().Be(staalB600.ElasticiteitsModulus);
    }

    // ── GammaS — partiële factor ──────────────────────────────────────────────

    [Fact]
    public void GammaS_IsStandaard115()
    {
        var staal = new BetonStaalContext(BetonStaalKwaliteitEnum.B500B);
        staal.GammaS.Should().BeApproximately(1.15, 0.01);
    }
}
