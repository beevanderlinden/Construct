# Fix: RestoreNavigationProperties Unnecessarily Triggered on ProjectInfo Update

## ?? Probleem

`RestoreNavigationProperties` werd **afgevuurd** wanneer je op "ProjectInfo Bijwerken" klikt. Dit zou **enkel** na het laden van een JSON-bestand moeten gebeuren.

**Root Cause:**
```csharp
public void SetProject(ProjectEntity project)
{
    RestoreNavigationProperties(project);  // ?? Altijd aangeroepen!
    CurrentProject = project;
    OnProjectChanged?.Invoke();
}

// In NewFile.razor
private void BijwerkenProjectInfo()
{
    // ...
    ProjectState?.SetProject(ProjectEntity);  // ? Dit triggert RestoreNavigationProperties!
}
```

## ? Oplossing

Splitsing van `SetProject` in twee duidelijke methoden:

### 1. **SetProject** - Voor deserialisatie (roept RestoreNavigationProperties aan)
```csharp
public void SetProject(ProjectEntity project)
{
    // ? Herstel navigation properties na deserialisatie
    // BELANGRIJK: Dit wordt ENKEL aangeroepen voor NIEUWE projecten (na JSON-load)
    RestoreNavigationProperties(project);
    
    CurrentProject = project;
    OnProjectChanged?.Invoke();
}
```

**Gebruik:** Enkel na JSON-deserialisatie (LoadProjectFromLocalStorageAsync, FileService.LoadAsync, etc.)

### 2. **UpdateProject** - Voor state updates (geen RestoreNavigationProperties)
```csharp
public void UpdateProject(ProjectEntity project)
{
    // ? Geen RestoreNavigationProperties! Dit is enkel voor state updates
    CurrentProject = project;
    OnProjectChanged?.Invoke();
}
```

**Gebruik:** Bij eenvoudige updates zoals ProjectInfo wijzigen

---

## ?? Files Modified

### 1. ProjectStateService.cs
- ? Voegde `UpdateProject` methode toe
- ? Documentatie toegevoegd aan beide methoden
- ? SetProject blijft voor deserialisatie

### 2. IProjectStateService.cs (Interface)
- ? Voegde `UpdateProject` methode toe aan interface
- ? Documentatie voor beide methoden

### 3. NewFile.razor
- ? Veranderd `BijwerkenProjectInfo()` van `SetProject` naar `UpdateProject`

### 4. NewProjectFile.razor
- ? Veranderd `BijwerkenProjectInfo()` van `SetProject` naar `UpdateProject`

---

## ?? Flow Na Fix

### Bij Project Load (JSON Deserialisatie)
```
LoadProjectFromLocalStorageAsync()
  ?
project = JsonSerializer.Deserialize(...)
  ?
ProjectState.SetProject(project)  // ? SetProject gebruikt
  ?? RestoreNavigationProperties() aangeroepen ?
  ?? Materialen Dictionary hersteld ?
  ?? AansluitendElement references hersteld ?
  ?? RestoreReferencesAfterDeserialization() per assemblage ?
```

### Bij ProjectInfo Update
```
User klikt "Bijwerken"
  ?
BijwerkenProjectInfo()
  ?? ProjectEntity.ProjectInfo.Naam = value
  ?? ProjectEntity.ProjectInfo.Nummer = value
  ?? ProjectState.UpdateProject(project)  // ? UpdateProject gebruikt
    ?? CurrentProject = project
    ?? OnProjectChanged?.Invoke()
    ?? ? RestoreNavigationProperties NIET aangeroepen (efficiënt!)
```

---

## ?? Efficiency Impact

### VOOR (Inefficiënt)
- Bij ProjectInfo update: RestoreNavigationProperties werd 1x onnodig aangeroepen
- Loops door alle assemblages en relaties
- Onnodige verwerking

### NA (Efficiënt)
- Bij ProjectInfo update: Enkel state update, geen extra verwerking
- RestoreNavigationProperties alleen bij deserialisatie
- **Performance verbetering op eenvoudige updates**

---

## ? Build Status
? Build successful - Geen fouten of waarschuwingen

---

## ?? Related Contexts

Dit fix complementeert:
- **REFACTORING_PLAN_InitAfterDeserialization.md** - Centrale deserialisatie flow
- **SERIALIZATION_ANALYSIS.md** - Serialisatie problemen
- **FIX_MATERIAAL_REGISTRATION.md** - Materiaal management
