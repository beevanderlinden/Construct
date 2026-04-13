using Construct.Application.Services;
using Construct.Domain.Common;
using Construct.Domain.Entities;
using Construct.Tests.Factories;
using Eurocode.BetonConstructies;
using FluentAssertions;
using Xunit;

namespace Construct.Tests.SteekTrap;

/// <summary>
/// Integratietests voor het opslaan en inladen van een .cprj-bestand.
/// Controleert dat na deserialisatie alle nested object-referenties correct zijn hersteld
/// via <see cref="AssemblageEntity.RestoreReferencesAfterDeserialization"/>.
/// </summary>
public class CprjSerializatieTests
{
    /// <summary>
    /// Maakt project RD-505003 aan, serialiseert het naar gzipped JSON (cprj-formaat),
    /// deserialiseert het terug en herstelt alle referenties — net zoals ProjectStateService doet.
    /// </summary>
    private static async Task<(SteekTrapEntity trap, ProjectEntity project)> RoundtripAsync()
    {
        var (_, original) = SteekTrapFactory.CreateProject_RD505003();

        var fileService = new FileService();
        var bytes = await fileService.GetGzippedJsonBytesAsync(original);

        var geladen = fileService.LoadFromBytes<ProjectEntity>(bytes);
        geladen.Should().NotBeNull(because: "deserialisatie mag niet mislukken");

        foreach (var assemblage in geladen!.Assemblages)
        {
            assemblage.RestoreReferencesAfterDeserialization(geladen);
        }

        var trap = geladen.Assemblages.OfType<SteekTrapEntity>().First();
        return (trap, geladen);
    }

    // ── Structuur ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Cprj_Inladen_LeverttGeldigProject()
    {
        var (trap, project) = await RoundtripAsync();

        project.Should().NotBeNull();
        project.Assemblages.Should().HaveCount(1);
        trap.Should().NotBeNull();
    }

    // ── Projectinfo ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Cprj_ProjectInfo_IsCorrectGedeserialiseerd()
    {
        var (_, project) = await RoundtripAsync();

        project.ProjectInfo.Nummer.Should().Be("RD-505003");
        project.ProjectInfo.Naam.Should().Be("12 app Hoflaan");
        project.ProjectInfo.Plaatsnaam.Should().Be("STOLWIJK");
        project.ProjectInfo.MinimaleREI.Should().Be(60);
    }

    // ── Materiaal-referentie ─────────────────────────────────────────────────

    [Fact]
    public async Task Cprj_Referentie_MateriaalIsGekoppeld()
    {
        var (trap, project) = await RoundtripAsync();

        trap.Materiaal.Should().NotBeNull(because: "Materiaal moet hersteld zijn via MateriaalId");
        trap.MateriaalId.Should().NotBeNull();
        trap.Materiaal!.Id.Should().Be(trap.MateriaalId!.Value,
            because: "Materiaal.Id moet overeenkomen met MateriaalId");
        project.Materialen.Should().ContainKey(trap.MateriaalId.Value,
            because: "MateriaalId moet aanwezig zijn in project.Materialen");
        project.Materialen[trap.MateriaalId.Value].Should().BeSameAs(trap.Materiaal,
            because: "trap.Materiaal moet de exacte instantie uit project.Materialen zijn");
    }

    [Fact]
    public async Task Cprj_Referentie_MateriaalBetonsterkteklasseGeborgd()
    {
        var (trap, _) = await RoundtripAsync();

        (trap.Materiaal as BetonContext)?.Betonsterkteklasse
            .Should().Be(BetonsterkteklasseEnum.C45_55, because: "betonsterkteklasse moet na roundtrip gelijk blijven");
    }

    // ── ProjectInfo-referentie ───────────────────────────────────────────────

    [Fact]
    public async Task Cprj_Referentie_ProjectInfoGekoppeldAanProject()
    {
        var (trap, project) = await RoundtripAsync();

        trap.ProjectInfo.Should().NotBeNull();
        trap.ProjectInfo.Should().BeSameAs(project.ProjectInfo,
            because: "trap.ProjectInfo moet dezelfde instantie zijn als project.ProjectInfo");
    }

    // ── Belastingen.Grondslagen-referentie ───────────────────────────────────

    [Fact]
    public async Task Cprj_Referentie_BelastingenGrondslagenGekoppeld()
    {
        var (trap, project) = await RoundtripAsync();

        trap.Belastingen.Should().NotBeNull();
        trap.Belastingen.Grondslagen.Should().NotBeNull();
        trap.Belastingen.Grondslagen.Should().BeSameAs(project.ProjectInfo.Grondslagen,
            because: "Belastingen.Grondslagen moet verwijzen naar project.ProjectInfo.Grondslagen");
    }

