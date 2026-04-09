using Construct.Domain.Entities;
using Construct.Tests.Factories;
using Eurocode.Belastingen;
using Eurocode.Grondslagen;
using FluentAssertions;
using Xunit;

namespace Construct.Tests.SteekTrap;

/// <summary>
/// Integratietest voor project RD-505007 "Mountain Network" Nieuwegein.
/// Controleert projectinstellingen, trap-geometrie (TR1-2 en TR3-6), bordes (BD1-3) en berekenbaarheid.
/// </summary>
public class ProjectRD505007Tests
{
    [Fact]
    public void Project_HeeftCorrecteProjectInfo()
    {
        var (_, _, _, project) = SteekTrapFactory.CreateProject_RD505007();

        project.ProjectInfo.Nummer.Should().Be("RD-505007");
        project.ProjectInfo.Naam.Should().Be("Mountain Network");
        project.ProjectInfo.Plaatsnaam.Should().Be("Nieuwegein");
        project.ProjectInfo.Grondslagen.Gevolgklasse.Should().Be(GevolgklasseEnum.CC2b);
        project.ProjectInfo.MinimaleREI.Should().Be(60);
    }

    [Fact]
    public void Project_HeeftCorrecteDefaultGebruiksklasse()
    {
        var (_, _, _, project) = SteekTrapFactory.CreateProject_RD505007();

        project.DefaultGebruiksklasse.Should().Be(GebruiksklasseEnum.C5_bijeenkomst_grote_menigtes);
    }

    [Fact]
    public void Project_HeeftDrieAssemblages()
    {
        var (_, _, _, project) = SteekTrapFactory.CreateProject_RD505007();

        project.Assemblages.Should().HaveCount(3);
        project.Assemblages.OfType<SteekTrapEntity>().Should().HaveCount(2);
        project.Assemblages.OfType<BordesEntity>().Should().HaveCount(1);
    }

    [Fact]
    public void Trap12_HeeftCorrecteGeometrie()
    {
        var (trap12, _, _, _) = SteekTrapFactory.CreateProject_RD505007();

        trap12.Merk.Should().Be("TR1-2");
        trap12.SchilDikte.Should().BeApproximately(150, 0.1);
        trap12.OptredeMaat.Should().BeApproximately(190, 0.1);
        trap12.AantredeMaat.Should().BeApproximately(190, 0.1);
        trap12.LengteTotaalEigenOpgave.Should().Be(2750);
        trap12.GebruikEigenLengte.Should().BeTrue();
        trap12.LtProjZ.Should().Be(2750);

             

        // belastingen
        // test bijweken
        trap12.Gebruiksklasse = GebruiksklasseEnum.B_kantoorgebouwen;
        trap12.Gebruiksklasse = GebruiksklasseEnum.C5_bijeenkomst_grote_menigtes;

        // belastingen
        trap12.Bijwerken();

        trap12.Gebruiksklasse.Should().Be(GebruiksklasseEnum.C5_bijeenkomst_grote_menigtes);
        trap12.Belastingen.BelastingGevallen[1].Gebruiksklasse.Should().Be(GebruiksklasseEnum.C5_bijeenkomst_grote_menigtes);
        trap12.PermanenteBelastingPerM2.Should().BeApproximately(8.25, 0.5);
        trap12.VeranderlijkeBelastingPerM2.Should().Be(5.0);
        trap12.VeranderlijkeBelastingPuntlast.Should().Be(7.0);

        // moment schil
        var schilKrachten = trap12.Krachten;
        schilKrachten.L.Should().BeApproximately(2.75, 0.1);
        
        // het moment zou moeten zijn
        // 1/8 x q x L^2 =
        // M_kar = 1/8 x (8.25 + 5) x 2.75^2 = 12.5 kNm


        schilKrachten.Mk.Should().BeApproximately(-12.5, 0.5);
        schilKrachten.MEd.Should().BeApproximately(-16.5, 0.5);
        schilKrachten.Mfreq.Should().BeApproximately(-11.1, 0.5);


        // wapening
        var br = trap12.MomentSchil;
        br.D.Should().BeApproximately(126, 0.1);
        br.AsRequired.Should().BeApproximately(307, 5);
        br.AsApplied.Should().BeGreaterThanOrEqualTo(br.AsRequired);

        // doorbuiging
        var db = trap12.DoorbuigingValidatie;
        db.Wc.Should().Be(0);
        db.Kruipkrimp.TheoretischeKruipCoefficient.Should().BeApproximately(1.80, 0.3);
        db.Wbijk.Should().BeApproximately(-3.6, 1); 
        db.Wmax.Should().BeApproximately(-4.6, 1); 

    }

