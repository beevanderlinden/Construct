using Construct.Tests.Factories;
using FluentAssertions;
using Xunit;

namespace Construct.Tests.SteekTrap;

/// <summary>
/// Toets-akkoord tests voor SteekTrapEntity.
/// Controleert of Bijwerken() geen uitzondering gooit en of de Eurocode-toetsen akkoord zijn.
/// </summary>
public class ToetsTests
{
    // ── Stap 7: alle 4 scenario's ────────────────────────────────────────────

    [Theory]
    [InlineData(8,  187.5, 220, 100, false)]   // scenario 1: half-verdieping
    [InlineData(16, 185,   220, 120, true)]    // scenario 2: vol-verdieping
    [InlineData(14, 200,   210, 130, true)]    // scenario 3: steile trap
    [InlineData(18, 170,   240, 150, false)]   // scenario 4: flauwe trap
    public void Bijwerken_GooitGeenUitzondering(
        int n, double op, double aan, double d, bool tand)
    {
        var (trap, _) = SteekTrapFactory.Create(n, d, op, aan, tand);
        var act = () => trap.Bijwerken();
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(8,  187.5, 220, 100, false)]
    [InlineData(16, 185,   220, 120, true)]
    [InlineData(14, 200,   210, 130, true)]
    [InlineData(18, 170,   240, 150, false)]
    public void MomentSchil_AsApplied_GroterDanOfGelijkAanAsRequired(
        int n, double op, double aan, double d, bool tand)
    {
        var (trap, _) = SteekTrapFactory.Create(n, d, op, aan, tand);
        trap.Bijwerken();
        trap.MomentSchil.Should().NotBeNull();
        trap.MomentSchil.AsRequired.Should().BePositive();
        trap.MomentSchil.AsApplied.Should().BeGreaterThanOrEqualTo(trap.MomentSchil.AsRequired);
    }

    // ── Individuele toetsen ──────────────────────────────────────────────────

    /// <summary>
    /// Voor een normale geometrie (16 treden, 120mm) is geen dwarskrachtwapening nodig.
    /// VEd &lt; VRd,c (betonweerstand zonder wapening).
    /// </summary>
    [Fact]
    public void Dwarskracht_GeenWapeningNodig_BijNormaleGeometrie()
    {
        var (trap, _) = SteekTrapFactory.Create(16, 120);
        trap.Bijwerken();
        trap.Dwarskracht.Ved.Should().BeLessThan(trap.Dwarskracht.DwarskrachtWeerstandBeton);
    }

    [Fact]
    public void Scheurwijdte_IsGevalideerdNaBijwerken()
    {
        var (trap, _) = SteekTrapFactory.Create(16, 120);
        trap.Bijwerken();
        trap.Scheurwijdte.IsValidated.Should().BeTrue();
    }

    /// <summary>
    /// Na Bijwerken() is de toegepaste wapening in de tand ≥ de benodigde wapening.
    /// </summary>
    [Fact]
    public void Tand_WapeningIsVoldoende()
    {
        var (trap, _) = SteekTrapFactory.Create(16, 120, heeftBoventand: true);
        trap.Bijwerken();
        trap.TandOpleggingBovenzijde.Should().NotBeNull();
        trap.TandOpleggingBovenzijde!.BuigingTand.AsApplied
            .Should().BeGreaterThanOrEqualTo(
                trap.TandOpleggingBovenzijde.BuigingTand.AsRequired);
    }
}
