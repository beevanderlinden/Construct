using Construct.Domain.Entities;
using Construct.Tests.Factories;
using Eurocode.Belastingen;
using Eurocode.BetonConstructies;
using Eurocode.Grondslagen;
using FluentAssertions;
using Xunit;

namespace Construct.Tests.SteekTrap;

/// <summary>
/// Integratietest voor project RD-505003 "12 app Hoflaan" STOLWIJK.
/// Controleert projectinstellingen, trap-geometrie en berekenbaarheid.
/// </summary>
public class ProjectRD505003Tests
{
    [Fact]
    public void Project_HeeftCorrecteProjectInfo()
    {
        var (_, project) = SteekTrapFactory.CreateProject_RD505003();

        project.ProjectInfo.Nummer.Should().Be("RD-505003");
        project.ProjectInfo.Naam.Should().Be("12 app Hoflaan");
        project.ProjectInfo.Plaatsnaam.Should().Be("STOLWIJK");
        project.ProjectInfo.Grondslagen.NationaleBijlage.Should().Be(NationaleBijlageEnum.NL);
        project.ProjectInfo.Grondslagen.Gevolgklasse.Should().Be(GevolgklasseEnum.CC2b);
        project.ProjectInfo.Grondslagen.OntwerpLevensduur.Should().Be(OntwerpLevensduurEnum.Vijftig);
        project.ProjectInfo.MinimaleREI.Should().Be(60);
    }

    [Fact]
    public void Project_HeeftCorrecteFabrieksInstelling()
    {
        var (_, project) = SteekTrapFactory.CreateProject_RD505003();

        project.ProjectInfo.FabrieksInstelling.FabrieksNaam.Should().Be("MBS");
        project.ProjectInfo.FabrieksInstelling.DiamBasis.Should().Be(8);
        project.ProjectInfo.FabrieksInstelling.DiamDetailWapening.Should().Be(6);
        project.ProjectInfo.FabrieksInstelling.HohBovengrens.Should().Be(150);
        project.ProjectInfo.FabrieksInstelling.HohOndergrensBasis.Should().Be(100);
    }

    [Fact]
    public void Project_HeeftCorrecteDefaultGebruiksklasse()
    {
        var (_, project) = SteekTrapFactory.CreateProject_RD505003();

        project.DefaultGebruiksklasse.Should().Be(GebruiksklasseEnum.A_gemeenschappelijke_trappen);
    }

    [Fact]
    public void Project_HeeftCorrecteDefaultDekking()
    {
        var (_, project) = SteekTrapFactory.CreateProject_RD505003();

        project.DefaultDekking.Boven.IsPlaatGeometrie.Should().BeTrue();
        project.DefaultDekking.Boven.IsKwaliteitsBeheersing.Should().BeTrue();
        project.DefaultDekking.Boven.Milieuklassen.Should().Contain(MilieuklasseEnum.XC1);
        project.DefaultDekking.Boven.DekkingToe.Should().BeApproximately(20, 0.1);

        project.DefaultDekking.Onder.IsPlaatGeometrie.Should().BeTrue();
        project.DefaultDekking.Onder.IsKwaliteitsBeheersing.Should().BeTrue();
        project.DefaultDekking.Onder.Milieuklassen.Should().Contain(MilieuklasseEnum.XC1);
        project.DefaultDekking.Onder.DekkingToe.Should().BeApproximately(20, 0.1);
    }

    [Fact]
    public void Project_HeeftPreciesEenAssemblage()
    {
        var (_, project) = SteekTrapFactory.CreateProject_RD505003();

        project.Assemblages.Should().HaveCount(1);
        project.Assemblages[0].Should().BeOfType<SteekTrapEntity>();
    }

