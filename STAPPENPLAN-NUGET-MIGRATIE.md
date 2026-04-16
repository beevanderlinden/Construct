# Stappenplan: Van project references naar NuGet packages

## Doel
- **Collega's** kunnen de `Construct` solution builden zonder de `Eurocode` en `Profielen` repos lokaal te hebben.
- **Jij** kunt lokaal blijven debuggen via project references, zonder iets te committen dat anderen breekt.

## Overzicht externe project references (te vervangen)

### In `Construct.Domain`
| Huidig (project reference) | NuGet package naam |
|---|---|
| `CommonLibrary` | `Kaskon.CommonLibrary` |
| `Eurocode0.Grondslagen` | `Kaskon.Eurocode0.Grondslagen` |
| `Eurocode1.Belastingen` | `Kaskon.Eurocode1.Belastingen` |
| `Eurocode2.BetonConstructies` | `Kaskon.Eurocode2.BetonConstructies` |
| `Eurocode3.StaalConstructies` | `Kaskon.Eurocode3.StaalConstructies` |
| `Eurocode5.HoutConstructies` | `Kaskon.Eurocode5.HoutConstructies` |
| `ExportFactory` | `Kaskon.ExportFactory` |
| `ParametrischeProfielen` | `Kaskon.ParametrischeProfielen` |
| `StaalProfielen` | `Kaskon.Profielen.Staal` |

### In `Construct.WebUI.Server`
| Huidig (project reference) | NuGet package naam |
|---|---|
| `EurocodeRazorClassLibrary` | `Kaskon.EurocodeRazorClassLibrary` |

---

## Stap 1 – Controleer de juiste NuGet package namen en versies

Kijk in de NuGet feed op `T:\04 Standaarden\NuGetPackages` welke packages beschikbaar zijn
en noteer de **nieuwste versie** van elk package uit de tabel hierboven.

> De packages die al als `PackageReference` in de `.csproj` staan (zoals `Kaskon.Algemeen`,
> `Kaskon.Toolbox.PrefabModels`) hoef je niet aan te raken.

---

## Stap 2 – Voeg een `nuget.config` toe aan de solution root

Zodat collega's automatisch de juiste NuGet feed gebruiken:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="Lokale NuGetPackages" value="T:\04 Standaarden\NuGetPackages" />
  </packageSources>
</configuration>
```

> **Let op:** als collega's het T-schijfpad niet hebben, gebruik dan een UNC-pad
> (`\\server\share\...`) of publiceer naar een gezamenlijke feed zoals Azure Artifacts of
> GitHub Packages.

---

## Stap 3 – Vervang project references door PackageReferences in `Construct.Domain.csproj`

Verwijder het gehele `<ItemGroup>` blok met de externe `<ProjectReference>` items
en vervang door `<PackageReference>` items:

```xml
<ItemGroup>
  <PackageReference Include="Kaskon.CommonLibrary" Version="5.0.0" />
  <PackageReference Include="Kaskon.Eurocode0.Grondslagen" Version="5.0.0" />
  <PackageReference Include="Kaskon.Eurocode1.Belastingen" Version="5.0.0" />
  <PackageReference Include="Kaskon.Eurocode2.BetonConstructies" Version="5.0.0" />
  <PackageReference Include="Kaskon.Eurocode3.StaalConstructies" Version="5.0.0" />
  <PackageReference Include="Kaskon.Eurocode5.HoutConstructies" Version="5.0.0" />
  <PackageReference Include="Kaskon.ExportFactory" Version="5.0.0" />
  <PackageReference Include="Kaskon.ParametrischeProfielen" Version="5.0.0" />
  <PackageReference Include="Kaskon.Profielen.Staal" Version="5.0.0" />
</ItemGroup>
```

*(Vul de juiste versienummers in uit stap 1.)*

---

## Stap 4 – Vervang project reference in `Construct.WebUI.Server.csproj`

Zelfde werkwijze: vervang de `<ProjectReference>` voor `EurocodeRazorClassLibrary`
door een `<PackageReference>`.

---

## Stap 5 – Verwijder de externe projecten uit de solution

De `.sln` bevat nu nog verwijzingen naar de externe `.csproj` bestanden
(`Eurocode`, `Profielen`). Verwijder ze via Visual Studio:

1. Klik rechts op het project in Solution Explorer → **Remove**
2. Doe dit voor alle externe projecten (zie lijst bovenaan)
3. Sla de `.sln` op

> Dit verwijdert alleen de solution-referentie, niet de bestanden op schijf.

---

## Stap 6 – Verifieer dat de solution bouwt

```
dotnet restore
dotnet build
```

Als dit slaagt, kunnen collega's nu builden zonder de `Eurocode` repo lokaal te hebben.

---

## Stap 7 – Lokale debug setup: `Directory.Build.props.user`

Zodat jij project references kunt blijven gebruiken zonder dat dit gecommit wordt:

### 7a – Voeg toe aan `.gitignore`

```
Directory.Build.props.user
```

### 7b – Maak `Directory.Build.props` aan in de solution root (wél committen)

```xml
<Project>
  <!-- Laad lokale user-overrides als het bestand bestaat (nooit committen) -->
  <Import Project="Directory.Build.props.user" Condition="Exists('Directory.Build.props.user')" />
