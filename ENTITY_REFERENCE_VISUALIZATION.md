# Entity Reference Restoration - Visueel Voorbeeld

## Scenario: Een project met Grondslagen, Materialen, Bordes en Ligger

### Stap 1: JSON-bestand (Opgeslagen op schijf)

```json
{
  "projectInfo": {
    "nummer": "2024-001",
    "naam": "Kantoorgebouw Amsterdam",
    "plaatsnaam": "Amsterdam",
    "grondslagen": {
      "e": 210000,
      "fyk": 500,
      "fck": 30,
      ...
    }
  },
  
  "materialen": {
    "00000000-0000-0000-0000-000000000001": {
      "$type": "BetonContext",
      "id": "00000000-0000-0000-0000-000000000001",
      "naam": "C30/37",
      "betonsterkteklasse": "C30_37"
    },
    "00000000-0000-0000-0000-000000000002": {
      "$type": "StaalContext",
      "id": "00000000-0000-0000-0000-000000000002",
      "naam": "S235",
      "staalKwaliteit": "S235"
    }
  },
  
  "assemblages": [
    {
      "$type": "Bordes",
      "guid": "10000000-0000-0000-0000-000000000001",
      "naam": "bordes 1e verdieping",
      "merk": "BD-1",
      "materiaalId": "00000000-0000-0000-0000-000000000001",
      "trap1AansluitendElementGuid": "20000000-0000-0000-0000-000000000001",
      "trap2AansluitendElementGuid": "20000000-0000-0000-0000-000000000001"
    },
    {
      "$type": "Ligger",
      "guid": "20000000-0000-0000-0000-000000000001",
      "naam": "steektrap",
      "merk": "TR-1",
      "materiaalId": "00000000-0000-0000-0000-000000000001"
    },
    {
      "$type": "Ligger",
      "guid": "30000000-0000-0000-0000-000000000001",
      "naam": "ligger staal",
      "merk": "L-1",
      "materiaalId": "00000000-0000-0000-0000-000000000002"
    }
  ]
}
```

---

## Stap 2: Object-Graph VOOR Restoration

```
ProjectEntity
??? ProjectInfo
?   ??? Grondslagen
?       ??? E: 210000
?       ??? fck: 30
?
??? Materialen (Dictionary)
?   ??? [GUID-001] ? BetonContext (C30/37) ? In-memory
?   ??? [GUID-002] ? StaalContext (S235) ? In-memory
?
??? Assemblages (List)
    ?
    ??? Bordes (BD-1)
    ?   ??? Materiaal: null ? NIET HERSTELD
    ?   ??? MateriaalId: GUID-001 ? Opgeslagen in JSON
    ?   ??? Trap1AansluitendElement: null ? NIET HERSTELD
    ?   ??? Trap1AansluitendElementGuid: GUID-20...001 ? Opgeslagen in JSON
    ?   ??? Trap2AansluitendElement: null ? NIET HERSTELD
    ?   ??? Trap2AansluitendElementGuid: GUID-20...001 ? Opgeslagen in JSON
    ?
    ??? Ligger (TR-1 - Steektrap)
    ?   ??? Materiaal: null ? NIET HERSTELD
    ?   ??? MateriaalId: GUID-001 ? Opgeslagen in JSON
    ?
    ??? Ligger (L-1 - Stalen ligger)
        ??? Materiaal: null ? NIET HERSTELD
        ??? MateriaalId: GUID-002 ? Opgeslagen in JSON
```

---

## Stap 3: Restoration Process

### Phase 1: Deserialisatie

```csharp
// JSON wordt gedeserialiseerd
var json = File.ReadAllText("project.json");
var project = JsonSerializer.Deserialize<ProjectEntity>(json, ProjectJsonOptions.Default);

// ? Materialen zijn geladen (Dictionary)
// ? Assemblages zijn geladen (List)
// ? Object-referenties zijn NIET hersteld
```

---

### Phase 2: Reference Restoration (ProjectStateService)

