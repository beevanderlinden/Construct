# Implementatieplan: Reference-by-ID Serialisatie

## ?? Doel
Reduceer JSON grootte en duplicatie door complexe properties **ENKEL via IDs** te serialiseren.

**Expected Result:**
- JSON 70-80% kleiner
- Geen materiaal-duplicatie
- Single source of truth voor complexe objecten

---

## ?? Fase 1: Analyseer Huidige Complexe Properties

### In AssemblageEntity (BaseClass):
```csharp
// ? Worden NU mee serialiseerd (hele objecten!)
public ProjectInfoEntity ProjectInfo { get; set; }
public BelastingenContext Belastingen { get; set; }
public DekkingContext PlaatDekking { get; set; }
public BaseMateriaal Materiaal { get; set; }

// ? Zouden ID's moeten zijn
public Guid ProjectInfoId { get; set; }       // ? ProjectInfo reference
public Guid? MateriaalId { get; set; }        // ? Materiaal reference
public Guid? BelastingenId { get; set; }      // ? Belastingen reference
```

### In BordesEntity (Afgeleide):
```csharp
// ? Worden NU mee serialiseerd
public VerbindingAansluitendElement Trap1 { get; set; }  // Bevat heel veel data
public VerbindingAansluitendElement Trap2 { get; set; }

// ? Al aanwezig (goed!)
public Guid? Trap1AansluitendElementGuid { get; set; }   // ? GOED!
public Guid? Trap2AansluitendElementGuid { get; set; }
```

---

## ?? Fase 2: Wijzigingen in AssemblageEntity

### 2.1 Voeg ID Properties toe

```csharp
public abstract class AssemblageEntity : BaseAssemblage
{
    // ? ID REFERENCES (primitieve types, klein)
    [JsonInclude]
    public Guid ProjectInfoId { get; set; } = Guid.Empty;
    
    [JsonInclude]
    public Guid? MateriaalId { get; set; }
    
    // ? COMPLEX OBJECTS (JsonIgnore!)
    [JsonIgnore]
    private ProjectInfoEntity _projectInfo = new();
    
    [JsonIgnore]
    public ProjectInfoEntity ProjectInfo
    {
        get => _projectInfo;
        set => SetNestedProperty(ref _projectInfo, value);
    }
    
    [JsonIgnore]
    private BaseMateriaal _materiaal = new BetonContext("C30/37");
    
    [JsonIgnore]
    public BaseMateriaal Materiaal
    {
        get => _materiaal;
        set => SetNestedProperty(ref _materiaal!, value);
    }
    
    // Idem voor andere complexe properties...
    [JsonIgnore]
    public DekkingContext PlaatDekking { get; set; } = new();
    
    [JsonIgnore]
    public BelastingenContext Belastingen { get; set; } = new();
}
```

### 2.2 Wat NIET verandert

```csharp
// Deze kunnen blijven (primitieve types / value types)
public Guid Guid { get; set; }
public string? Naam { get; set; }
public string? Merk { get; set; }
public double Breedte { get; set; }
public double Lengte { get; set; }

// Deze was al ID-based (goed!)
public Guid? Trap1AansluitendElementGuid { get; set; }
public Guid? Trap2AansluitendElementGuid { get; set; }
```

---

## ?? Fase 3: Serialisatie Update (SaveProjectToLocalStorageAsync)

**VOOR (huidig):**
```csharp
public async Task SaveProjectToLocalStorageAsync()
{
    // MateriaalJson als workaround voor polymorfisme
    foreach (var assemblage in CurrentProject.Assemblages)
    {
        assemblage.MateriaalJson = 
            JsonSerializer.SerializeToNode(assemblage.Materiaal, ...);
    }
    
    var jsonString = JsonSerializer.Serialize(CurrentProject, ...);
}
```

