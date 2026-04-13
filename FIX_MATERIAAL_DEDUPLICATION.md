# Fix: Materiaal Deduplication na Deserialisatie

## ?? Probleem

Bij het opslaan en later openen van een project krijg je **plotseling 2 (of meer) Materialen** in `project.Materialen`, terwijl de Assemblages (bordes) ANDERE materialen hebben!

**Scenario:**
1. Nieuw project: `project.Materialen = {BetonContext1}`
2. Bordes: `bordes.Materiaal = BetonContext1` (dezelfde reference!) ?
3. **Save** (JSON)
4. **Load** (JSON)
5. Nu: `project.Materialen = {BetonContext1, BetonContext2}` ? **TWEE verschillende objekten!**
6. `bordes.Materiaal = BetonContext2` (ANDERE dan BetonContext1!)

## ?? Root Cause

### Problem 1: LocalStorage Load
```csharp
// LOAD (oude code)
foreach (var assemblage in project.Assemblages.Where(a=>a.MateriaalJson != null))
{
    // ? Dit maakt NIEUWE BetonContext objecten aan!
    assemblage.Materiaal = 
        MateriaalFactory.Create(assemblage.MateriaalJson!.AsObject(), ...);
}
```

Iedere assemblage krijgt een NIEUWE BetonContext, ook al hadden ze dezelfde ID!

### Problem 2: RestoreNavigationProperties
```csharp
// (oude code)
foreach (var assemblage in project.Assemblages)
{
    if (assemblage.Materiaal != null && assemblage.Materiaal.Id != Guid.Empty)
    {
        // ? Laaste assemblage overschrijft vorige!
        project.Materialen[assemblage.Materiaal.Id] = assemblage.Materiaal;
    }
}
```

Twee assemblages met DEZELFDE `Materiaal.Id`? De tweede overschrijft de eerste!

---

## ? Oplossing Geïmplementeerd

### 1. **Deduplicatie in LoadProjectFromLocalStorageAsync()**
```csharp
public async Task LoadProjectFromLocalStorageAsync()
{
    // ...
    
    // ? Polymorfe assemblages vullen MET deduplicatie
    var materiaalCache = new Dictionary<Guid, BaseMateriaal>();
    
    foreach (var assemblage in project.Assemblages.Where(a => a.MateriaalJson != null))
    {
        var materiaal = MateriaalFactory.Create(assemblage.MateriaalJson!.AsObject(), ...);
        
        // ? Controleer of we dit materiaal al hebben gemaakt (op basis van ID)
        if (materiaal.Id != Guid.Empty && materiaalCache.TryGetValue(materiaal.Id, out var cachedMateriaal))
        {
            // ? Hergebruik bestaand materiaal
            assemblage.Materiaal = cachedMateriaal;
        }
        else
        {
            // ? Voeg nieuw materiaal toe aan cache
            if (materiaal.Id != Guid.Empty)
            {
                materiaalCache[materiaal.Id] = materiaal;
            }
            assemblage.Materiaal = materiaal;
        }
    }
    
    project.InitAll();
    SetProject(project);
}
```

**Flow:**
- Eerste Bordes: MateriaalFactory maakt BetonContext1(Id=ABC) ? cache[ABC] = BetonContext1 ?
- Tweede Bordes: MateriaalFactory maakt BetonContext2(Id=ABC) ? HERGEBRUIK cache[ABC] = BetonContext1 ?
- Resultaat: Beide verwijzen naar HETZELFDE BetonContext1

### 2. **Deduplicatie in RestoreNavigationProperties()**
```csharp
private void RestoreNavigationProperties(ProjectEntity project)
{
    if (project?.Assemblages == null) return;

    // ? DEDUPLICATIE: Zorg dat assemblages met dezelfde Materiaal.Id
    // naar HETZELFDE object verwijzen (niet meerdere kopieën)
    var materiaalCache = new Dictionary<Guid, BaseMateriaal>();
    
    foreach (var assemblage in project.Assemblages)
    {
        if (assemblage.Materiaal != null && assemblage.Materiaal.Id != Guid.Empty)
        {
            if (materiaalCache.TryGetValue(assemblage.Materiaal.Id, out var cachedMateriaal))
            {
                // ? Vervang door bestaand materiaal in cache
                assemblage.Materiaal = cachedMateriaal;
            }
            else
            {
                // ? Voeg nieuw materiaal toe aan cache
                materiaalCache[assemblage.Materiaal.Id] = assemblage.Materiaal;
            }
        }
    }

    // Herstel project.Materialen Dictionary (NA deduplicatie!)
    project.Materialen.Clear();
    foreach (var assemblage in project.Assemblages)
    {
        if (assemblage.Materiaal != null && assemblage.Materiaal.Id != Guid.Empty)
        {
            project.Materialen[assemblage.Materiaal.Id] = assemblage.Materiaal;
        }
    }
    
    // ... rest van herstel (AansluitendeElementen, etc.)
}
```

**Flow:**
- Bordes1: `materiaal = BetonContext(Id=ABC)` ? Eerste keer ? cache[ABC] = BetonContext(Id=ABC)
- Bordes2: `materiaal = BetonContext(Id=ABC)` (ANDER object!) ? Vervang door cache[ABC]!
- Resultaat: `bordes1.Materiaal === bordes2.Materiaal` ?

---

## ?? Result

### NA fix:
1. Nieuw project: `project.Materialen = {BetonContext1}`
2. Save ? Load
3. `project.Materialen = {BetonContext1}` ? (DEZELFDE!)
4. `bordes.Materiaal === project.Materialen.Values.First()` ? (DEZELFDE object reference!)

### Data Integrity:
- ? Geen materiaal-duplicaten na load
- ? Alle assemblages verwijzen naar DEZELFDE BetonContext
- ? Deduplicatie op basis van `Materiaal.Id`
- ? Werkt voor zowel LocalStorage als File Load

---

## ?? Files Modified

- ? Construct.Application/Services/ProjectStateService.cs
  - Line ~8: Added `using CommonLibrary.Models;`
  - Line ~68-100: Deduplicatie in LoadProjectFromLocalStorageAsync()
  - Line ~120-170: Deduplicatie in RestoreNavigationProperties()

---

## ?? Materiaal Deduplicatie Strategy

```
Save:
  Bordes1.Materiaal = BetonContext(Id=ABC)
  Bordes2.Materiaal = BetonContext(Id=ABC)  ? SAME OBJECT
  ? Serialize
  {"MateriaalJson": "Guid:ABC, ...", "MateriaalJson": "Guid:ABC, ..."}

Load (met deduplicatie):
  Cache = {}
  Bordes1: Create BetonContext(Id=ABC) ? Cache[ABC] = new BetonContext1
  Bordes2: Create BetonContext(Id=ABC) ? HERGEBRUIK Cache[ABC]!
  ? Result
  Bordes1.Materiaal === Bordes2.Materiaal ? (SAME OBJECT REFERENCE)
```

---

## ? Build Status
? Build successful - Geen fouten of waarschuwingen.

---

## ?? Related Fixes

Dit complementeert:
- **FIX_PROJECTSTATE_UPDATE_FLOW.md** - State management split
- **FIX_MATERIAAL_REGISTRATION.md** - Materiaal creation
- **SERIALIZATION_ANALYSIS.md** - Serialisatie-issues
