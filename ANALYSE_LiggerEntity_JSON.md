# ?? Analyse: LiggerEntity JSON Structuur

**Datum:** 2026-02-06  
**Project:** 8080-AI  
**Assemblage:** `Assemblages[1]` - Stalen ligger "L-1"  
**MateriaalType:** Staal (S235)

---

## ?? Huidige JSON Structuur

```json
{
  "$type": "Ligger",
  "Id": "9741f37c-ccb9-4c84-b03d-454c6e9d01be",
  "Merk": "L-1",
  "Naam": "ligger staal",
  "MateriaalId": "e5e7f76b-b3ae-47fb-b40e-e5a37d1feba8",
  "Beam": { ... },
  "EurcodeResultaten": [],
  "ProjectInfo": { ... },
  "Belastingen": { ... },
  "AfwerkingVlaklast": 0,
  "PlaatDekking": {
    "Onder": { "Beton": { ... } },
    "Boven": { "Beton": { ... } }
  },
  "EngineeringCategorie": "...",
  "IsPrefab": true,
  "AssemblageType": "StaalAssemblage",
  "Gebruiksklasse": "B_kantoorgebouwen",
  "Bijgewerkt": "2026-02-06T08:35:25...",
  "Breedte": 1200,
  "Lengte": 3000,
  "Hoogte": 2200
}
```

---

## ? **Probleem 1: Materiaal-specifieke Properties**

### **Situatie:**
Een **stalen ligger** bevat `PlaatDekking` met volledige `BetonDekkingContext` inclusief:
- ? `Beton.Betonsterkteklasse` = "C40_50"
- ? `Beton.CementKlasse` = "N"
- ? `BetonStaal.BetonStaalKwaliteit` = "B500A"
- ? Milieuklassen: ["X0"]
- ? IsPlaatGeometrie, IsKwaliteitsBeheersing

### **Waarom is dit fout?**
- ?? **Stalen liggers hebben geen betondekking**
- ?? **Betonsterkteklasse is niet relevant voor staal**
- ?? **Milieuklassen voor beton zijn niet van toepassing**
- ?? **BetonStaal (wapening) wordt niet gebruikt in stalen profielen**

### **Impact:**
```json
// Voorbeeld: PlaatDekking.Onder voor STAAL
{
  "Beton": {
    "BetonStaal": { "BetonStaalKwaliteit": "B500A" },
    "Betonsterkteklasse": "C40_50",
    "CementKlasse": "N"
  },
  "Milieuklassen": ["X0"]
}
```
**Dit is 100+ regels JSON die NIET relevant zijn voor staal!**

### **Oplossing:**
```csharp
// ? HUIDIG: AssemblageEntity (base class)
public class AssemblageEntity 
{
    public PlaatDekkingWrapper PlaatDekking { get; set; } // Voor ALLE types
}

// ? VOORSTEL: Polymorfisme
public abstract class AssemblageEntity 
{
    // Gemeenschappelijke properties
}

public class BetonAssemblageEntity : AssemblageEntity
{
    public PlaatDekkingWrapper PlaatDekking { get; set; } // ? Alleen voor beton
    public BetonContext Beton { get; set; }
}

public class StaalAssemblageEntity : AssemblageEntity
{
    public StaalContext Staal { get; set; } // ? Materiaal-specifiek
    // GEEN PlaatDekking
}

public class HoutAssemblageEntity : AssemblageEntity
{
    public HoutContext Hout { get; set; } // ? Materiaal-specifiek
    // GEEN PlaatDekking
}
```

**JSON resultaat (STAAL):**
```json
{
  "$type": "StaalAssemblage",
  "Staal": {
    "StaalKwaliteit": "S235",
    "E": 210000,
    "GammaM0": 1.0
  }
  // GEEN PlaatDekking ?
}
```

---

## ? **Probleem 2: Runtime/Berekende Data wordt Persistent**

### **Situatie:**
```json
{
  "EurcodeResultaten": [],  // ? Lege array wordt opgeslagen
  "Bijgewerkt": "2026-02-06T08:35:25.4951185+01:00"  // ? Runtime timestamp
}
```

Ook in nested objects:
```json
{
  "Beton": {
    "Id": "c47bc900-a953-42b5-bb8a-644cbbd7d0a8",
    "GewijzigdOp": "2026-02-06T07:35:25.4847255Z"  // ? Runtime data
  },
  "BetonStaal": {
    "Id": "4925eb51-4c58-4469-a7b3-f2ad1a869ba6",
    "GewijzigdOp": "2026-02-06T07:35:25.4831857Z"  // ? Runtime data
  }
}
```

### **Waarom is dit fout?**
- ?? **EurcodeResultaten** zijn berekend ? kunnen opnieuw worden gegenereerd
- ?? **GewijzigdOp timestamps** overal maken JSON onnodig groot
- ?? **Nested IDs** maken tracking ingewikkeld
- ?? Leidt tot **merge conflicts** bij Git (elke save = nieuwe timestamps)

