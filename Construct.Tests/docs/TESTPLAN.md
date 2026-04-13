# Testplan: AssemblageEntity — SteekTrap berekeningen

## Doel

Diverse standaard `SteekTrapEntity`-configuraties opbouwen, berekeningen uitvoeren
en de resultaten automatisch controleren met xUnit.

---

## Stap 1 – Testproject aanmaken

Voeg een nieuw **xUnit** project toe aan de solution: `Construct.Tests`

- Target: `.NET 9`
- Map: `Construct.Tests\`
- Projectreferenties toevoegen:
  - `Construct.Domain`
  - `Construct.Application`

```xml
<!-- Construct.Tests\Construct.Tests.csproj -->
<PackageReference Include="xunit" Version="2.9.*" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.8.*" />
<PackageReference Include="FluentAssertions" Version="6.*" />
```

---

## Stap 2 – TestFactory aanmaken

Maak `Construct.Tests\Factories\SteekTrapFactory.cs`.

Kopieer de logica uit `AddAssemblage.razor → GetSteekTrap()` naar een statische factory,
zodat alle tests hetzelfde startpunt gebruiken:

```csharp
public static class SteekTrapFactory
{
    /// <summary>
    /// Maakt een standaard SteekTrapEntity met een vers ProjectEntity en BetonContext (C20/25).
    /// </summary>
    public static (SteekTrapEntity trap, ProjectEntity project) Create(
        int aantalTreden = 16,
        double schildikte = 120,
        double optrede = 185,
        double aantrede = 220,
        bool heeftBoventand = true)
    {
        var project = new ProjectEntity();
        var beton = project.VoegMateriaalToe(new BetonContext("C20/25"));

        var trap = new SteekTrapEntity(project.ProjectInfo)
        {
            Id = Guid.NewGuid(),
            Naam = "test steektrap",
            Merk = "TR-T1",
            Materiaal = beton,
            MateriaalId = beton.Id,
            OptredeAantal1 = aantalTreden,
            OptredeMaat = optrede,
            AantredeMaat = aantrede,
            HeeftBoventand = heeftBoventand,
        };
        trap.SchilDikte = schildikte;
        trap.LengteBoven = trap.AantredeMaat + trap.WelMaat;

        trap.Belastingen.GenereerBelastingCombinaties(
            trap.Belastingen,
            trap.Belastingen.BelastingGevallen,
            trap.Belastingen.CombinatiesTypes);

        return (trap, project);
    }
}
```

---

## Stap 3 – De 4 testscenario's

| # | Naam                      | Treden | Optrede | Aantrede | Schildikte | Wel-type       | Tand |
|---|---------------------------|--------|---------|----------|------------|----------------|------|
| 1 | Halfverdiepigtstrap       | 8      | 187.5   | 220      | 100        | Rechte trede   | nee  |
| 2 | Volledige verdiepingstrap | 16     | 185     | 220      | 120        | StandaardWel   | ja   |
| 3 | Steile trap (nauwe ruimte)| 14     | 200     | 210      | 130        | Wel type 2     | ja   |
| 4 | Lange flauwe trap         | 18     | 170     | 240      | 150        | EigenOpgave    | nee  |

---

## Stap 4 – Geometrie-tests

Maak `Construct.Tests\SteekTrap\GeometrieTests.cs`.

Controleer per scenario de berekende afmetingen:

```csharp
[Fact]
public void HalfVerdieping_LengteTotaal_IsCorrect()
{
    var (trap, _) = SteekTrapFactory.Create(aantalTreden: 8, aantrede: 220, heeftBoventand: false);
    // LengteTotaal = n * aan (geen tand)
    trap.LengteTotaal.Should().BeApproximately(8 * 220.0, precision: 1.0);
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
    double verwacht = trap.LengteTotaal * trap.SchuineMaat / trap.AantredeMaat;
    trap.LengteSchuin.Should().BeApproximately(verwacht, precision: 1.0);
}