</Project>
```

### 7c – Maak `Directory.Build.props.user` aan op jouw machine (nooit committen)

```xml
<Project>
  <ItemGroup>
    <!-- Vervang NuGet packages door lokale project references voor debuggen -->
    <PackageReference Remove="Kaskon.CommonLibrary" />
    <PackageReference Remove="Kaskon.Eurocode0.Grondslagen" />
    <PackageReference Remove="Kaskon.Eurocode1.Belastingen" />
    <PackageReference Remove="Kaskon.Eurocode2.BetonConstructies" />
    <PackageReference Remove="Kaskon.Eurocode3.StaalConstructies" />
    <PackageReference Remove="Kaskon.Eurocode5.HoutConstructies" />
    <PackageReference Remove="Kaskon.ExportFactory" />
    <PackageReference Remove="Kaskon.ParametrischeProfielen" />
    <PackageReference Remove="Kaskon.Profielen.Staal" />
    <PackageReference Remove="Kaskon.EurocodeRazorClassLibrary" />

    <ProjectReference Include="C:\Users\bvanderlinden\source\repos\Eurocode\CommonLibrary\CommonLibrary.csproj" />
    <ProjectReference Include="C:\Users\bvanderlinden\source\repos\Eurocode\Eurocode0.Grondslagen\Eurocode0.Grondslagen.csproj" />
    <ProjectReference Include="C:\Users\bvanderlinden\source\repos\Eurocode\Eurocode1.Belastingen\Eurocode1.Belastingen.csproj" />
    <ProjectReference Include="C:\Users\bvanderlinden\source\repos\Eurocode\Eurocode2.BetonConstructies\Eurocode2.BetonConstructies.csproj" />
    <ProjectReference Include="C:\Users\bvanderlinden\source\repos\Eurocode\Eurocode3.StaalConstructies\Eurocode3.StaalConstructies.csproj" />
    <ProjectReference Include="C:\Users\bvanderlinden\source\repos\Eurocode\Eurocode5.HoutConstructies\Eurocode5.HoutConstructies.csproj" />
    <ProjectReference Include="C:\Users\bvanderlinden\source\repos\Eurocode\ExportFactory\ExportFactory.csproj" />
    <ProjectReference Include="C:\Users\bvanderlinden\source\repos\Profielen\ParametrischeProfielen\ParametrischeProfielen.csproj" />
    <ProjectReference Include="C:\Users\bvanderlinden\source\repos\Profielen\StaalProfielen\StaalProfielen.csproj" />
    <ProjectReference Include="C:\Users\bvanderlinden\source\repos\Eurocode\EurocodeRazorClassLibrary\EurocodeRazorClassLibrary.csproj" />
  </ItemGroup>
</Project>
```

---

## Stap 8 – Werkwijze bij toekomstige wijzigingen in Eurocode

| Situatie | Actie |
|---|---|
| Je wijzigt iets in Eurocode dat Construct nodig heeft | Verhoog versie in Eurocode → publiceer nieuw NuGet package naar `T:\04 Standaarden\NuGetPackages` → update versienummer in `.csproj` → commit |
| Collega wil de nieuwe versie | `dotnet restore` haalt de nieuwe NuGet op |
| Jij wilt debuggen in Eurocode | `Directory.Build.props.user` regelt dit automatisch |

---

## Aandachtspunten

- **`HoutProfielen` en `BetonProfielen`** staan in de solution maar niet als directe
  `ProjectReference` in een `.csproj`. Controleer of dit indirect meekomt via een
  andere project reference, of dat ze los verwijderd kunnen worden.
- **Versie pinning:** gebruik exacte versies in de `.csproj` (geen wildcards) zodat
  collega's altijd dezelfde versie gebruiken.
- **`Mechanica.LiggerSB`** is een intern project in de solution en hoeft niet vervangen te worden.