    // ── Berekende contexten ──────────────────────────────────────────────────

    [Fact]
    public async Task Cprj_Referentie_MomentSchilNietNull()
    {
        var (trap, _) = await RoundtripAsync();

        trap.MomentSchil.Should().NotBeNull(because: "MomentSchil moet door Init() aangemaakt zijn");
    }

    [Fact]
    public async Task Cprj_Referentie_WapeningSchilNietNull()
    {
        var (trap, _) = await RoundtripAsync();

        trap.WapeningSchil.Should().NotBeNull(because: "WapeningSchil moet na deserialisatie beschikbaar zijn");
    }

    [Fact]
    public async Task Cprj_Referentie_ProfielSchilNietNull()
    {
        var (trap, _) = await RoundtripAsync();

        trap.ProfielSchil.Should().NotBeNull();
        trap.ProfielSchil.Hoogte.Should().BeApproximately(150, 0.1,
            because: "schildikte moet na roundtrip gelijk zijn aan de opgegeven waarde (150 mm)");
    }

    [Fact]
    public async Task Cprj_Referentie_PlaatDekkingNietNull()
    {
        var (trap, _) = await RoundtripAsync();

        trap.PlaatDekking.Should().NotBeNull();
        trap.PlaatDekking.Boven.Should().NotBeNull(because: "DekkingBoven moet na Init() beschikbaar zijn");
        trap.PlaatDekking.Onder.Should().NotBeNull(because: "DekkingOnder moet na Init() beschikbaar zijn");
    }

    // ── ValidationReport ─────────────────────────────────────────────────────

    [Fact]
    public async Task Cprj_Validatie_AllReferentiesGezond()
    {
        var (_, project) = await RoundtripAsync();

        var report = ReferenceValidationHelper.ValidateProjectReferences(project);

        report.IsHealthy.Should().BeTrue(
            because: report.Issues.Count > 0
                ? "Openstaande issues: " + string.Join("; ", report.Issues.Select(i => i.ErrorMessage))
                : "alle referenties moeten hersteld zijn");
        report.InvalidMaterialReferences.Should().Be(0);
        report.InvalidAssemblageReferences.Should().Be(0);
    }

    // ── Regressie: berekenbaarheid na roundtrip ──────────────────────────────

    [Fact]
    public async Task Cprj_NaBijwerken_GooitGeenUitzondering()
    {
        var (trap, _) = await RoundtripAsync();

        var act = () => trap.Bijwerken();
        act.Should().NotThrow(because: "Bijwerken() mag na een cprj-roundtrip geen uitzondering gooien");
    }

    [Fact]
    public async Task Cprj_NaBijwerken_MomentSchilRegressieWaarden()
    {
        var (trap, _) = await RoundtripAsync();
        trap.Bijwerken();

        var m = trap.MomentSchil;
        m.Should().NotBeNull();

        m.Moment.Should().BeApproximately(-21.6, 1.0,   because: "rekenmoment M~Ed~ moet gelijk zijn na roundtrip");
        m.AsRequired.Should().BeApproximately(404, 20,  because: "benodigde wapening A~s,req~ moet gelijk zijn na roundtrip");
        m.AsApplied.Should().BeApproximately(437, 20,   because: "toegepaste wapening A~s,prov~ moet gelijk zijn na roundtrip");
        m.AsProvidedText.Should().Be("Ø8-120",           because: "wapeningkeuze moet na roundtrip gelijk zijn");
        m.Hoogte.Should().BeApproximately(150, 0.1,     because: "profielhoogte h moet gelijk zijn na roundtrip");
    }

    [Fact]
    public async Task Cprj_NaBijwerken_DoorbuigingRegressieWaarden()
    {
        var (trap, _) = await RoundtripAsync();
        trap.Bijwerken();

        var doorbuiging = trap.Toetsen
            .OfType<DoorbuigingValidatieContext>()
            .FirstOrDefault();

        doorbuiging.Should().NotBeNull(because: "DoorbuigingValidatieContext moet aanwezig zijn na Bijwerken()");
        doorbuiging!.LengteMM.Should().BeApproximately(4584, 40,    because: "rekenlengthe doorbuiging moet gelijk zijn na roundtrip");
        doorbuiging.Wmax.Should().BeApproximately(-7.88, 1.0,        because: "maximale doorbuiging Wmax moet gelijk zijn na roundtrip");
        doorbuiging.Wbijk.Should().BeApproximately(-5.98, 1.0,       because: "bijkomende doorbuiging Wbijk moet gelijk zijn na roundtrip");
    }
}