[Fact]
public void SchilDikte_BlijftGelijkNaInit()
{
    var (trap, _) = SteekTrapFactory.Create(schildikte: 130);
    trap.SchilDikte.Should().Be(130);
}
```

**Wat controleren:**
- `LengteTotaal` → `(n-1)*aan + halsDikte + tandLengte` (met tand), of `n*aan` (zonder tand)
- `HoogteTotaal` → `n * optrede`
- `LengteSchuin` → `LengteTotaal * SchuineMaat / AantredeMaat`
- `SchilDikte` → ingestelde waarde (geen drift na `Init`)
- `AdviesSchildikteMin` → positief getal, kleiner dan of gelijk aan `SchilDikte`

---

## Stap 5 – Belastingtests

Maak `Construct.Tests\SteekTrap\BelastingTests.cs`.

```csharp
[Fact]
public void EigenGewicht_LigtBinnenVerwachtBereik()
{
    var (trap, _) = SteekTrapFactory.Create();
    var gk = trap.GetGk();
    gk.Should().BeInRange(3.0, 8.0); // kN/m²
}

[Fact]
public void DsnOppTrede_KloptMetShoelace()
{
    var (trap, _) = SteekTrapFactory.Create();
    var opp1 = trap.GetDsnOppTrede();
    var opp2 = trap.GetDsnOppTredePolygoon();
    opp1.Should().BeApproximately(opp2, precision: 0.5);
}

[Fact]
public void GrotereSchilDikte_GeftHogerEigenGewicht()
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
```

**Wat controleren:**
- `GetGk()` in realistisch bereik [3 – 8 kN/m²]
- `GetDsnOppTrede()` ≈ `GetDsnOppTredePolygoon()` (schoenveterformule)
- Eigengewicht stijgt bij grotere schildikte
- `PermanenteBelastingPerM2` = `EigenGewichtPerM2` + `AfwerkingVlaklast`

---

## Stap 6 – Snedekrachten-tests

Maak `Construct.Tests\SteekTrap\SnedekrachtenTests.cs`.

```csharp
[Fact]
public void MEd_IsPositiefNaBijwerken()
{
    var (trap, _) = SteekTrapFactory.Create();
    trap.Bijwerken();
    trap.Snedekrachten.My.Should().BePositive();
}

[Fact]
public void VEd_IsPositiefNaBijwerken()
{
    var (trap, _) = SteekTrapFactory.Create();
    trap.Bijwerken();
    trap.Snedekrachten.Vz.Should().BePositive();
}

[Fact]
public void VEd_VerhoudingtotMEd_KloptMetOpgelegdeBalk()
{
    var (trap, _) = SteekTrapFactory.Create();
    trap.Bijwerken();
    // VEd ≈ q*L/2, MEd ≈ q*L²/8  → VEd ≈ 4*MEd/L
    double verwachtVEd = 4 * trap.Snedekrachten.My / (trap.LengteTotaal / 1000);
    trap.Snedekrachten.Vz.Should().BeApproximately(verwachtVEd, precision: verwachtVEd * 0.15);
}

[Fact]
public void SnedekrachtenBGT_KleinerDanOfGelijkAanUGT()
{
    var (trap, _) = SteekTrapFactory.Create();
    trap.Bijwerken();
    trap.SnedekrachtenBGT.My.Should().BeLessOrEqualTo(trap.Snedekrachten.My);
}
```

**Wat controleren:**
- `Snedekrachten.My > 0` na Bijwerken
- `Snedekrachten.Vz > 0`
- Verhouding `VEd/MEd` overeenkomt met opgelegde-balk formule (±15%)
- `SnedekrachtenBGT.My ≤ Snedekrachten.My` (qp ≤ qEd)

---

## Stap 7 – Toets-resultaten (akkoord-tests)

Maak `Construct.Tests\SteekTrap\ToetsTests.cs`.

```csharp
[Theory]
[InlineData(8,  187.5, 220, 100, false)]
[InlineData(16, 185,   220, 120, true)]
[InlineData(14, 200,   210, 130, true)]
[InlineData(18, 170,   240, 150, false)]
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
```

---

## Stap 8 – Regressietests (bekende waarden vastleggen)

Maak `Construct.Tests\SteekTrap\RegressieTests.cs`.

Werkwijze:
1. Draai de 4 trappen éénmaal (test of via de app).
2. Noteer de uitkomsten op de `// TODO`-regels hieronder.
3. Daarna dienen deze waarden als vaste referentie voor toekomstige wijzigingen.

