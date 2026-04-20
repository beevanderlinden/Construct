using Construct.Application.Services;
using Construct.Domain.Entities;
using Construct.Tests.Factories;
using Eurocode.Belastingen;
using Eurocode.BetonConstructies;
using Eurocode.Grondslagen;
using FluentAssertions;
using Xunit;

namespace Construct.Tests.SteekTrap;

/// <summary>
/// Integratietest voor project RD-506383 "De Bloemenkamer aan Raadhuisstraat 73" SPRANG-CAPELLE.
/// Controleert projectinstellingen, trap-geometrie (TR01, TR02), bordes (BD01) en berekenbaarheid.
/// </summary>
public class ProjectRD506383Tests
{
    [Fact]
    public void Project_HeeftCorrecteProjectInfo()
    {
        var (_, _, _, project) = SteekTrapFactory.CreateProject_RD506383();

        project.ProjectInfo.Nummer.Should().Be("RD-506383");
        project.ProjectInfo.Naam.Should().Be("De Bloemenkamer aan Raadhuisstraat 73");
        project.ProjectInfo.Plaatsnaam.Should().Be("SPRANG-CAPELLE");
        project.ProjectInfo.Grondslagen.NationaleBijlage.Should().Be(NationaleBijlageEnum.NL);
        project.ProjectInfo.Grondslagen.Gevolgklasse.Should().Be(GevolgklasseEnum.CC2b);
        project.ProjectInfo.Grondslagen.OntwerpLevensduur.Should().Be(OntwerpLevensduurEnum.Vijftig);
    }

    [Fact] public void Project_WapeningIsZoalsVerwacht()
    {
        var (tr01, tr02, bd01, _) = SteekTrapFactory.CreateProject_RD506383();

        tr01.Bijwerken();
        tr02.Bijwerken();
        bd01.Bijwerken();

        // Actuele waarden ophalen voor diagnose
        var tr01Basis   = tr01.PlaatWapening?.Onder?.BasisWapening?.Tekst;
        var tr01Verdeel = tr01.PlaatWapening?.Onder?.VerdeelWapening?.Tekst;
        var tr01Tand    = tr01.TandOpleggingBovenzijde?.WapeningAlgemeen?.Tekst;


        var tr02Basis   = tr02.PlaatWapening?.Onder?.BasisWapening?.Tekst;
        var tr02Verdeel = tr02.PlaatWapening?.Onder?.VerdeelWapening?.Tekst;
        var tr02Tand    = tr02.TandOpleggingBovenzijde?.WapeningAlgemeen?.Tekst;


        var bd01Basis   = bd01.PlaatWapening?.Onder?.BasisWapening?.Tekst;
        var bd01Verdeel = bd01.PlaatWapening?.Onder?.VerdeelWapening?.Tekst;
        var bd01Vs1     = bd01.Stroken.Last().PlaatWapening?.Onder?.BasisWapening?.Tekst;

        var defaultWap1 = "r8-150";
        var defaultWap2 = "Ø6-115";

        // ik verwacht:
        tr01Basis.Should().Be("r8-125");
        tr01Verdeel.Should().Be(defaultWap1);
        tr01Tand.Should().Be(defaultWap2);

        tr02Basis.Should().Be(defaultWap1);
        tr02Verdeel.Should().Be(defaultWap1);
        tr02Tand.Should().Be(defaultWap2);

        bd01Basis.Should().Be(defaultWap1);
        bd01Verdeel.Should().Be(defaultWap1);
        bd01Vs1.Should().Be("8-150+3r8");
       
    }


    [Fact]
    public void Project_HeeftCorrecteDefaultGebruiksklasse()
    {
        var (_, _, _, project) = SteekTrapFactory.CreateProject_RD506383();

        project.DefaultGebruiksklasse.Should().Be(GebruiksklasseEnum.A_gemeenschappelijke_trappen);
    }

    [Fact]
    public void Project_HeeftDrieAssemblages()
    {
        var (_, _, _, project) = SteekTrapFactory.CreateProject_RD506383();

        project.Assemblages.Should().HaveCount(3);
        project.Assemblages.OfType<SteekTrapEntity>().Should().HaveCount(2);
        project.Assemblages.OfType<BordesEntity>().Should().HaveCount(1);
    }

