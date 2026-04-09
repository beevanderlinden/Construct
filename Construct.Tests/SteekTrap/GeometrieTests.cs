using Construct.Domain.Entities;
using Construct.Tests.Factories;
using FluentAssertions;
using Xunit;

namespace Construct.Tests.SteekTrap;

/// <summary>
/// Geometrie-tests voor SteekTrapEntity.
/// Controleert berekende afmetingen (LengteTotaal, HoogteTotaal, LengteSchuin, SchilDikte, AdviesSchildikteMin).
/// </summary>
public class GeometrieTests
{
    // ── Stap 3: 4 scenario's als Theory ──────────────────────────────────────

    /// <summary>
    /// HoogteTotaal = n × optrede voor alle 4 standaard-scenario's.
    /// </summary>
    [Theory]
    [InlineData(8,  187.5, 220, 100, false)]   // scenario 1: half-verdieping
    [InlineData(16, 185,   220, 120, true)]    // scenario 2: vol-verdieping
    [InlineData(14, 200,   210, 130, true)]    // scenario 3: steile trap
    [InlineData(18, 170,   240, 150, false)]   // scenario 4: flauwe trap
    public void HoogteTotaal_IsAantalTredenMaalOptrede(
        int n, double op, double aan, double d, bool tand)
    {
        var (trap, _) = SteekTrapFactory.Create(n, d, op, aan, tand);
        trap.HoogteTotaal.Should().BeApproximately(n * op, precision: 1.0);
    }

    /// <summary>
    /// LengteTotaal = n × aantrede voor de twee scenario's zonder tand (1 en 4).
    /// </summary>
    [Theory]
    [InlineData(8,  187.5, 220, 100, 120, 300, true, 8080.0)]   // scenario 1
    [InlineData(18, 170,   240, 150, 140, 400, false, null)]   // scenario 2
    public void ElementLengte_IsCorrect(
        int n, double op, double aan, double d, double lb, double lo, 
        bool gebruikEigenLengte, double? eigenLengte)
    {
        var (trap, _) = SteekTrapFactory.Create(
            aantalTreden: n, 
            schildikte: d, 
            optrede: op, 
            aantrede: aan, 
            heeftBoventand: false);

        trap.LengteOnder = lo;
        trap.LengteBoven = lb;
        trap.GebruikEigenLengte = gebruikEigenLengte;
        trap.LengteTotaalEigenOpgave = eigenLengte;

        


        if (trap.GebruikEigenLengte)
        {
            trap.LengteTotaalEigenOpgave.Should().HaveValue();
            trap.LtProjZ.Should().BeApproximately(trap.LengteTotaalEigenOpgave!.Value, precision: 1.0);
        }
        else
        {
            trap.LtProjZ.Should().BeApproximately((n-1) * aan + trap.LengteOnder!.Value + trap.LengteBoven!.Value, precision: 100.0);

        }

    }

    // ── Stap 4: expliciete tests uit TESTPLAN ─────────────────────────────────

    [Fact]
    public void HalfVerdieping_LengteTotaal_IsCorrect()
    {
        var (trap, _) = SteekTrapFactory.Create(aantalTreden: 8, aantrede: 220, heeftBoventand: false);
        trap.LengteBoven = 220;
        trap.TandOpleggingOnderzijde = null; // geen tand onder
        
        // LengteTotaal =  n × aan (geen tand) + lengteOnder
        trap.LtProjZ.Should().BeApproximately(8 * 220.0 + trap.LengteOnder?? 0, precision: 1.0);
    }

    [Fact]
    public void HalfVerdieping_HoogteTotaal_IsCorrect()
    {
        var (trap, _) = SteekTrapFactory.Create(aantalTreden: 8, optrede: 187.5);
        trap.HoogteTotaal.Should().BeApproximately(8 * 187.5, precision: 1.0);
    }

    [Fact]
    public void HalfVerdieping_LengteSchuin_IsCorrect()
    {
        var (trap, _) = SteekTrapFactory.Create(aantalTreden: 8, optrede: 187.5, aantrede: 220);
        double verwacht = trap.LtProjZ * trap.SchuineMaat / trap.AantredeMaat;
        trap.LengteSchuin.Should().BeApproximately(verwacht, precision: 1.0);
    }

    [Fact]
    public void SchilDikte_BlijftGelijkNaInit()
    {
        var (trap, _) = SteekTrapFactory.Create(schildikte: 130);
        trap.SchilDikte.Should().Be(130);
    }

    // ── AdviesSchildikteMin ──────────────────────────────────────────────────

    [Fact]
    public void AdviesSchildikteMin_IsPositief()
    {
        var (trap, _) = SteekTrapFactory.Create();
        trap.AdviesSchildikteMin.Should().BePositive();
    }

    /// <summary>
    /// Voor een korte trap (8 treden) is de geadviseerde minimale schildikte ≤ de gekozen schildikte.
    /// </summary>
    [Fact]
    public void AdviesSchildikteMin_HalfVerdieping_KleinerDanSchilDikte()
    {
        var (trap, _) = SteekTrapFactory.Create(
            aantalTreden: 8, schildikte: 100, optrede: 187.5, heeftBoventand: false);
        trap.AdviesSchildikteMin.Should().BeLessOrEqualTo(trap.SchilDikte);
    }
}
