# Fix: Materiaal Registration bij Nieuwe Assemblages

## ?? Probleem

Bij het toevoegen van een nieuw Bordes of Kolom aan een project ontbreekt het **Materiaal**. Dit betekent dat:

1. `bordes.Materiaal` is `null`
2. Het materiaal is NIET geregistreerd in `project.Materialen`
3. Dit zorgt voor inconsistenties bij serialisatie

**Expected Flow:**
```
User klikt "Bordes toevoegen"
  ?
Maak nieuwe BordesEntity aan
  ?? Controleer project.Materialen op bestaande BetonContext
  ?? Als leeg: maak nieuw BetonContext("C20/25")
  ?? Voeg toe aan project.Materialen
  ?? Voeg referentie toe aan bordes.Materiaal (DEZELFDE object!)
```

---

## ? Oplossing Geïmplementeerd

### Bestand: AddAssemblage.razor

#### 1. **GetOrAddBetonMaterial()** - Reeds aanwezig
```csharp
private BetonContext GetOrAddBetonMaterial()
{
    if (ProjectEntity?.Materialen == null)
        throw new InvalidOperationException("Project.Materialen is null");

    // Haal eerste beton materiaal op
    var betonMaterial = ProjectEntity.Materialen.Values
        .OfType<BetonContext>()
        .FirstOrDefault();

    // Als niet gevonden, maak en voeg standaard toe
    if (betonMaterial == null)
    {
        betonMaterial = new BetonContext("C20/25");
        ProjectEntity.Materialen.Add(betonMaterial.Id, betonMaterial);
    }

    return betonMaterial;
}
```

#### 2. **GetKolom()** - GEWIJZIGD
```csharp
// ? VOOR:
private KolomEntity GetKolom()
{
    KolomEntity kolom = new()
    {
        Guid = Guid.NewGuid(),
        ProjectInfo = ProjectEntity!.ProjectInfo,
        Naam = "kolom",
        Merk = "KL-1",
        // ? GEEN Materiaal!
    };
    // ...
}

// ? NA:
private KolomEntity GetKolom()
{
    KolomEntity kolom = new()
    {
        Guid = Guid.NewGuid(),
        ProjectInfo = ProjectEntity!.ProjectInfo,
        Naam = "kolom",
        Merk = "KL-1",
        Materiaal = GetOrAddBetonMaterial(),  // ? Voeg materiaal toe
    };
    // ...
}
```

#### 3. **case "bordes"** - GEWIJZIGD
```csharp
// ? VOOR:
case "bordes":
    BordesEntity bordes = new()
    {
        ProjectInfo = ProjectEntity.ProjectInfo,
        Guid = Guid.NewGuid(),
        Naam = "bordes",
        Merk = "BD-1",
        Breedte = 1300,
        Lengte = 2700,
        // ? GEEN Materiaal!
    };

// ? NA:
case "bordes":
    BordesEntity bordes = new()
    {
        ProjectInfo = ProjectEntity.ProjectInfo,
        Guid = Guid.NewGuid(),
        Naam = "bordes",
        Merk = "BD-1",
        Breedte = 1300,
        Lengte = 2700,
        Materiaal = GetOrAddBetonMaterial(),  // ? Voeg materiaal toe
    };
```

---

## ?? Result

Nu wordt er bij het toevoegen van een nieuw Bordes of Kolom:

1. ? **Automatisch een BetonContext aangemaakt** (of bestaande hergebruikt)
2. ? **Geregistreerd in project.Materialen** (key = Guid, value = BetonContext)
3. ? **Referentie ingesteld in bordes.Materiaal** (DEZELFDE object!)
4. ? **Consistent met andere assemblages** (LiggerEntity, SteekTrapEntity)

---

## ?? Materiaal Flow

```
project.Materialen
    ?? Guid: "11111..." ? BetonContext("C20/25")
    ?                    ?
    ?                    ? (same object reference)
    ?                    ?
bordes.Materiaal ?????????

kolom.Materiaal ??????????
```

Alle assemblages van hetzelfde type delen HETZELFDE materiaal object!

---

## ?? Files Modified

- ? Construct.WebUI.Server/Components/Pages/Project/AddAssemblage.razor
  - Line ~155: Voeg `Materiaal = GetOrAddBetonMaterial()` toe aan GetKolom()
  - Line ~295: Voeg `Materiaal = GetOrAddBetonMaterial()` toe aan case "bordes"

---

## ? Build Status
? Build successful - Geen fouten of waarschuwingen.