    [Fact]
    public void TR01_HeeftCorrecteGeometrie()
    {
        var (tr01, _, _, _) = SteekTrapFactory.CreateProject_RD506383();

        tr01.Merk.Should().Be("TR01");
        tr01.OptredeMaat.Should().BeApproximately(184, 0.1);
        tr01.AantredeMaat.Should().BeApproximately(220, 0.1);
        tr01.SchilDikte.Should().BeApproximately(150, 0.1);
        tr01.LtProjZ.Should().BeApproximately(3500, 0.1);
    }

    [Fact]
    public void TR02_HeeftCorrecteGeometrie()
    {
        var (_, tr02, _, _) = SteekTrapFactory.CreateProject_RD506383();

        tr02.Merk.Should().Be("TR02");
        tr02.OptredeMaat.Should().BeApproximately(184, 0.1);
        tr02.AantredeMaat.Should().BeApproximately(220, 0.1);
        tr02.SchilDikte.Should().BeApproximately(150, 0.1);
        tr02.LtProjZ.Should().BeApproximately(1900, 0.1);
    }

    [Fact]
    public void BD01_HeeftCorrecteGeometrie()
    {
        var (_, _, bd01, _) = SteekTrapFactory.CreateProject_RD506383();

        bd01.Merk.Should().Be("BD01");
        bd01.Lengte.Should().BeApproximately(2550, 0.1);
        bd01.Breedte.Should().BeApproximately(1280, 0.1);
    }

    [Fact]
    public void BD01_HeeftCorrecteAansluitingen()
    {
        var (tr01, tr02, bd01, _) = SteekTrapFactory.CreateProject_RD506383();

        bd01.Trap1.AansluitendElement.Should().Be(tr02);
        bd01.Trap1.Randafstand.Should().BeApproximately(150, 0.1);

        bd01.Trap2.AansluitendElement.Should().Be(tr02);
        bd01.Trap2.Randafstand.Should().BeApproximately(150, 0.1);
    }

    [Fact]
    public void TR01_Bijwerken_GooitGeenUitzondering()
    {
        var (tr01, _, _, _) = SteekTrapFactory.CreateProject_RD506383();

        var act = () => tr01.Bijwerken();
        act.Should().NotThrow();
    }

    [Fact]
    public void TR02_Bijwerken_GooitGeenUitzondering()
    {
        var (_, tr02, _, _) = SteekTrapFactory.CreateProject_RD506383();

        var act = () => tr02.Bijwerken();
        act.Should().NotThrow();
    }

    [Fact]
    public void BD01_Bijwerken_GooitGeenUitzondering()
    {
        var (_, _, bd01, _) = SteekTrapFactory.CreateProject_RD506383();

        var act = () => bd01.Bijwerken();
        act.Should().NotThrow();
    }

    [Fact]
    public void TR01_MomentSchil_IsNaBijwerkenBeschikbaar()
    {
        var (tr01, _, _, _) = SteekTrapFactory.CreateProject_RD506383();
        tr01.Bijwerken();

        tr01.MomentSchil.Should().NotBeNull();
        tr01.MomentSchil.AsRequired.Should().BePositive();
        tr01.MomentSchil.AsApplied.Should().BeGreaterThanOrEqualTo(tr01.MomentSchil.AsRequired);
    }

    [Fact]
    public void TR02_MomentSchil_IsNaBijwerkenBeschikbaar()
    {
        var (_, tr02, _, _) = SteekTrapFactory.CreateProject_RD506383();
        tr02.Bijwerken();

        tr02.MomentSchil.Should().NotBeNull();
        tr02.MomentSchil.AsRequired.Should().BePositive();
        tr02.MomentSchil.AsApplied.Should().BeGreaterThanOrEqualTo(tr02.MomentSchil.AsRequired);
    }

    /// <summary>
    /// Schrijft het project als .cprj-bestand naar wwwroot\defaults zodat het in de UI geopend kan worden.
    /// </summary>
    [Fact]
    public async Task Project_Opslaan_Als_CprjBestand()
    {
        var (_, _, _, project) = SteekTrapFactory.CreateProject_RD506383();

        var fileService = new FileService();
        var bytes = await fileService.GetGzippedJsonBytesAsync(project);

        var outputPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
                "Construct.WebUI.Server", "wwwroot", "defaults",
                "506383-de-bloemenkamer.cprj"));

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await File.WriteAllBytesAsync(outputPath, bytes);

        File.Exists(outputPath).Should().BeTrue(because: "het .cprj-bestand moet zijn aangemaakt");
        new FileInfo(outputPath).Length.Should().BeGreaterThan(0, because: "het bestand mag niet leeg zijn");
    }
}