**NA (ref-by-ID):**
```csharp
public async Task SaveProjectToLocalStorageAsync()
{
    if (CurrentProject is null) return;

    // ? NIEUW: Stel alle ID references in VOOR serialisatie
    foreach (var assemblage in CurrentProject.Assemblages)
    {
        // ID's instellen voor serialisatie
        assemblage.ProjectInfoId = CurrentProject.ProjectInfo.Id;  
        assemblage.MateriaalId = assemblage.Materiaal?.Id ?? Guid.Empty;
        
        // MateriaalJson niet meer nodig! (want Materiaal is JsonIgnore nu)
        // assemblage.MateriaalJson = ...;  // ? VERWIJDEREN
    }

    var jsonString = JsonSerializer.Serialize(CurrentProject, ProjectJsonOptions.Fast);
    await _js.InvokeVoidAsync("localStorage.setItem", "currentProject", jsonString);
}
```

---

## ?? Fase 4: Deserialisatie Update (LoadProjectFromLocalStorageAsync & RestoreNavigationProperties)

**VOOR (huidig):**
```csharp
public async Task LoadProjectFromLocalStorageAsync()
{
    var project = JsonSerializer.Deserialize<ProjectEntity>(...);
    
    // Materiaal reconstructie via MateriaalFactory
    var materiaalCache = new Dictionary<Guid, BaseMateriaal>();
    foreach (var assemblage in project.Assemblages.Where(a => a.MateriaalJson != null))
    {
        var materiaal = MateriaalFactory.Create(assemblage.MateriaalJson!.AsObject(), ...);
        // ...
    }
    
    project.InitAll();
    SetProject(project);
}
```

**NA (ref-by-ID):**
```csharp
public async Task LoadProjectFromLocalStorageAsync()
{
    var jsonString = await _js.InvokeAsync<string?>("localStorage.getItem", "currentProject");
    if (string.IsNullOrWhiteSpace(jsonString))
        return;

    var project = JsonSerializer.Deserialize<ProjectEntity>(jsonString, ProjectJsonOptions.Fast);
    if (project is null)
        return;

    // ? VEREENVOUDIGD: Geen MateriaalFactory meer nodig!
    // Materialen zitten in project.Materialen Dictionary
    // (ReferenceHandler doet zijn werk via JSON $ref/$id)
    
    project.InitAll();
    SetProject(project);  // ? Roept RestoreNavigationProperties aan
}
```

**RestoreNavigationProperties (vereenvoudigd):**
```csharp
private void RestoreNavigationProperties(ProjectEntity project)
{
    if (project?.Assemblages == null) return;

    // ? NIEUW: Herstellen via IDs
    foreach (var assemblage in project.Assemblages)
    {
        // ProjectInfo: dezelfde als project.ProjectInfo
        assemblage.ProjectInfo = project.ProjectInfo;
        
        // Materiaal: opzoeken in project.Materialen
        if (assemblage.MateriaalId.HasValue && 
            project.Materialen.TryGetValue(assemblage.MateriaalId.Value, out var materiaal))
        {
            assemblage.Materiaal = materiaal;  // ? HERSTELD via ID
        }
        
        // BordesEntity specifiek:
        if (assemblage is BordesEntity bordes)
        {
            // Trap1.AansluitendElement
            if (bordes.Trap1AansluitendElementGuid.HasValue)
            {
                var element = project.Assemblages.FirstOrDefault(
                    a => a.Guid == bordes.Trap1AansluitendElementGuid);
                if (element != null)
                {
                    bordes.Trap1.AansluitendElement = element;
                }
            }
            // Trap2 idem...
        }
    }

    // RestoreReferencesAfterDeserialization per assemblage
    foreach (var assemblage in project.Assemblages)
    {
        assemblage.RestoreReferencesAfterDeserialization(project.ProjectInfo);
    }
}
```

---

## ?? Fase 5: Specifieke Changes per File

### AssemblageEntity.cs
```
? Voeg toe:
   - [JsonInclude] Guid ProjectInfoId
   - [JsonInclude] Guid? MateriaalId
   
? Voeg JsonIgnore toe aan:
   - ProjectInfoEntity ProjectInfo
   - BaseMateriaal Materiaal
   - DekkingContext PlaatDekking (?)
   - BelastingenContext Belastingen (?)
```