```csharp
public void RestoreNavigationProperties(ProjectEntity project)
{
    // Itereer door alle assemblages
    foreach (var assemblage in project.Assemblages)
    {
        // Virtual call ? roept de juiste subtype-implementatie aan
        assemblage.RestoreReferencesAfterDeserialization(project);
    }
}
```

---

### Phase 3: Per-Assemblage Restoration

#### 3.1 Bordes Restoration

```csharp
public override void RestoreReferencesAfterDeserialization(ProjectEntity project)
{
    // Eerst base-klasse herstel
    base.RestoreReferencesAfterDeserialization(project);
    
    // Dan bordes-specifieke herstel
    RestoreAssemblageReferences(
        guid => project.Assemblages.FirstOrDefault(a => a.Guid == guid)
    );
}

// In AssemblageEntity (base):
protected void RestoreMaterialReference(Func<Guid, BaseMateriaal?> resolver)
{
    _materiaalRef.Restore(
        guid => project.Materialen.TryGetValue(guid, out var mat) ? mat : null
    );
}
```

**Flow voor Bordes (BD-1):**

```
1. RestoreReferencesAfterDeserialization(project) aanroepen
   ?
2. base.RestoreReferencesAfterDeserialization(project)
   ?
3. RestoreMaterialReference() aanroepen
   ?? _materiaalRef.Restore(guid => project.Materialen[GUID-001])
      ?? EntityId: GUID-001 (was al ingesteld uit JSON)
      ?? Entity: project.Materialen[GUID-001] ? HERSTELD!
   
4. RestoreAssemblageReferences() aanroepen
   ?? _trap1Reference.Restore(guid => project.Assemblages.First(a => a.Guid == GUID-20...001))
   ?  ?? Entity: <Ligger TR-1> ? HERSTELD!
   ?
   ?? _trap2Reference.Restore(guid => project.Assemblages.First(a => a.Guid == GUID-20...001))
      ?? Entity: <Ligger TR-1> ? HERSTELD! (SAME REFERENCE!)
```

#### 3.2 Ligger (TR-1) Restoration

```csharp
// LiggerEntity erft van AssemblageEntity
// Geen override nodig, base-implementatie volstaat

public override void RestoreReferencesAfterDeserialization(ProjectEntity project)
{
    base.RestoreReferencesAfterDeserialization(project);
    
    // RestoreMaterialReference() wordt in base aangeroepen:
    // _materiaalRef.Restore(guid => project.Materialen[GUID-001])
    //    ?? Entity: <BetonContext C30/37> ? HERSTELD!
}
```

#### 3.3 Ligger (L-1) Restoration

```csharp
// Idem als TR-1, maar andere MaterialId

public override void RestoreReferencesAfterDeserialization(ProjectEntity project)
{
    base.RestoreReferencesAfterDeserialization(project);
    
    // RestoreMaterialReference() wordt in base aangeroepen:
    // _materiaalRef.Restore(guid => project.Materialen[GUID-002])
    //    ?? Entity: <StaalContext S235> ? HERSTELD!
}
```

---

## Stap 4: Object-Graph NA Restoration