    [Fact]
    public void Trap12_WijzigingProject_HeeftCorrectResultaat()
    {
        var (trap12, _, _, proj) = SteekTrapFactory.CreateProject_RD505007();



        // check voor projectwijziging
        trap12.ProjectInfo.Grondslagen.Gevolgklasse.Should().Be(GevolgklasseEnum.CC2b);


        // wijzig
        proj.ProjectInfo.Grondslagen.Gevolgklasse = GevolgklasseEnum.CC3;

        trap12.ProjectInfo.Grondslagen.Gevolgklasse.Should().Be(GevolgklasseEnum.CC3);
        // of nog beter
        trap12.ProjectInfo.Should().Be(proj.ProjectInfo);
        

    }


    [Fact]
    public void Trap36_HeeftCorrecteGeometrie()
    {
        var (_, trap36, _, _) = SteekTrapFactory.CreateProject_RD505007();

        trap36.Merk.Should().Be("TR3-6");
        trap36.SchilDikte.Should().BeApproximately(100, 0.1);
        trap36.OptredeMaat.Should().BeApproximately(204, 0.1);
        trap36.AantredeMaat.Should().BeApproximately(220, 0.1);
        trap36.LengteTotaalEigenOpgave.Should().Be(2040);
        trap36.GebruikEigenLengte.Should().BeTrue();
    }

    [Fact]
    public void Bordes_HeeftCorrecteGeometrie()
    {
        var (_, _, bordes, _) = SteekTrapFactory.CreateProject_RD505007();

        bordes.Merk.Should().Be("BD1-3");
        bordes.Dikte.Should().BeApproximately(240, 0.1);
        bordes.Breedte.Should().BeApproximately(1100, 0.1);
        bordes.Lengte.Should().BeApproximately(3700, 0.1);
    }

    [Fact]
    public void Bordes_HeeftTrappenGekoppeld()
    {
        var (trap12, trap36, bordes, _) = SteekTrapFactory.CreateProject_RD505007();

        bordes.Trap1.AansluitendElement.Should().Be(trap12);
        bordes.Trap2.AansluitendElement.Should().Be(trap36);
    }

    [Fact]
    public void Trap12_Bijwerken_GooitGeenUitzondering()
    {
        var (trap12, _, _, _) = SteekTrapFactory.CreateProject_RD505007();

        var act = () => trap12.Bijwerken();
        act.Should().NotThrow();
    }

    [Fact]
    public void Trap36_Bijwerken_GooitGeenUitzondering()
    {
        var (_, trap36, _, _) = SteekTrapFactory.CreateProject_RD505007();

        var act = () => trap36.Bijwerken();
        act.Should().NotThrow();
    }

    [Fact]
    public void Bordes_Bijwerken_GooitGeenUitzondering()
    {
        var (trap12, trap36, bordes, _) = SteekTrapFactory.CreateProject_RD505007();
        trap12.Bijwerken();
        trap36.Bijwerken();

        var act = () => bordes.Bijwerken();
        act.Should().NotThrow();
    }

    [Fact]
    public void Trap12_MomentSchil_IsNaBijwerkenBeschikbaar()
    {
        var (trap12, _, _, _) = SteekTrapFactory.CreateProject_RD505007();
        trap12.Bijwerken();

        trap12.MomentSchil.Should().NotBeNull();
        trap12.MomentSchil.AsRequired.Should().BePositive();
        trap12.MomentSchil.AsApplied.Should().BeGreaterThanOrEqualTo(trap12.MomentSchil.AsRequired);
    }

    [Fact]
    public void Trap36_MomentSchil_IsNaBijwerkenBeschikbaar()
    {
        var (_, trap36, _, _) = SteekTrapFactory.CreateProject_RD505007();
        trap36.Bijwerken();

        trap36.MomentSchil.Should().NotBeNull();
        trap36.MomentSchil.AsRequired.Should().BePositive();
        trap36.MomentSchil.AsApplied.Should().BeGreaterThanOrEqualTo(trap36.MomentSchil.AsRequired);
    }
}