### **Oplossing:**
```csharp
// ? VOORSTEL: [JsonIgnore] voor runtime properties
public class AssemblageEntity 
{
    [JsonIgnore]
    public List<BaseEurocodeContext> EurcodeResultaten { get; set; } // Runtime

    [JsonIgnore]
    public DateTime Bijgewerkt { get; set; } // Runtime
}

public class BetonContext 
{
    [JsonIgnore]
    public Guid Id { get; set; } // Runtime - generate on load

    [JsonIgnore]
    public DateTime GewijzigdOp { get; set; } // Runtime
}
```

**JSON resultaat:**
```json
{
  "$type": "Ligger",
  "Merk": "L-1",
  "MateriaalId": "e5e7f76b-b3ae-47fb-b40e-e5a37d1feba8"
  // GEEN EurcodeResultaten ?
  // GEEN Bijgewerkt ?
}
```

---

## ? **Probleem 3: Belastingen 3x Gedupliceerd**

### **Situatie:**
Dezelfde belastingen staan op **3 verschillende plekken**:

1?? **Project-niveau** ? `ProjectEntity.Belastingen`  
2?? **Assemblage-niveau** ? `AssemblageEntity.Belastingen`  
3?? **Beam-niveau** ? `Beam.LoadContext`

**Voorbeeld duplication:**
```json
// 1. ProjectEntity.Belastingen (niet in snippet maar bestaat)

// 2. AssemblageEntity.Belastingen
{
  "Belastingen": {
    "Grondslagen": { "Id": "f961fc06-...", "Gevolgklasse": "CC2" },
    "BelastingGevallen": [
      { "Nr": 1, "Omschrijving": "G", "Type": "Permanent" },
      { "Nr": 2, "Omschrijving": "q", "Type": "Veranderlijk" }
    ],
    "BelastingCombinaties": [ ... 5 combinaties ... ]
  }
}

// 3. Beam.LoadContext (EXACT DEZELFDE DATA!)
{
  "Beam": {
    "LoadContext": {
      "Grondslagen": { "Id": "90ffdbdc-...", "Gevolgklasse": "CC2" },
      "BelastingGevallen": [
        { "Nr": 1, "Omschrijving": "G", "Type": "Permanent" },
        { "Nr": 2, "Omschrijving": "q", "Type": "Veranderlijk" }
      ],
      "BelastingCombinaties": [ ... EXACT DEZELFDE 5 combinaties ... ]
    }
  }
}
```

**Impact:**
- ?? **~200 regels JSON per assemblage** ? 3x gedupliceerd = **600 regels**
- ?? **Inconsistencies:** Als BelastingGeval 1 wijzigt op project-niveau, moet je 3 plekken updaten
- ?? **Sync issues:** Referenties naar BelastingGevallen kunnen out-of-sync raken

### **Oplossing:**

#### **Optie A: Alleen op Project-niveau (RECOMMENDED)**
```csharp
public class ProjectEntity 
{
    public BelastingenContext Belastingen { get; set; } // ? Centraal
}

public class AssemblageEntity 
{
    // ? VERWIJDER: public BelastingenContext Belastingen { get; set; }
    
    [JsonIgnore]
    public BelastingenContext Belastingen => ProjectInfo.Belastingen; // ? Referentie
}

public class Beam 
{
    // ? VERWIJDER: public BelastingenContext LoadContext { get; set; }
}
```

**JSON resultaat:**
```json
{
  "ProjectInfo": { "Grondslagen": { ... } },
  // GEEN Belastingen op assemblage-niveau ?
  "Beam": {
    // GEEN LoadContext ?
    "Loads": [ ... ]  // Alleen load definitie
  }
}
```

#### **Optie B: Referentie via BelastingGevalId (Alternatief)**
```csharp
public class Beam 
{
    public List<BeamLoad> Loads { get; set; }
}

public class BeamLoad 
{
    public string Name { get; set; }  // "DL1g"
    public Guid BelastingGevalId { get; set; }  // ? Referentie naar BelastingGeval
    public double StartMagnitude { get; set; }
    public double EndMagnitude { get; set; }
}
```

**JSON resultaat:**
```json
{
  "Loads": [
    {
      "Name": "DL1g",
      "BelastingGevalId": "e1f048c7-ac1f-454f-b159-552e448d2d19",  // ? Referentie
      "StartMagnitude": -10,
      "EndMagnitude": -10
    }
  ]
}
```

---

## ? **Probleem 4: ProjectInfo Duplicatie**

### **Situatie:**
```json
{
  "ProjectInfo": {
    "Nummer": "8080-AI",
    "Naam": "Test materialen naar Assemblages",
    "Plaatsnaam": "DEN HAAG",
    "Grondslagen": {
      "NationaleBijlage": "NL",
      "OntwerpLevensduur": "Vijftig",
      "Gevolgklasse": "CC2",
      "Id": "f961fc06-0ea9-4ca1-8662-2a6d64ae1542",
      "GewijzigdOp": "2026-02-06T07:34:58.0735513Z"
    }
  }
}
```

