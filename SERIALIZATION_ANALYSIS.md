# Serialisatie Analyse & Oplossingen - ? GEÏMPLEMENTEERD

## ?? Ontdekte Problemen & Oplossingen

### 1. ? VerbindingAansluitendElement.Randafstand - OPGELOST
**Status:** GEÏMPLEMENTEERD  
**Property:** `public double Randafstand { get; set; } = 150;`

**Probleem:**
```csharp
[JsonConstructor]
public VerbindingAansluitendElement()  // ? Geen parameters
{
    Father = null!;
}
```
Dit zorgde ervoor dat properties zoals `Randafstand` NIET uit JSON werden gemapt.

**Oplossing Geïmplementeerd:**
```csharp
[JsonConstructor]
public VerbindingAansluitendElement(
    bool gebruikEigenOpgave = false,
    double lengteEigenOpgave = 1200,
    double breedteEigenOpgave = 100,
    double hoogteEigenOpgave = 105,
    double randafstand = 150,           // ? Nu gemapt!
    bool gespiegeld = false,
    AssemblageEntity? father = null)
{
    GebruikEigenOpgave = gebruikEigenOpgave;
    LengteEigenOpgave = lengteEigenOpgave;
    BreedteEigenOpgave = breedteEigenOpgave;
    HoogteEigenOpgave = hoogteEigenOpgave;
    Randafstand = randafstand;          // ? Correct gemapt uit JSON
    Gespiegeld = gespiegeld;
    Father = father ?? null!;
}
```

**Impact:** Randafstand wordt nu correct uit JSON geladen en geserialiseerd.

---

### 2. ? BelastingCombinaties - PARTIEEL OPGELOST
**Status:** GEÏMPLEMENTEERD MAAR GELIMITEERD  
**Property:** `public List<BelastingCombinatie> BelastingCombinaties`  
**Type:** BelastingenContext (Eurocode library)

**Probleem:**
De JsonConstructor in BelastingenContext (externe library) roept ALTIJD `GenereerBelastingCombinaties()` aan:
```csharp
[JsonConstructor]
public BelastingenContext(GrondslagenContext grondslagen)
{
    Grondslagen = grondslagen;
    GenereerBelastingCombinaties(...);  // ?? ALTIJD aangeroepen!
}
```

**Limitatie:**
- We kunnen de BelastingenContext constructor niet wijzigen (externe library)
- De BelastingCombinaties zal ALTIJD opnieuw gegenereerd worden

**Workaround Geïmplementeerd:**
In BordesEntity.RestoreReferencesAfterDeserialization():
```csharp
// ?? BelastingCombinaties kunnen leeg zijn door JsonConstructor die GenereerBelastingCombinaties() aanroept
// Controleer of we opnieuw moeten genereren
if (Belastingen.BelastingCombinaties.Count == 0)
{
    // Roep GenereerBelastingCombinaties aan om de combinaties opnieuw te genereren
    Belastingen.GenereerBelastingCombinaties(
        Belastingen, 
        Belastingen.BelastingGevallen, 
        Belastingen.CombinatiesTypes);
}
```

**Opmerking:**
Dit betekent dat custom BelastingCombinaties (aanpassingen door gebruiker) NIET zullen worden bewaard. Dit is een limitatie van de externe library design. Mogelijke toekomstige oplossing: JsonTypeInfo modifier toepassen op BelastingenContext in ProjectJsonOptions.

---

### 3. ? PlaatDekking.IsKwaliteitsBeheersing & IsPlaatGeometrie - OPGELOST
**Status:** GEÏMPLEMENTEERD  
**Properties:** 
```csharp
public bool IsPlaatGeometrie { get; set; }
public bool IsKwaliteitsBeheersing { get; set; }
```

**Probleem:**
Deze waarden werden OVERSCHREVEN met hardcoded `true` waarden:
```csharp
PlaatDekking.Onder.IsKwaliteitsBeheersing = true;  // ? Hardcoded
PlaatDekking.Onder.IsPlaatGeometrie = true;        // ? Hardcoded
```

