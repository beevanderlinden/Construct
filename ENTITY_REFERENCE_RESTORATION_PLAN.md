# Entity Reference Restoration - Implementatieplan

## Doel
Een generiek referentie-systeem dat automatisch alle entity-referenties in je project.json herstelt na deserialisatie, zonder circular references of redundante data op te slaan.

---

## Architectuur

### 1. Basis: EntityReference<T> Helper Class

```csharp
// Location: Construct.Domain/Common/EntityReference.cs

using System;
using System.Text.Json.Serialization;

namespace Construct.Domain.Common
{
    /// <summary>
    /// Generieke helper voor het beheren van entity-referenties met ID-opslag en object-herstel.
    /// Serialiseert ALLEEN de Guid (EntityId), niet het object zelf (Entity).
    /// </summary>
    public abstract class EntityReference<T> where T : class
    {
        /// <summary>
        /// De Guid-referentie naar de entity. Dit wordt geserialiseerd.
        /// </summary>
        public Guid? EntityId { get; set; }

        /// <summary>
        /// Het werkelijke object. Dit wordt NIET geserialiseerd.
        /// Wordt ingesteld via Attach() of Restore().
        /// </summary>
        [JsonIgnore]
        public T? Entity { get; private set; }

        /// <summary>
        /// Koppelt een entity-object aan deze referentie.
        /// Gebruikt bij creatie/updates.
        /// </summary>
        public void Attach(T? entity)
        {
            Entity = entity;
            EntityId = entity != null ? GetEntityId(entity) : null;
        }

        /// <summary>
        /// Herstelt de entity-referentie via een resolver-functie.
        /// Gebruikt na JSON-deserialisatie.
        /// </summary>
        public void Restore(Func<Guid, T?> resolver)
        {
            if (EntityId.HasValue)
            {
                Entity = resolver(EntityId.Value);
            }
            else
            {
                Entity = null;
            }
        }

        /// <summary>
        /// Abstrakte methode om de Guid van een entity op te halen.
        /// Implementeer dit in afgeleide klassen.
        /// </summary>
        protected abstract Guid GetEntityId(T entity);
    }
}
```

---

## Implementatie in Project

### 2. Concrete EntityReference Implementaties

#### 2.1 AssemblageReference
```csharp
// Location: Construct.Domain/Common/AssemblageReference.cs

using Construct.Domain.Entities;

namespace Construct.Domain.Common
{
    public class AssemblageReference : EntityReference<AssemblageEntity>
    {
        protected override Guid GetEntityId(AssemblageEntity entity)
            => entity.Guid;
    }
}
```

#### 2.2 MateriaalReference
```csharp
// Location: Construct.Domain/Common/MateriaalReference.cs

using CommonLibrary.Models;

namespace Construct.Domain.Common
{
    public class MateriaalReference : EntityReference<BaseMateriaal>
    {
        protected override Guid GetEntityId(BaseMateriaal entity)
            => entity.Id;
    }
}
```

#### 2.3 StrookReference (optioneel)
```csharp
// Location: Construct.Domain/Common/StrookReference.cs

using Construct.Domain.Entities;

namespace Construct.Domain.Common
{
    public class StrookReference : EntityReference<StrookEntity>
    {
        protected override Guid GetEntityId(StrookEntity entity)
            => Guid.Parse(entity.Id);
    }
}
```

---

### 3. Gebruik in Entities

#### 3.1 AssemblageEntity - Materiaal-referentie

**Voor:** (Huidig)
```csharp
[JsonIgnore]
public BaseMateriaal Materiaal { get; set; }
public Guid? MateriaalId { get; set; }
```

**Na:** (Met EntityReference)
```csharp
private MateriaalReference _materiaalRef = new();

[JsonPropertyName("materiaalId")]
public Guid? MateriaalId 
{
    get => _materiaalRef.EntityId;
    set => _materiaalRef.EntityId = value;
}

[JsonIgnore]
public BaseMateriaal? Materiaal
{
    get => _materiaalRef.Entity;
    set => _materiaalRef.Attach(value);
}

private void RestoreMaterialReference(Func<Guid, BaseMateriaal?> materiaalResolver)
{
    _materiaalRef.Restore(materiaalResolver);
}
```

---

#### 3.2 BordesEntity - Trap-referentie

**Voor:** (Huidig - waarschijnlijk handmatig)
```csharp
public Guid? Trap1AansluitendElementGuid { get; set; }
public AssemblageEntity? Trap1AansluitendElement { get; set; }

public Guid? Trap2AansluitendElementGuid { get; set; }
public AssemblageEntity? Trap2AansluitendElement { get; set; }
```

**Na:** (Met EntityReference)
```csharp
private AssemblageReference _trap1Reference = new();
private AssemblageReference _trap2Reference = new();

[JsonPropertyName("trap1AansluitendElementGuid")]
public Guid? Trap1AansluitendElementGuid
{
    get => _trap1Reference.EntityId;
    set => _trap1Reference.EntityId = value;
}

[JsonIgnore]
public AssemblageEntity? Trap1AansluitendElement
{
    get => _trap1Reference.Entity;
    set => _trap1Reference.Attach(value);
}

[JsonPropertyName("trap2AansluitendElementGuid")]
public Guid? Trap2AansluitendElementGuid
{
    get => _trap2Reference.EntityId;
    set => _trap2Reference.EntityId = value;
}

[JsonIgnore]
public AssemblageEntity? Trap2AansluitendElement
{
    get => _trap2Reference.Entity;
    set => _trap2Reference.Attach(value);
}

private void RestoreAssemblageReferences(Func<Guid, AssemblageEntity?> assemblagResolver)
{
    _trap1Reference.Restore(assemblagResolver);
    _trap2Reference.Restore(assemblagResolver);
}
```