Dit staat **in elke assemblage**, terwijl het al op `ProjectEntity` niveau bestaat!

### **Oplossing:**
```csharp
public class AssemblageEntity 
{
    // ? VERWIJDER: public ProjectInfoEntity ProjectInfo { get; set; }
    
    [JsonIgnore]
    public ProjectInfoEntity ProjectInfo { get; set; } // ? Runtime referentie (gezet in Init())
}
```

**JSON resultaat:**
```json
{
  "Id": "9741f37c-...",
  "Merk": "L-1",
  "MateriaalId": "e5e7f76b-..."
  // GEEN ProjectInfo ?
}
```

---

## ? **Probleem 5: Beam.PlaatWapening**

### **Situatie:**
```json
{
  "Beam": {
    "Schematisering": "VrijOpgelegd",
    "Length": 4,
    "Loads": [ ... ],
    "PlaatWapening": null  // ? Beton-specifiek in Beam (algemeen)
  }
}
```

### **Waarom is dit fout?**
- ?? **PlaatWapening** is alleen relevant voor **betonnen platen**
- ?? Zit nu in `Beam` (algemene class)
- ?? Voor stalen liggers is dit altijd `null`

### **Oplossing:**
```csharp
// ? HUIDIG
public class Beam 
{
    public PlaatWapeningWrapper? PlaatWapening { get; set; } // Voor ALLE beams
}

// ? VOORSTEL: Maak specifieke Beam types
public abstract class Beam 
{
    public double Length { get; set; }
    public List<BeamLoad> Loads { get; set; }
}

public class BetonBeam : Beam 
{
    public PlaatWapeningWrapper PlaatWapening { get; set; } // ? Alleen voor beton
}

public class StaalBeam : Beam 
{
    public StaalProfielEnum Profiel { get; set; } // ? Staal-specifiek
    // GEEN PlaatWapening
}
```

---

## ?? **Samenvatting Verbeteringen**

### **1. Materiaal-specifieke Inheritance**
```
AssemblageEntity (abstract)
??? BetonAssemblageEntity
?   ??? PlaatDekking ?
?   ??? PlaatWapening ?
??? StaalAssemblageEntity
?   ??? StaalProfiel ?
??? HoutAssemblageEntity
    ??? HoutKwaliteit ?
```

### **2. Runtime Properties [JsonIgnore]**
- ? `EurcodeResultaten` ? Runtime berekend
- ? `Bijgewerkt` timestamp ? Runtime
- ? Nested `Id` en `GewijzigdOp` ? Runtime

### **3. Belastingen Centraliseren**
- ? **Project-niveau:** `ProjectEntity.Belastingen`
- ? **Assemblage-niveau:** Verwijderen
- ? **Beam-niveau:** Verwijderen
- ? **Beam Loads:** Alleen load definitie + referentie naar BelastingGeval

### **4. ProjectInfo Referentie**
- ? `AssemblageEntity.ProjectInfo` persisteren
- ? `[JsonIgnore]` + Init() via parent referentie

### **5. Beam Type Hierarchie**
```
Beam (abstract)
??? BetonBeam (met PlaatWapening)
??? StaalBeam (zonder PlaatWapening)
??? HoutBeam (zonder PlaatWapening)
```

---

## ?? **Impact: File Size Reductie**

### **Voor (huidig):**
```json
{
  "Assemblages[1]": {
    // ~800 regels JSON
    "PlaatDekking": { ... 200 regels beton ... },
    "ProjectInfo": { ... 50 regels ... },
    "Belastingen": { ... 250 regels ... },
    "Beam.LoadContext": { ... 250 regels ... }
  }
}
```

### **Na (optimized):**
```json
{
  "Assemblages[1]": {
    // ~150 regels JSON ?
    "MateriaalId": "e5e7f76b-...",
    "Beam": {
      "Length": 4,
      "Loads": [...]  // ? Alleen load definitie
    }
  }
}
```

**Reductie:** **~650 regels** ? **~150 regels** = **81% kleiner** ??

---

## ? **Volgende Stappen**

1. **Refactor:** Maak `BetonAssemblageEntity`, `StaalAssemblageEntity`, `HoutAssemblageEntity`
2. **[JsonIgnore]:** Voeg toe aan alle runtime properties
3. **Belastingen:** Centraliseer op project-niveau
4. **ProjectInfo:** Maak referentie i.p.v. duplicatie
5. **Beam:** Maak type hierarchy (BetonBeam, StaalBeam)
6. **Test:** Sla bestaand project op ? vergelijk JSON voor/na

---

## ?? **Gerelateerde Issues**

- [x] ? MateriaalId naar PascalCase (gedaan)
- [ ] ? Materiaal-specifieke properties scheiden
- [ ] ? Runtime properties niet persisteren
- [ ] ? Belastingen centraliseren
- [ ] ? ProjectInfo referentie i.p.v. duplicatie

---

**Auteur:** GitHub Copilot  
**Datum:** 2026-02-06  
**Versie:** 1.0
