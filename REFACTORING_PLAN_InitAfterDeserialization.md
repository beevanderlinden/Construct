# Refactoring: Init After Deserialization - ? VOLTOOID

## Status
? **IMPLEMENTATIE VOLTOOID EN GEBOUWD**

---

## Probleem (OPGELOST)
De `BordesEntity.Init(ProjectInfoEntity projectInfo)` methode werd **3x opgeroepen** bij laden van een project met 1 bordes, wat inefficiënt was. Dit gebeurde omdat:
1. Referenties na JSON-deserialisatie moeten worden hersteld
2. Dit gebeurde ad-hoc en meerdere keren
3. Er was geen centraal moment voor initialisatie na deserialisatie

## Huidige situatie (VOOR REFACTORING)

### Problemen die zijn opgelost:
- ? **Belastingen = new(...)** - overschreef bestaande Belastingen-relaties
  - ? **Nu: Belastingen ??= new()** - bewaart bestaande data
- ? **Geen gestructureerde post-deserialisatie flow**
  - ? **Nu: Centraal moment in ProjectStateService.RestoreNavigationProperties()**
- ? **Init() werd aangeroepen uit constructor**
  - ? **Nu: Alleen in RestoreNavigationProperties() na deserialisatie**
- ? **Father-relatie in VerbindingAansluitendElement kon niet worden hersteld**
  - ? **Nu: Father property is settable (niet meer init-only)**

---

## Implementatie Gelokaliseerd

### 1. ? AssemblageEntity.cs (base class)
**Toegevoegd:** Centraal moment voor post-deserialisatie herstel
```csharp
/// <summary>
/// Herstelt object-referenties en relaties na JSON-deserialisatie.
/// Roept slechts eenmaal aan via ProjectStateService.RestoreNavigationProperties()
/// </summary>
public virtual void RestoreReferencesAfterDeserialization(ProjectInfoEntity projectInfo)
{
    ProjectInfo = projectInfo;
}
```

### 2. ? BordesEntity.cs
**Verwijderd:**
- `Init(ProjectInfo)` call uit constructor (regel 96)

**Hernoemd en Refactored:**
- `public override void Init()` ? `public override void RestoreReferencesAfterDeserialization()`
- Gewijzigd: `Belastingen = new(...)` ? `Belastingen ??= new(...)`
- Gewijzigd: `PlaatDekking.Onder = new(...)` ? `PlaatDekking.Onder ??= new(...)`
- Gewijzigd: `PlaatDekking.Boven = new(...)` ? `PlaatDekking.Boven ??= new(...)`
- Gewijzigd: `PlaatWapening = new(...)` ? `PlaatWapening ??= new(...)`
- **Toegevoegd:** Relatie-herstel voor VerbindingAansluitendElement:
  ```csharp
  // ? Herstel bidirectionele Father-relaties
  Trap1.Father = this;
  Trap2.Father = this;
  ```

### 3. ? VerbindingAansluitendElement.cs
**Gewijzigd:** Father property van `init` naar `set`
```csharp
// VOOR: public AssemblageEntity Father { get; init; } = null!;
// NA:   public AssemblageEntity Father { get; set; } = null!;
```
Dit permite herstel van relaties na deserialisatie.

### 4. ? ProjectStateService.cs
**Uitgebreid:** RestoreNavigationProperties() met centraal moment voor alle relatie-herstel
```csharp
// ? NIEUW: Roep RestoreReferencesAfterDeserialization aan per assemblage
// Dit is het centraal moment waar alle relaties eenmalig worden hersteld
foreach (var assemblage in project.Assemblages)
{
    assemblage.RestoreReferencesAfterDeserialization(project.ProjectInfo);
}
```

---

## Voordelen van deze Refactoring

| Voordeel | Toelichting |
|----------|-------------|
| **Eenmalig** | RestoreReferencesAfterDeserialization wordt slechts 1x per assemblage aangeroepen |
| **Gestructureerd** | Centraal moment (ProjectStateService) voor alle herstelwerk |
| **Testbaar** | Mock ProjectInfoEntity en test RestoreReferencesAfterDeserialization onafhankelijk |
| **Extensible** | Andere AssemblageEntity subclasses kunnen eigen RestoreReferences implementeren |
| **Geen data-loss** | `??=` operators bewaren bestaande data (bv. BelastingCombinaties) |
| **Enkelvoudige verantwoordelijkheid** | Duidelijke scheiding tussen:
  - Constructor: basis-object setup
  - RestoreReferencesAfterDeserialization: relatie-herstel na deserialisatie |

---

## Flow na deze Refactoring

```
JSON-bestand geladen
  ?
JsonSerializer.Deserialize<ProjectEntity>()
  ?
ProjectStateService.SetProject(project)
  ?
ProjectStateService.RestoreNavigationProperties(project)
  ?? [Oude flow] Herstel Materialen Dictionary
  ?? [Oude flow] Herstel AansluitendeElementen in BordesEntity's
  ?? [NIEUW] Roep RestoreReferencesAfterDeserialization() per assemblage ?
    ?? BordesEntity.RestoreReferencesAfterDeserialization()
      ?? ProjectInfo = projectInfo
      ?? Belastingen ??= new(...) [Bewaart bestaande combinaties!]
      ?? Belastingen.Grondslagen = ProjectInfo.Grondslagen
      ?? Trap1.Father = this
      ?? Trap2.Father = this
      ?? PlaatDekking & PlaatWapening herstel met ??=
```

---

## Build Status
? **Build succesvol** - Geen fouten of waarschuwingen gerelateerd aan deze refactoring

---

## Toekomstige verbetering
Andere AssemblageEntity subclasses (SteekTrapEntity, KolomEntity, LiggerEntity) kunnen hun eigen `RestoreReferencesAfterDeserialization()` implementatie krijgen als zij speciale relatie-herstel nodig hebben.