```
ProjectEntity
??? ProjectInfo
?   ??? Grondslagen
?       ??? E: 210000
?       ??? fck: 30
?
??? Materialen (Dictionary)
?   ??? [GUID-001] ? BetonContext (C30/37) ?
?   ??? [GUID-002] ? StaalContext (S235) ?
?
??? Assemblages (List)
    ?
    ??? Bordes (BD-1)
    ?   ??? _materiaalRef
    ?   ?   ??? EntityId: GUID-001
    ?   ?   ??? Entity: ??????????????
    ?   ?                            ?
    ?   ??? Materiaal: ?????????????????> BetonContext (C30/37) ? HERSTELD!
    ?   ?                            ?
    ?   ??? _trap1Reference          ?
    ?   ?   ??? EntityId: GUID-20...001
    ?   ?   ??? Entity: ????
    ?   ?                  ?
    ?   ??? Trap1AansluitendElement: ?????> Ligger (TR-1) ? HERSTELD!
    ?   ?                  ?
    ?   ??? _trap2Reference           ?
    ?   ?   ??? EntityId: GUID-20...001
    ?   ?   ??? Entity: ??????
    ?   ?
    ?   ??? Trap2AansluitendElement: ??> Ligger (TR-1) ? HERSTELD! (SAME)
    ?
    ??? Ligger (TR-1)
    ?   ??? _materiaalRef
    ?   ?   ??? EntityId: GUID-001
    ?   ?   ??? Entity: ?> BetonContext (C30/37) ? HERSTELD!
    ?   ?       (SHARED REFERENCE MET BORDES!)
    ?   ??? Materiaal: ??> BetonContext (C30/37) ?
    ?
    ??? Ligger (L-1)
        ??? _materiaalRef
        ?   ??? EntityId: GUID-002
        ?   ??? Entity: ?> StaalContext (S235) ? HERSTELD!
        ??? Materiaal: ??> StaalContext (S235) ?
```

---

## Stap 5: Runtime-gebruik (Na Restoration)

```csharp
// Code in je Blazor-component of service:
var project = await projectStateService.LoadProjectAsync(jsonContent);

// Nu zijn alle referenties beschikbaar! ??
var bordes = project.Assemblages.OfType<BordesEntity>().First();
var steektrap = bordes.Trap1AansluitendElement;  // ? NIET NULL!
var betonMat = steektrap.Materiaal;              // ? NIET NULL!

Console.WriteLine($"Steektrap materiaal: {betonMat.Naam}");  // Output: "C30/37"
Console.WriteLine($"Bordes materiaal: {bordes.Materiaal.Naam}"); // Output: "C30/37"
Console.WriteLine($"Same reference? {ReferenceEquals(betonMat, bordes.Materiaal)}"); 
// Output: "True" - beide wijzen naar hetzelfde BetonContext object!
```

---

## Diagram: Data Flow

```
???????????????????????????????????????????????????????????????????
?                    project.json (schijf)                        ?
?                                                                  ?
?  - Materialen: [GUID ? MaterialContext]                        ?
?  - Assemblages: [Assemb. met EntityId values]                  ?
???????????????????????????????????????????????????????????????????
                     ?
                     ? JsonSerializer.Deserialize()
                     
???????????????????????????????????????????????????????????????????
?           ProjectEntity (in-memory, INCOMPLETE)                 ?
?                                                                  ?
?  - Materialen: ? Loaded                                        ?
?  - Assemblages: ? Loaded (maar Entity-props zijn NULL)        ?
???????????????????????????????????????????????????????????????????
                     ?
                     ? ProjectStateService.RestoreNavigationProperties()
                     
     ????????????????????????????????????
     ? foreach (var a in Assemblages)   ?
     ?   a.RestoreReferencesAfter...()  ?
     ????????????????????????????????????
                    ?
        ?????????????????????????
        ?           ?           ?
    
    ????????????????????????????????????????????????????
    ? AssemblageEntity.RestoreReferences...()          ?
    ?   RestoreMaterialReference()                      ?
    ?   ? _materiaalRef.Restore(project.Materialen)   ?
    ????????????????????????????????????????????????????
    
    ????????????????????????????????????????????????????
    ? BordesEntity.RestoreReferences...()              ?
    ?   base.RestoreReferences...() (bovenstaande)    ?
    ?   RestoreAssemblageReferences()                  ?
    ?   ? _trap1Ref.Restore(project.Assemblages)      ?
    ?   ? _trap2Ref.Restore(project.Assemblages)      ?
    ????????????????????????????????????????????????????
                     ?
                     ?
                     
???????????????????????????????????????????????????????????????????
?           ProjectEntity (in-memory, COMPLETE)                   ?
?                                                                  ?
?  ? Alle Entity-properties hersteld                             ?
?  ? Alle referenties wijzen naar juiste objecten               ?
?  ? Shared references behouden (bv. materiaal tussen units)    ?
???????????????????????????????????????????????????????????????????
```