### ProjectStateService.cs
```
? Update SaveProjectToLocalStorageAsync():
   - Stel ID's in (AssemblageMaterialId, etc.)
   - Verwijder MateriaalJson workaround
   
? Update LoadProjectFromLocalStorageAsync():
   - Verwijder MateriaalFactory logica
   - Eenvoudiger: JSON deserialisatie + SetProject
   
? Update RestoreNavigationProperties():
   - Herstellen via ID lookups (veel eenvoudiger!)
```

### ProjectEntity.cs
```
? Mogelijk: [JsonIgnore] op Materialen Dictionary
   (Want dit wordt opgebouwd vanuit Assemblages)
```

---

## ?? Implementatie Volgorde

### **Stap 1: Voorbereiding** (geen functie-changes)
- [ ] Voeg ID properties toe aan AssemblageEntity
- [ ] Test: Compileert nog?

### **Stap 2: Serialisatie** (JsonIgnore toevoegen)
- [ ] Voeg `[JsonIgnore]` toe aan complexe properties
- [ ] Test: `InspectSaveFile` - JSON moet veel kleiner zijn!

### **Stap 3: Save Flow** 
- [ ] Update SaveProjectToLocalStorageAsync() - ID's instellen

### **Stap 4: Load Flow**
- [ ] Update LoadProjectFromLocalStorageAsync() - MateriaalFactory verwijderen
- [ ] Update RestoreNavigationProperties() - via ID's herstellen
- [ ] Test: Load project ? alles intact?

### **Stap 5: Verificatie**
- [ ] Save nieuw project met Bordes
- [ ] Inspect JSON in `/File/inspect-save-file`
- [ ] Load ? alles werkt?

---

## ?? Expected JSON Change

### VOOR (huidig):
```json
{
  "Assemblages": [
    {
      "MateriaalJson": {
        "Id": "beton-1",
        "MateriaalType": "Beton",
        "Sterkteklasse": "C30/37",
        "Opmerking": "...",
        "Gebruiksomgeving": {...}
        // Hele BetonContext hier!
      },
      "ProjectInfo": {
        "Guid": "...",
        "Naam": "...",
        "Grondslagen": {...}
        // HELE ProjectInfo hier!
      },
      "Belastingen": {...},
      "PlaatDekking": {...},
      // etc. VEEL DATA!
    }
  ]
}
// GROOT!
```

### NA (ref-by-ID):
```json
{
  "ProjectInfo": {
    "Guid": "project-1",
    "Naam": "Mijn Project",
    "Grondslagen": {...}
    // ProjectInfo EENMAAL!
  },
  "Materialen": {
    "beton-1": {
      "Id": "beton-1",
      "MateriaalType": "Beton",
      "Sterkteklasse": "C30/37"
      // Materiaal EENMAAL!
    }
  },
  "Assemblages": [
    {
      "Guid": "bordes-1",
      "Naam": "BD-1",
      "MateriaalId": "beton-1",      // ? ENKEL ID!
      "ProjectInfoId": "project-1",  // ? ENKEL ID!
      // Geen MateriaalJson meer
      // Geen ProjectInfo duplicate
      // ~70% kleiner!
    }
  ]
}
```

---

## ? Checklist

- [ ] **AssemblageEntity**: ID properties toegevoegd
- [ ] **AssemblageEntity**: [JsonIgnore] op complexe properties
- [ ] **ProjectStateService**: SaveProjectToLocalStorageAsync() updated
- [ ] **ProjectStateService**: LoadProjectFromLocalStorageAsync() vereenvoudigd
- [ ] **ProjectStateService**: RestoreNavigationProperties() updated
- [ ] **Build**: Compileert zonder fouten
- [ ] **Test**: InspectSaveFile toont kleinere JSON
- [ ] **Test**: Load/Save cycle werkt
- [ ] **Documentatie**: Update leden naar nieuwe aanpak

---

## ?? Voordelen Na Implementatie

| Aspect | Voor | Na |
|--------|------|-----|
| **JSON Grootte** | 100% | ~20-30% |
| **Materiaal Duplicatie** | Ja (probleem) | Nee (opgelost!) |
| **Leesbaarheid** | Complex | Schoon |
| **Ref Handling** | MateriaalFactory | Native JSON Refs |
| **Maintenance** | Moeilijk (workarounds) | Makkelijk |

Zal ik direct beginnen met implementatie?
