# Entity Materiaal Reference - Complete Audit

## ?? Status Check: Materiaal Assignments OVERAL

Je hebt **GELIJK** - we hebben inconsistenties! Hier is een complete audit van ALLE plaatsen waar Materiaal wordt ingesteld:

---

## ? CORRECT (MateriaalId wordt ingesteld)

### 1. **AddAssemblage.razor** (UI Component)
- ? `GetLiggerStaal()` - zet beide `Materiaal` + `MateriaalId`
- ? `GetLiggerBeton()` - zet beide `Materiaal` + `MateriaalId`
- ? `GetLiggerHout()` - zet beide `Materiaal` + `MateriaalId`
- ? `GetSteekTrap()` - zet beide `Materiaal` + `MateriaalId` (RECENTLY FIXED)
- ? `GetKolom()` - zet beide `Materiaal` + `MateriaalId` (RECENTLY FIXED)
- ? `case "bordes"` - zet beide `Materiaal` + `MateriaalId` (RECENTLY FIXED)

---

## ? PROBLEEM: MateriaalId NIET ingesteld

### 2. **SteekTrapEntity.cs Constructor** ??
```csharp
public SteekTrapEntity(ProjectInfoEntity projectInfo)
{
    ProjectInfo = projectInfo;
    Materiaal = new BetonContext();  // ? MateriaalId wordt NIET ingesteld!
    // ... rest
    Init(projectInfo);
}
```
**Problem**: Wanneer je programmatisch een SteekTrapEntity maakt met `new SteekTrapEntity(projectInfo)`, krijgt het een NEW BetonContext die NIET in project.Materialen zit!

### 3. **BordesEntity Constructor** ? CORRECT
```csharp
public BordesEntity()
{
    AssemblageType = AssemblageTypeEnum.BetonAssemblage;
    Materiaal = new BetonContext() { StaalKwaliteit = ... };
    // ? Maar: Init() wordt NIET geroepen ? ProjectInfo staat op wrong values
}
```

### 4. **LiggerEntity Constructor** ?? PROBLEEM
```csharp
public LiggerEntity()
{
    AssemblageType = AssemblageTypeEnum.StaalAssemblage;
    Materiaal = new StaalContext() { StaalKwaliteit = StaalKwaliteitEnum.S235};
    // ? MateriaalId wordt NIET ingesteld!
    // ? Dit is default constructor, geen projectInfo beschikbaar
}
```

---

## ?? Complete Checklist: Waar worden Assemblages gemaakt?

### **1. AddAssemblage.razor**
- ? OnMenuChange() ? CreateAssemblage() ? via GetXxx() methods
- ? MateriaalId ALTIJD ingesteld

### **2. SteekTrapEntity Constructor Chain**
```csharp
// Chain 1: Default constructor (PROBLEEM)
new SteekTrapEntity()  
  ? new BetonContext() ? NO MateriaalId!

// Chain 2: With ProjectInfo (BETTER)
new SteekTrapEntity(projectInfo)
  ? new BetonContext() ? NO MateriaalId!
  ? Init(projectInfo) ? should fix it?
```

### **3. BordesEntity Constructor**
```csharp
new BordesEntity()
  ? Trap1, Trap2 ? VerbindingAansluitendElement()
  ? Trap1.AansluitendElement = null initially
```

### **4. LiggerEntity Constructor**
```csharp
new LiggerEntity()
  ? this.Materiaal = new StaalContext()
  ? NO MateriaalId!
```

### **5. KolomEntity** - Check nodig!
```csharp
// Need to check constructor
```

---

## ?? Root Cause

**De default constructors van Entities stellen MateriaalId NIET in!**

Dit werkt fine in AddAssemblage.razor omdat we er handmatig `MateriaalId =` instellen.

Maar als je ergens ANDERS een entity maakt (bijvoorbeeld in een test, factory, of programmatisch), dan:
1. ? `Materiaal` wordt ingesteld (naar een temp context)
2. ? `MateriaalId` blijft null
3. ? Bij serialisatie ? Materiaal ontbreekt
4. ? Bij restore ? Materiaal = null

---

## ?? FIXES NEEDED

### **Fix 1: Update SteekTrapEntity Constructor**

```csharp
public SteekTrapEntity(ProjectInfoEntity projectInfo)
{
    ProjectInfo = projectInfo;
    
    // ? OLD:
    // Materiaal = new BetonContext();
    
    // ? NEW: Maak ID aan, zet MateriaalId
    var tempBeton = new BetonContext();
    tempBeton.Id = Guid.NewGuid();  // ? IMPORTANT!
    Materiaal = tempBeton;
    MateriaalId = tempBeton.Id;  // ? NEW!
    
    // ... rest
}
```

### **Fix 2: Update LiggerEntity Constructor**

```csharp
public LiggerEntity()
{
    AssemblageType = AssemblageTypeEnum.StaalAssemblage;
    
    var staal = new StaalContext() { StaalKwaliteit = StaalKwaliteitEnum.S235 };
    staal.Id = Guid.NewGuid();  // ? Generate ID
    
    Materiaal = staal;
    MateriaalId = staal.Id;  // ? NEW!
    
    // ... rest
}
```

### **Fix 3: Check BordesEntity Constructor**

```csharp
public BordesEntity()
{
    AssemblageType = AssemblageTypeEnum.BetonAssemblage;
    
    var beton = new BetonContext();
    beton.Id = Guid.NewGuid();
    
    Materiaal = beton;
    MateriaalId = beton.Id;  // ? NEW!
    
    // ... rest
}
```

### **Fix 4: Check KolomEntity Constructor**

Need to find and apply same fix.

---

## ?? When Does This Matter?

1. **AddAssemblage.razor** - ? SAFE (manual MateriaalId assignment)
2. **Programmatic creation** - ? RISKY (uses default constructor)
3. **Cloning/Copying entities** - ? RISKY
4. **Tests** - ? RISKY
5. **Serialization restore** - ? HANDLES via RestoreReferencesAfterDeserialization

---

## ?? Action Items

- [ ] Fix SteekTrapEntity constructor
- [ ] Fix LiggerEntity constructor
- [ ] Fix BordesEntity constructor
- [ ] Find and fix KolomEntity constructor
- [ ] Add unit tests to verify MateriaalId is always set
- [ ] Create factory methods if direct construction is too risky

---

## ?? Best Practice Going Forward

**RULE: Whenever you set `Materiaal`, ALWAYS set `MateriaalId` at the same time!**

```csharp
// ? WRONG:
Materiaal = new BetonContext();

// ? CORRECT:
var beton = new BetonContext();
beton.Id = Guid.NewGuid();
Materiaal = beton;
MateriaalId = beton.Id;
```

Or use a helper method:

```csharp
private void SetMaterialWithId<T>(T material) where T : BaseMateriaal
{
    if (material.Id == Guid.Empty)
        material.Id = Guid.NewGuid();
    
    Materiaal = material;
    MateriaalId = material.Id;
}
```

---

## Summary

? **AddAssemblage.razor** - FIXED
? **Entity Constructors** - NEED FIXES
? **RestoreReferencesAfterDeserialization** - WORKS FINE
?? **Programmatic creation** - AT RISK