    [Fact]
    public void Trap_HeeftCorrecteGeometrie()
    {
        var (trap, _) = SteekTrapFactory.CreateProject_RD505003();

        trap.Merk.Should().Be("TR01/TR02");
        trap.OptredeAantal1.Should().Be(16);
        trap.OptredeMaat.Should().BeApproximately(188, 0.1);
        trap.AantredeMaat.Should().BeApproximately(220, 0.1);
        trap.Breedte.Should().BeApproximately(1200, 0.1);
        trap.SchilDikte.Should().BeApproximately(150, 0.1);
        trap.LengteBoven.Should().BeApproximately(235, 0.1);
        trap.WelMaatVertikaal.Should().BeApproximately(60, 0.1);
        trap.WelOpgaveHoek.Should().BeApproximately(15, 0.1);
    }

    [Fact]
    public void Trap_HeeftCorrecteHoogteTotaal()
    {
        var (trap, _) = SteekTrapFactory.CreateProject_RD505003();

        // HoogteTotaal = n × optrede = 16 × 188 = 3008 mm
        trap.HoogteTotaal.Should().BeApproximately(16 * 188.0, 1.0);
    }

    [Fact]
    public void Trap_HeeftCorrecteGebruiksklasse()
    {
        var (trap, _) = SteekTrapFactory.CreateProject_RD505003();

        trap.Gebruiksklasse.Should().Be(GebruiksklasseEnum.A_gemeenschappelijke_trappen);
    }

    [Fact]
    public void Trap_Bijwerken_GooitGeenUitzondering()
    {
        var (trap, _) = SteekTrapFactory.CreateProject_RD505003();

        var act = () => trap.Bijwerken();
        act.Should().NotThrow();
    }

    /// <summary>
    /// Regressietest: MomentSchil (BendingResults) na Bijwerken() met bekende referentiewaarden.
    /// Momentwapening schil: M=-21.6 kNm | AsReq=404 | AsProv=437 | Ø8-115 | b=1000 | h=150 | d=126 | z=123
    /// </summary>
    [Fact]
    public void Trap_MomentSchil_RegressieWaarden()
    {
        var (trap, _) = SteekTrapFactory.CreateProject_RD505003();
        trap.Bijwerken();

        var m = trap.MomentSchil;
        m.Should().NotBeNull();

        m.Moment.Should().BeApproximately(-21.6, 1.0,    because: "rekenmoment M~Ed~");
        m.AsRequired.Should().BeApproximately(392, 20,   because: "benodigde wapening A~s,req~");
        m.AsApplied.Should().BeApproximately(402, 20,    because: "toegepaste wapening A~s,prov~");
        m.AsProvidedText.Should().Be("Ø8-125",                        because: "wapeningkeuze optimalisatie");
        m.Breedte.Should().BeApproximately(1000, 0.1,    because: "rekenbreedte b = 1000 mm");
        m.Hoogte.Should().BeApproximately(150, 0.1,      because: "profielhoogte h = 150 mm (schildikte)");
        m.D.Should().BeApproximately(126, 2,             because: "nuttige hoogte d = h - z~ref~");
        m.Z.Should().BeApproximately(123, 2,             because: "inwendige hefboomsarm z");

        var doorbuiging = trap.Toetsen.FirstOrDefault(t => t is DoorbuigingValidatieContext) as DoorbuigingValidatieContext;
        doorbuiging.Should().NotBeNull();

        doorbuiging!.LengteMM.Should().BeApproximately(4584, 40, because: "lengte doorbuiging");
        doorbuiging!.Wapening.ToString().Should().Be("Ø8-125");
        doorbuiging!.Kruipkrimp.TheoretischeKruipCoefficient.Should().BeApproximately(2.0, 0.2);
        doorbuiging.Wmax.Should().BeApproximately(-7.88, 1.0);
        doorbuiging.Wbijk.Should().BeApproximately(-5.98, 1.0);



    }

    [Fact]
    public void Trap_MomentSchil_IsNaBijwerkenBeschikbaar()
    {
        var (trap, _) = SteekTrapFactory.CreateProject_RD505003();
        trap.Bijwerken();

        trap.MomentSchil.Should().NotBeNull();
        trap.MomentSchil.AsRequired.Should().BePositive();
        trap.MomentSchil.AsApplied.Should().BeGreaterThanOrEqualTo(trap.MomentSchil.AsRequired);
    }
}
