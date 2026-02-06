# Reference Restoration Diagnostics Guide

## Waarom is `Materiaal` NULL?

Mogelijke oorzaken:

### 1. **MateriaalReference niet hersteld**
- ? `RestoreReferencesAfterDeserialization()` niet aangeroepen
- ? `RestoreMaterialReference()` niet aangeroepen
- ? **Controleer**: Roept `ProjectStateService.SetProject()` de methode aan?

### 2. **MateriaalId ontbreekt of is Guid.Empty**
```csharp
if (!assemblage.MateriaalId.HasValue || assemblage.MateriaalId == Guid.Empty)
{
    // Materiaal kan niet hersteld worden
}
```

### 3. **Materiaal niet in project.Materialen Dictionary**
- ? BetonContext/StaalContext/HoutContext bestaat niet in project
- ? Wrong Guid in MateriaalId
- ? **Fix**: Controleer JSON file - is het materiaal daar?

### 4. **Restore resolver retourneert NULL**
```csharp
// In RestoreMaterialReference:
_materiaalRef.Restore(
    guid => project.Materialen.TryGetValue(guid, out var mat) ? mat : null
    // Als deze NULL retourneert ? Materiaal wordt NULL
);
```

---

## Hoe debug je dit?

### **Stap 1: Check de Browser Console**
Bij projectload zie je:
```
? All entity references restored successfully!
```
of
```
?? Reference Validation Report
Invalid Material References: 3
  - LiggerEntity 'L-1'
    Type: Materiaal
    ID: 00000000-0000-0000-0000-000000000001
    Error: MateriaalId=00000000-0000-0000-0000-000000000001 maar Materiaal is NULL
```

### **Stap 2: Voeg ReferenceValidationDebug Component toe**
```razor
<ReferenceValidationDebug />
```
Dit geeft live visuele feedback in je Blazor UI.

### **Stap 3: Handmatig testen**
```csharp
// In je component code-behind
var report = ProjectState.ValidateCurrentProjectReferences();

if (!report.IsHealthy)
{
    foreach (var issue in report.Issues)
    {
        Console.Error.WriteLine($"{issue.EntityName}: {issue.ErrorMessage}");
    }
}
```

---

## Check de JSON File Direct

Open je `project.json` en controleer:

```json
{
  "assemblages": [
    {
      "$type": "Ligger",
      "id": "...",
      "materiaalId": "00000000-0000-0000-0000-000000000001",  // ? Zit dit hier?
      ...
    }
  ],
  "materialen": {
    "00000000-0000-0000-0000-000000000001": {  // ? Staat het materiaal hier?
      "$type": "BetonContext",
      "id": "00000000-0000-0000-0000-000000000001",
      ...
    }
  }
}
```

### Common Issues:

1. **MateriaalId present, maar waarde is Guid.Empty**
   ```json
   "materiaalId": "00000000-0000-0000-0000-000000000000"  // ? WRONG!
   ```
   ? **Fix**: MateriaalId moet een echte Guid zijn

2. **MateriaalId absent in JSON**
   ```json
   {
     "$type": "Ligger",
     "naam": "ligger 1"
     // materiaalId ontbreekt!
   }
   ```
   ? **Fix**: Check `SaveProjectToLocalStorageAsync()` - wordt MateriaalId ingesteld?

3. **Materiaal in JSON, maar niet in Materialen Dictionary**
   ```json
   "assemblages": [
     { "materiaalId": "GUID-001" }  // ? Kijkt naar 001
   ],
   "materialen": {
     "GUID-002": { ... }  // ? Maar 002 staat hier!
   }
   ```
   ? **Fix**: Guid mismatch in de code

---

## Debugging Checklist

```
Materiaal is NULL?

? Stap 1: Check MateriaalId in Assemblage
  - Roep in DevTools: `window.debugAssemblage = true`
  - Check console output

? Stap 2: Check project.Materialen Dictionary
  - Bevat het de Guid die in MateriaalId staat?
  
? Stap 3: Check RestoreReferencesAfterDeserialization
  - Wordt het aangeroepen?
  - Logt het fouten?

? Stap 4: Check ReferenceValidationDebug
  - Toont het NULL referenties aan?

? Stap 5: Check JSON file
  - Is materiaalId aanwezig?
  - Staat het materiaal in Materialen dict?
```

---

## Advanced: Console Logging toevoegen

In `ProjectStateService.RestoreNavigationProperties()`:

```csharp
private void RestoreNavigationProperties(ProjectEntity project)
{
    if (project?.Assemblages == null) return;

    foreach (var assemblage in project.Assemblages)
    {
        Console.WriteLine($"Restoring {assemblage.GetType().Name} '{assemblage.Merk}'");
        
        assemblage.RestoreReferencesAfterDeserialization(project);
        
        if (assemblage.Materiaal == null && assemblage.MateriaalId.HasValue)
        {
            Console.Error.WriteLine(
                $"  ? Materiaal NULL! MateriaalId={assemblage.MateriaalId}, " +
                $"In dict? {project.Materialen.ContainsKey(assemblage.MateriaalId.Value)}"
            );
        }
        else if (assemblage.Materiaal != null)
        {
            Console.WriteLine($"  ? Materiaal restored: {assemblage.Materiaal.Naam}");
        }
    }
}
```

---

## Test Code voor Component

```razor
@if (ShouldShowValidation)
{
    <div class="alert alert-info">
        <button @onclick="TestValidation">Run Validation</button>
        
        @if (LastValidation != null)
        {
            <pre>@ReferenceValidationHelper.GetSummary(LastValidation)</pre>
        }
    </div>
}

@code {
    private ReferenceValidationHelper.ValidationReport? LastValidation;
    private bool ShouldShowValidation = true;

    private void TestValidation()
    {
        if (ProjectState?.CurrentProject != null)
        {
            LastValidation = ReferenceValidationHelper.ValidateProjectReferences(
                ProjectState.CurrentProject
            );
        }
    }
}
```

---

## Waarschijnlijke Root Causes

### **Causa 1: MateriaalId niet ingesteld bij Serialisatie**
```csharp
// SaveProjectToLocalStorageAsync() moet MateriaalId instellen:
foreach (var assemblage in CurrentProject.Assemblages)
{
    assemblage.MateriaalId = assemblage.Materiaal?.Id;  // ? Dit moet VOOR serialisatie!
}
```

### **Causa 2: RestoreReferencesAfterDeserialization niet aangeroepen**
```csharp
// ProjectStateService.SetProject() moet dit aanroepen:
public void SetProject(ProjectEntity project)
{
    RestoreNavigationProperties(project);  // ? Dit MOET worden aangeroepen!
    CurrentProject = project;
}
```

### **Causa 3: Fout Guid type in AssemblageReference**
```csharp
// AssemblageReference.GetEntityId moet entity.Id gebruiken, niet entity.Guid!
protected override Guid GetEntityId(AssemblageEntity entity)
    => entity.Id;  // ? CORRECT
    // => entity.Guid;  // ? WRONG - AssemblageEntity heeft geen Guid property
```

### **Causa 4: JSON Deserialize ReferenceHandler**
```csharp
// ProjectJsonOptions moet ReferenceHandler.Ignore hebben:
var options = new JsonSerializerOptions
{
    ReferenceHandler = ReferenceHandler.IgnoreCycles,  // ? VEREIST!
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
};
```

---

## Next Steps

1. **Run Build** - controleer op compile errors
2. **Open Browser Console** - check validation output
3. **Voeg ReferenceValidationDebug toe** - zie live status
4. **Inspect Project.json** - check structure
5. **Enable Advanced Logging** - debug details
