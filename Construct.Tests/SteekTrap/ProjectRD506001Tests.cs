using Construct.Domain.Entities;
using Construct.Tests.Factories;
using Eurocode.BetonConstructies;
using FluentAssertions;
using Xunit;

namespace Construct.Tests.SteekTrap;

/// <summary>
/// Integratietest voor project RD-506001 trap TR1.
/// Geometrie: LtProjZ=4000 mm, Op=193 mm, Aan=220 mm, schil=150 mm, afwerking=1.5 kN/m².
/// Controleert snedekrachten na Bijwerken() en doorbuigingsvalidatie.
/// </summary>
public class ProjectRD506001Tests
{
    [Fact]
    public void Trap_HeeftCorrecteGeometrie()
    {
        var (trap, _) = SteekTrapFactory.CreateProject_RD506001();

        trap.Merk.Should().Be("TR1");
        trap.OptredeMaat.Should().BeApproximately(193, 0.1);
        trap.AantredeMaat.Should().BeApproximately(220, 0.1);
        trap.SchilDikte.Should().BeApproximately(150, 0.1);
        trap.LtProjZ.Should().BeApproximately(4000, 0.1);
        trap.GebruikEigenLengte.Should().BeTrue();
        trap.AfwerkingVlaklast.Should().BeApproximately(1.5, 0.01);
    }

    [Fact]
    public void Trap_Bijwerken_GooitGeenUitzondering()
    {
        var (trap, _) = SteekTrapFactory.CreateProject_RD506001();

        var act = () => trap.Bijwerken();
        act.Should().NotThrow();
    }

    /// <summary>
    /// Regressietest: snedekrachten na Bijwerken() met bekende referentiewaarden.
    /// MEd ≈ -31.6 kNm, Mfreq ≈ -21.8 kNm, Mqp ≈ -20.6 kNm.
    /// </summary>
    [Fact]
    public void Trap_Doorbuiging_SnedekrachtenReferentieWaarden()
    {
        var (trap, _) = SteekTrapFactory.CreateProject_RD506001();
        trap.Bijwerken();

        trap.Krachten.Should().NotBeNull();

        trap.Krachten.MEd.Should().BeApproximately(-31.6, 1.0,  because: "rekenmoment M~Ed~ (fundamenteel)");
        trap.Krachten.Mfreq.Should().BeApproximately(-21.8, 1.0, because: "rekenmoment M~freq~ (frequent)");
        trap.Krachten.Mqp.Should().BeApproximately(-20.6, 1.0,  because: "rekenmoment M~qp~ (quasi-blijvend)");
    }

    [Fact]
    public void Trap_DoorbuigingValidatie_BeschikbaarNaBijwerken()
    {
        var (trap, _) = SteekTrapFactory.CreateProject_RD506001();
        trap.Bijwerken();

        var doorbuiging = trap.Toetsen.FirstOrDefault(t => t is DoorbuigingValidatieContext) as DoorbuigingValidatieContext;
        doorbuiging.Should().NotBeNull();
        doorbuiging!.LengteMM.Should().BeApproximately(5321, 1.0, because: "doorbuigingslengte = LtProjZ bij GebruikEigenLengte");
    }

    /// <summary>
    /// Variant: Op=180, Aan=240, schil=150 mm. Controleert dat doorbuiging beschikbaar is na Bijwerken().
    /// </summary>
    [Fact]
    public void Trap_Op180_Aan240_Schil150_DoorbuigingValidatie_BeschikbaarNaBijwerken()
    {
        var (trap, _) = SteekTrapFactory.CreateProject_RD506001(optrede: 180, aantrede: 240, schildikte: 150);

        var act = () => trap.Bijwerken();
        act.Should().NotThrow();

        trap.DoorbuigingValidatie.Should().NotBeNull();
        trap.DoorbuigingValidatie.LengteMM.Should().BeGreaterThan(0);
        Math.Abs(trap.DoorbuigingValidatie.Wbijk).Should().BeGreaterThan(0, because: "bijkomende doorbuiging moet berekend zijn");
        Math.Abs(trap.DoorbuigingValidatie.Wmax).Should().BeGreaterThan(0, because: "maximale doorbuiging moet berekend zijn");
    }

    /// <summary>
    /// Variant: Op=180, Aan=240, schil=160 mm. Controleert dat doorbuiging beschikbaar is na Bijwerken().
    /// </summary>
    [Fact]
    public void Trap_Op180_Aan240_Schil160_DoorbuigingValidatie_BeschikbaarNaBijwerken()
    {
        var (trap, _) = SteekTrapFactory.CreateProject_RD506001(optrede: 180, aantrede: 240, schildikte: 160);

        var act = () => trap.Bijwerken();
        act.Should().NotThrow();

        trap.DoorbuigingValidatie.Should().NotBeNull();
        trap.DoorbuigingValidatie.LengteMM.Should().BeGreaterThan(0);
        Math.Abs(trap.DoorbuigingValidatie.Wbijk).Should().BeGreaterThan(0, because: "bijkomende doorbuiging moet berekend zijn");
        Math.Abs(trap.DoorbuigingValidatie.Wmax).Should().BeGreaterThan(0, because: "maximale doorbuiging moet berekend zijn");
    }

    /// <summary>
    /// Vergelijkt schil=150 mm vs schil=160 mm (Op=180, Aan=240).
    /// Een dikkere schil leidt tot minder doorbuiging (Wbijk en Wmax).
    /// </summary>
    [Fact]
    public void Trap_Op180_Aan240_Schil160_HeeftMindereDeflectieDanSchil150()
    {
        var (trap150, _) = SteekTrapFactory.CreateProject_RD506001(optrede: 180, aantrede: 240, schildikte: 150);
        var (trap160, _) = SteekTrapFactory.CreateProject_RD506001(optrede: 180, aantrede: 240, schildikte: 160);

        trap150.Bijwerken();
        trap160.Bijwerken();

        trap150.DoorbuigingValidatie.Should().NotBeNull();
        trap160.DoorbuigingValidatie.Should().NotBeNull();



        Math.Abs(trap160.DoorbuigingValidatie.Wbijk).Should().BeLessOrEqualTo(
            Math.Abs(trap150.DoorbuigingValidatie.Wbijk),
            because: "dikkere schil (160 mm) geeft minder bijkomende doorbuiging dan dunnere schil (150 mm)");

        Math.Abs(trap160.DoorbuigingValidatie.Wmax).Should().BeLessOrEqualTo(
            Math.Abs(trap150.DoorbuigingValidatie.Wmax),
            because: "dikkere schil (160 mm) geeft minder maximale doorbuiging dan dunnere schil (150 mm)");
    }





}