---

### 4. Restauratie in ProjectEntity

#### 4.1 Update RestoreReferencesAfterDeserialization()

```csharp
public virtual void RestoreReferencesAfterDeserialization(ProjectEntity project)
{
    if (project == null) return;

    // Reeds bestaande herstelstappen
    ProjectInfo = project.ProjectInfo;
    Belastingen ??= new(grondslagen: ProjectInfo.Grondslagen);
    Belastingen.Grondslagen = ProjectInfo.Grondslagen;

    if (Belastingen.BelastingCombinaties.Count == 0)
    {
        Belastingen.GenereerBelastingCombinaties(
            Belastingen,
            Belastingen.BelastingGevallen,
            Belastingen.CombinatiesTypes);
    }

    // ? NIEUW: Materiaal-referentie herstellen
    RestoreMaterialReference(
        guid => project.Materialen.TryGetValue(guid, out var mat) ? mat : null
    );
}
```

#### 4.2 Update BordesEntity.RestoreReferencesAfterDeserialization()

```csharp
public override void RestoreReferencesAfterDeserialization(ProjectEntity project)
{
    base.RestoreReferencesAfterDeserialization(project);

    // ? NIEUW: Trap-referenties herstellen
    RestoreAssemblageReferences(
        guid => project.Assemblages.FirstOrDefault(a => a.Guid == guid)
    );
}
```

#### 4.3 Centraal in ProjectStateService

```csharp
public async Task<ProjectEntity> LoadProjectAsync(string jsonContent)
{
    var project = JsonSerializer.Deserialize<ProjectEntity>(jsonContent, ProjectJsonOptions.Default);
    
    if (project != null)
    {
        // Stap 1: Alle referenties herstellen
        RestoreNavigationProperties(project);
    }

    return project;
}

private void RestoreNavigationProperties(ProjectEntity project)
{
    // Herstellen voor alle assemblages
    foreach (var assemblage in project.Assemblages)
    {
        // Virtual call - iedere subtype doet zijn eigen herstel
        assemblage.RestoreReferencesAfterDeserialization(project);
    }
}
```

---

## Implementatiestappen

### Stap 1: Create Base Class
- [ ] Maak `EntityReference<T>` in `Construct.Domain/Common/`
- [ ] Defineer abstract methode `GetEntityId()`

### Stap 2: Create Concrete Classes
- [ ] `AssemblageReference : EntityReference<AssemblageEntity>`
- [ ] `MateriaalReference : EntityReference<BaseMateriaal>`
- [ ] `StrookReference : EntityReference<StrookEntity>` (optioneel)

### Stap 3: Update AssemblageEntity
- [ ] Voeg `MateriaalReference` field toe
- [ ] Update `MateriaalId` property (getter/setter naar reference)
- [ ] Update `Materiaal` property (getter/setter naar reference)
- [ ] Voeg `RestoreMaterialReference()` methode toe
- [ ] Update `RestoreReferencesAfterDeserialization()` om herstel aan te roepen

### Stap 4: Update BordesEntity
- [ ] Voeg `AssemblageReference` fields toe voor trap1 en trap2
- [ ] Update guid/entity properties naar references
- [ ] Voeg `RestoreAssemblageReferences()` methode toe
- [ ] Override `RestoreReferencesAfterDeserialization()` voor trap-herstel

### Stap 5: Update ProjectStateService
- [ ] Update `RestoreNavigationProperties()` om alle assemblages te herstellen
- [ ] Test na deserialisatie dat alle referenties correct geladen zijn

---

## Voordelen van dit Systeem

? **Generiek**: Herbruikbaar voor alle entity-relaties  
? **Clean**: Gescheiden concerns - serialisatie vs. object-graph  
? **Veilig**: Type-safe via generics  
? **Testable**: Gemakkelijk mock resolvers doorgeven  
? **Backward compatible**: Bestaande JSON blijft werkend  
? **Centraal beheer**: Alle herstel in één plaats  

---

## Voorbeeld: Volledige Flow

```csharp
// JSON file bevat:
{
  "assemblages": [
    {
      "$type": "Ligger",
      "guid": "12345...",
      "materiaalId": "67890...",  // ? Alleen Guid opgeslagen
      ...
    }
  ],
  "materialen": {
    "67890...": {
      "$type": "BetonContext",
      "id": "67890...",
      ...
    }
  }
}

// Na deserialisatie:
var project = JsonSerializer.Deserialize<ProjectEntity>(json);

// RestoreNavigationProperties() aanroepen:
foreach (var assemblage in project.Assemblages)
{
    assemblage.RestoreReferencesAfterDeserialization(project);
    // Voor LiggerEntity: RestoreMaterialReference() roept
    //   materiaalRef.Restore(guid => project.Materialen.TryGetValue(guid, ...))
    //   aan - VOILÀ: Materiaal object is nu beschikbaar!
}

// Nu kun je gebruiken:
var ligger = project.Assemblages[0] as LiggerEntity;
Console.WriteLine(ligger.Materiaal.Naam); // ? Werkt!
```

---

## Notities

- Start met `AssemblageEntity.Materiaal` als proof-of-concept
- Breid daarna uit naar andere referenties (trappen, stroken, etc.)
- Overweeg caching in de resolver-functies voor performance
- Voeg logging toe in `Restore()` voor debugging
- Maak unit tests voor deserialisatie + herstel
