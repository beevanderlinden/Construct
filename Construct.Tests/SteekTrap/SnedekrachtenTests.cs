using Construct.Tests.Factories;
using FluentAssertions;
using Xunit;

namespace Construct.Tests.SteekTrap;

/// <summary>
/// Snedekrachten-tests voor SteekTrapEntity.
/// Controleert MEd, VEd en de verhouding VEd/MEd na Bijwerken().
/// </summary>
public class SnedekrachtenTests
{
    /// <summary>
    /// Het domein gebruikt een negatieve tekenconventie voor doorzakkend moment (sagging = negatief).
    /// Na Bijwerken() moet er een niet-nul moment aanwezig zijn.
    /// </summary>
    [Fact]
    public void MEd_IsAanwezigNaBijwerken()
    {
        var (trap, _) = SteekTrapFactory.Create();
        trap.Bijwerken();
        // Sagging moment is negatief in dit framework (negatieve tekenconventie).
        trap.Snedekrachten.My.Should().BeNegative();
    }

    [Fact]
    public void VEd_IsPositiefNaBijwerken()
    {
        var (trap, _) = SteekTrapFactory.Create();
        trap.Bijwerken();
        trap.Snedekrachten.Vz.Should().BePositive();
    }

    /// <summary>
    /// Voor een opgelegde balk met gelijkmatige belasting geldt: VEd = q·L/2 en MEd = q·L²/8.
    /// Hieruit volgt: |VEd| ≈ 4·|MEd|/L  (tolerantie ±15%).
    /// Math.Abs() is nodig omdat het domein sagging-moment negatief definieert.
    /// </summary>
    [Fact]
    public void VEd_VerhoudingTotMEd_KloptMetOpgelegdeBalk()
    {
        var (trap, _) = SteekTrapFactory.Create();
        trap.Bijwerken();
        // VEd ≈ q*L/2, |MEd| ≈ q*L²/8  →  VEd ≈ 4*|MEd|/L
        double verwachtVEd = 4 * Math.Abs(trap.Snedekrachten.My) / (trap.LtProjZ / 1000);
        trap.Snedekrachten.Vz.Should().BeApproximately(verwachtVEd, precision: verwachtVEd * 0.15);
    }

    /// <summary>
    /// Quasi-blijvende belastingcombinatie (BGT) heeft een kleinere absolute waarde dan UGT.
    /// Met negatieve tekenconventie: BGT.My ≥ UGT.My  (minder negatief = kleiner moment).
    /// </summary>
    [Fact]
    public void SnedekrachtenBGT_KleinerInAbsoluteWaardeDanUGT()
    {
        var (trap, _) = SteekTrapFactory.Create();
        trap.Bijwerken();
        // |BGT.My| ≤ |UGT.My|  →  met negatieve conv.: BGT.My ≥ UGT.My
        trap.SnedekrachtenBGT.My.Should().BeGreaterThanOrEqualTo(trap.Snedekrachten.My);
    }
}
