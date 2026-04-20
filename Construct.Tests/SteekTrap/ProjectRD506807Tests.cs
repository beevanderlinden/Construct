using Construct.Tests.Factories;
using Eurocode.Belastingen;
using Eurocode.Grondslagen;
using FluentAssertions;
using Xunit;

namespace Construct.Tests.SteekTrap;

/// <summary>
/// Integratietest voor project RD-506807 "9 app. Rijsweg 60" MALDEN.
/// Controleert projectinstellingen en trap-geometrie (TR01+02) met dragende bomen.
/// </summary>
public class ProjectRD506807Tests
{
    [Fact]
    public void Project_HeeftCorrecteProjectInfo()
    {
        var (_, project) = SteekTrapFactory.CreateProject_RD506807();

        project.ProjectInfo.Nummer.Should().Be("RD-506807");
        project.ProjectInfo.Naam.Should().Be("9 app. Rijsweg 60");
        project.ProjectInfo.Plaatsnaam.Should().Be("MALDEN");
        project.ProjectInfo.Grondslagen.NationaleBijlage.Should().Be(NationaleBijlageEnum.NL);
        project.ProjectInfo.Grondslagen.Gevolgklasse.Should().Be(GevolgklasseEnum.CC2b);
        project.ProjectInfo.Grondslagen.OntwerpLevensduur.Should().Be(OntwerpLevensduurEnum.Vijftig);
    }

    [Fact]
    public void Project_HeeftCorrecteDefaultGebruiksklasse()
    {
        var (_, project) = SteekTrapFactory.CreateProject_RD506807();

        project.DefaultGebruiksklasse.Should().Be(GebruiksklasseEnum.A_gemeenschappelijke_trappen);
    }

    [Fact]
    public void Trap_HeeftCorrecteGeometrie()
    {
        var (trap, _) = SteekTrapFactory.CreateProject_RD506807();

        trap.Merk.Should().Be("TR01+02");
        trap.SchilDikte.Should().BeApproximately(100, 0.1);
        trap.OptredeMaat.Should().BeApproximately(185, 0.1);
        trap.AantredeMaat.Should().BeApproximately(220, 0.1);
        trap.Breedte.Should().BeApproximately(900, 0.1);
        trap.LengteTotaalEigenOpgave.Should().Be(1375);
        trap.GebruikEigenLengte.Should().BeTrue();
    }

    [Fact]
    public void Trap_HeeftDragendeBomen()
    {
        var (trap, _) = SteekTrapFactory.CreateProject_RD506807();

        trap.DragendeTrapBomen.Should().BeTrue();
        trap.TrapBomenHoogte.Should().BeApproximately(300, 0.1);
    }

    [Fact]
    public void Trap_Isberekenbaar()
    {
        var (trap, _) = SteekTrapFactory.CreateProject_RD506807();

        var act = () => trap.Bijwerken();
        act.Should().NotThrow();
    }
}