```csharp
[Fact]
public void Trap1_HalfVerdieping_RegressieWaarden()
{
    var (trap, _) = SteekTrapFactory.Create(8, 100, 187.5, 220, false);
    trap.Bijwerken();

    trap.LengteTotaal.Should().BeApproximately(/* TODO */ 0, 1);
    trap.HoogteTotaal.Should().BeApproximately(/* TODO */ 0, 1);
    trap.Snedekrachten.My.Should().BeApproximately(/* TODO */ 0, 0.5);
    trap.Snedekrachten.Vz.Should().BeApproximately(/* TODO */ 0, 0.5);
    trap.MomentSchil.AsRequired.Should().BeApproximately(/* TODO */ 0, 5);
}

[Fact]
public void Trap2_VolVerdieping_RegressieWaarden()
{
    var (trap, _) = SteekTrapFactory.Create(16, 120, 185, 220, true);
    trap.Bijwerken();

    trap.LengteTotaal.Should().BeApproximately(/* TODO */ 0, 1);
    trap.Snedekrachten.My.Should().BeApproximately(/* TODO */ 0, 0.5);
    trap.MomentSchil.AsRequired.Should().BeApproximately(/* TODO */ 0, 5);
}

[Fact]
public void Trap3_SteileSmalleTrap_RegressieWaarden()
{
    var (trap, _) = SteekTrapFactory.Create(14, 130, 200, 210, true);
    trap.Bijwerken();

    trap.LengteTotaal.Should().BeApproximately(/* TODO */ 0, 1);
    trap.Snedekrachten.My.Should().BeApproximately(/* TODO */ 0, 0.5);
}

[Fact]
public void Trap4_LangeFlauweTrap_RegressieWaarden()
{
    var (trap, _) = SteekTrapFactory.Create(18, 150, 170, 240, false);
    trap.Bijwerken();

    trap.LengteTotaal.Should().BeApproximately(/* TODO */ 0, 1);
    trap.Snedekrachten.My.Should().BeApproximately(/* TODO */ 0, 0.5);
}
```

---

## Stap 9 – CI-integratie

Voeg het testproject toe aan de build pipeline:

```yaml
# .github/workflows/ci.yml  of  azure-pipelines.yml
- name: Run tests
  run: dotnet test Construct.Tests --configuration Release --logger trx
```

---

## Prioriteitsvolgorde implementatie

| Prioriteit | Stap                        | Reden                                      |
|------------|-----------------------------|--------------------------------------------|
| 1          | Stap 1 – project aanmaken   | Randvoorwaarde voor alles                  |
| 2          | Stap 2 – factory            | Randvoorwaarde voor alle tests             |
| 3          | Stap 4 – geometrie          | Eenvoudig, deterministisch, snel           |
| 4          | Stap 5 – belasting          | Eenvoudig, deterministisch                 |
| 5          | Stap 6 – snedekrachten      | Vereist `Bijwerken()`                      |
| 6          | Stap 7 – toets-akkoord      | Vereist `Bijwerken()`                      |
| 7          | Stap 8 – regressie          | Pas invullen na eerste succesvolle run     |
| 8          | Stap 3 – scenario's uitbr.  | Iteratief uitbreiden                       |
| 9          | Stap 9 – CI                 | Als alle tests groen zijn                  |

---

## Mappenstructuur testproject

```
Construct.Tests\
├── docs\
│   └── TESTPLAN.md                  ← dit bestand
├── Factories\
│   └── SteekTrapFactory.cs
├── SteekTrap\
│   ├── GeometrieTests.cs
│   ├── BelastingTests.cs
│   ├── SnedekrachtenTests.cs
│   ├── ToetsTests.cs
│   └── RegressieTests.cs
└── Construct.Tests.csproj
```