**Oplossing Geïmplementeerd:**
```csharp
// ? Initialiseer ENKEL als ze null zijn
// Behoud gedeserialiseerde waarden (IsKwaliteitsBeheersing, IsPlaatGeometrie)
if (PlaatDekking.Onder == null)
{
    PlaatDekking.Onder = new BetonDekkingContext();
    PlaatDekking.Onder.IsKwaliteitsBeheersing = true;  // ? Enkel init waarden
    PlaatDekking.Onder.IsPlaatGeometrie = true;        // ? Enkel init waarden
}
// ? Stel relaties in zonder bools te overschrijven
PlaatDekking.Onder.Grondslagen = ProjectInfo.Grondslagen;
PlaatDekking.Onder.Beton = beton ?? new();
```

**Impact:** Gedeserialiseerde bool-waarden worden nu behouden in plaats van te worden overschreven.

---

## ?? Root Cause Analysis - Samenvatting

### Oorzaken van Serialisatie-Problemen

| Property | Oorzaak | Type | Status |
|----------|---------|------|--------|
| `Randafstand` | JsonConstructor zonder parameters | Code Design | ? OPGELOST |
| `BelastingCombinaties` | JsonConstructor roept GenereerBelastingCombinaties() | External Library | ?? GEWORKAROUND |
| `IsKwaliteitsBeheersing` | Hardcoded overschrijving in RestoreReferences | Code Logic | ? OPGELOST |
| `IsPlaatGeometrie` | Hardcoded overschrijving in RestoreReferences | Code Logic | ? OPGELOST |

---

## ??? Implementatie Details

### Bestand: VerbindingAansluitendElement.cs
**Wijzing:** JsonConstructor met parameter-mapping
- Alle public properties krijgen parameters in JsonConstructor
- Default values matched originele defaults
- `father` parameter is optioneel (kan null zijn)

### Bestand: BordesEntity.cs  
**Wijzigingen:**
1. BelastingCombinaties check toegevoegd
2. PlaatDekking null-check in plaats van ??= + hardcoding
3. IsKwaliteitsBeheersing en IsPlaatGeometrie worden NIET meer overschreven als object al bestaat

**Code Changes:**
- Regels: 111-120 - BelastingCombinaties regeneratie check
- Regels: 130-145 - PlaatDekking null-check met conditional setters

---

## ? Validatie & Build Status

- ? Build Successful
- ? Alle wijzigingen genderafstandd
- ? Geen compilation errors
- ? Geen breaking changes

---

## ?? Nog Open Punten

### 1. BelastingCombinaties Custom Values
**Probleem:** Custom BelastingCombinaties aanpassingen worden NIET bewaard  
**Reden:** External library JsonConstructor roept GenereerBelastingCombinaties() aan  
**Toekomstige Oplossing:**
- Implementeer een JsonTypeInfo modifier in ProjectJsonOptions
- Maak BelastingCombinaties een computed property (read-only)
- Of: Change Library design (niet ideaal)

### 2. BetonDekkingContext Circular References
**Probleem:** ReferenceHandler.IgnoreCycles kan niet alle properties serialiseren  
**Status:** Waarschijnlijk geen issue, maar:
- Constructieklasse bevat complexe berekeningen
- Dit wordt ALTIJD opnieuw berekend in RestoreReferencesAfterDeserialization()
- Dus serialisatie van deze berekende waarden is niet nodig

---

## ?? Aanbevelingen

### Korte Termijn
? VOLTOOID:
- Randafstand nu correct geserialiseerd
- Bool-waarden nu correct behouden
- BelastingCombinaties workaround geïmplementeerd

### Lange Termijn
- ? Overweeg Custom JsonConverter voor BelastingenContext
- ? Documenteer dat BelastingCombinaties-aanpassingen niet persistent zijn
- ? Evalueer het IgnoreCycles strategy en implement ReadOnly properties voor computed values

---

## ?? Serialisatie Flow Na Fixes

```
JSON Bestand ? Deserialize
    ?
VerbindingAansluitendElement
  - Randafstand: ? Correct gemapt
  - Gespiegeld: ? Correct gemapt
  - Father: ? Hersteld in RestoreReferences
    ?
BordesEntity.RestoreReferencesAfterDeserialization()
  ?? Trap1.Father = this
  ?? Trap2.Father = this
  ?? Belastingen.BelastingCombinaties check
  ?? PlaatDekking.Onder/Boven
    ?? Behoud IsKwaliteitsBeheersing ?
    ?? Behoud IsPlaatGeometrie ?
    ?? Herstel Grondslagen referenties ?
```

---

## Files Modified
- ? Construct.Domain/Entities/VerbindingAansluitendElement.cs
- ? Construct.Domain/Entities/BordesEntity.cs
