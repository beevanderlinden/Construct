using Construct.Domain.Entities;
using Construct.Tests.Factories;
using FluentAssertions;
using Xunit;

namespace Construct.Tests.SteekTrap;

/// <summary>
/// Belasting-tests voor SteekTrapEntity.
/// Controleert eigengewicht, doorsnede-oppervlak en belastingopbouw.
/// </summary>
public class BelastingTests
{
    [Fact]
    public void EigenGewicht_LigtBinnenVerwachtBereik()
    {
        var (trap, _) = SteekTrapFactory.Create();
        var gk = trap.GetGk();
        gk.Should().BeInRange(3.0, 8.0); // kN/m²
    }

    /// <summary>
    /// GetDsnOppTrede (analytisch) ≈ GetDsnOppTredePolygoon (schoenveterformule).
    /// </summary>
    [Fact]
    public void DsnOppTrede_KloptMetShoelace()
    {
        var (trap, _) = SteekTrapFactory.Create();
        var opp1 = trap.GetDsnOppTrede();
        var opp2 = trap.GetDsnOppTredePolygoon();
        opp1.Should().BeApproximately(opp2, precision: 0.5);
    }

    [Fact]
    public void GrotereSchilDikte_GeeftHogerEigenGewicht()
    {
        var (trap100, _) = SteekTrapFactory.Create(schildikte: 100);
        var (trap150, _) = SteekTrapFactory.Create(schildikte: 150);
        trap150.GetGk().Should().BeGreaterThan(trap100.GetGk());
    }

    [Fact]
    public void PermanenteBelasting_IsEigenGewichtPlusAfwerking()
    {
        var (trap, _) = SteekTrapFactory.Create();
        trap.AfwerkingVlaklast = 0.5;
        trap.PermanenteBelastingPerM2.Should().BeApproximately(trap.EigenGewichtPerM2 + 0.5, precision: 0.01);
    }
}