---

## Voordelen van dit Systeem: Visualisatie

### ? ZONDER EntityReference (Oud systeem)

```
JSON:
??? bordes
?   ??? materiaalId: GUID-001
?   ??? trap1AansluitendElementGuid: GUID-20...001
?   ??? trap1AansluitendElement: { ... } ? VOLLEDIGE KOPIE!
?
??? ligger (TR-1)
    ??? materiaalId: GUID-001
    ??? materiaal: { ... } ? VOLLEDIGE KOPIE!

Probleem: 
- ? BetonContext (GUID-001) DRIE KEER in JSON
- ? Circulaire referenties mogelijk
- ? Inconsistenties na aanpassingen
- ? Enorme JSON-bestanden
```

### ? MET EntityReference (Nieuw systeem)

```
JSON:
??? bordes
?   ??? materiaalId: GUID-001 ?
?   ??? trap1AansluitendElementGuid: GUID-20...001 ?
?
??? ligger (TR-1)
?   ??? materiaalId: GUID-001 ?
?
??? ligger (L-1)
    ??? materiaalId: GUID-002 ?

Voordelen:
- ? Slechts 1 kopie van BetonContext in JSON
- ? Geen circulaire referenties
- ? Kleinere JSON-bestanden
- ? Automatische herstel na load
- ? Shared references behouden
```

---

## Implementatie Checklist voor dit Voorbeeld

```
[ ] Stap 1: EntityReference<T> base class aanmaken
    ?? Construct.Domain/Common/EntityReference.cs

[ ] Stap 2: AssemblageReference aanmaken
    ?? Construct.Domain/Common/AssemblageReference.cs

[ ] Stap 3: MateriaalReference aanmaken
    ?? Construct.Domain/Common/MateriaalReference.cs

[ ] Stap 4: AssemblageEntity aanpassen
    ?? Voeg _materiaalRef field toe
    ?? Update MateriaalId property
    ?? Update Materiaal property
    ?? Voeg RestoreMaterialReference() methode toe

[ ] Stap 5: BordesEntity aanpassen
    ?? Voeg _trap1Reference en _trap2Reference fields toe
    ?? Update guid/entity properties
    ?? Voeg RestoreAssemblageReferences() methode toe
    ?? Override RestoreReferencesAfterDeserialization()

[ ] Stap 6: ProjectStateService aanpassen
    ?? Update LoadProjectAsync()
    ?? Voeg RestoreNavigationProperties() methode toe
    ?? Test deserialisatie + herstel

[ ] Stap 7: Unit Tests
    ?? Test EntityReference.Restore()
    ?? Test BordesEntity restauration
    ?? Test shared material references
```

---

## Test Scenario

```csharp
[Fact]
public async Task TestEntityReferenceRestoration()
{
    // Arrange
    var jsonContent = LoadJson("project-with-bordes-ligger.json");
    var projectStateService = new ProjectStateService();
    
    // Act
    var project = await projectStateService.LoadProjectAsync(jsonContent);
    
    // Assert
    var bordes = project.Assemblages.OfType<BordesEntity>().First();
    var steektrap = bordes.Trap1AansluitendElement;
    var ligger = project.Assemblages.OfType<LiggerEntity>().Last();
    
    // ? Trap-referentie hersteld
    Assert.NotNull(steektrap);
    Assert.Equal("TR-1", steektrap.Merk);
    
    // ? Materiaal-referentie hersteld
    Assert.NotNull(bordes.Materiaal);
    Assert.Equal("C30/37", bordes.Materiaal.Naam);
    
    // ? Shared reference behouden
    Assert.Same(steektrap.Materiaal, bordes.Materiaal);
    Assert.Same(ligger.Materiaal, steektrap.Materiaal); // Beide C30/37
}
```
