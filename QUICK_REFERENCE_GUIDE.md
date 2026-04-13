# Reference Restoration - Quick Fix Summary

## ?? Je hebt geïmplementeerd:

### 1. **EntityReference<T>** System
- Generic base class voor all entity references
- Separates ID (serialisatie) from Entity (runtime)

### 2. **Concrete Reference Types**
- `MateriaalReference` - voor materialen
- `AssemblageReference` - voor trap-referenties
- `StrookReference` - optioneel voor toekomst

### 3. **Integration in Entities**
- `AssemblageEntity.Materiaal` - uses `MateriaalReference`
- `VerbindingAansluitendElement.AansluitendElement` - uses `AssemblageReference`
- `BordesEntity.Trap1/Trap2` - roept restore aan

### 4. **Central Orchestration**
- `ProjectStateService.RestoreNavigationProperties()` - calls restore on all assemblages
- `ProjectStateService.ValidateCurrentProjectReferences()` - validates all refs

### 5. **Diagnostics**
- `ReferenceValidationHelper` - validates & reports issues
- `ReferenceValidationDebug.razor` - visual component for UI
- Browser console logging - automatic on load

---

## ?? Debugging Workflow

### Problem: `Materiaal` is NULL

**Quick Debug Steps:**

1. **Check Browser Console** (F12)
   ```
   Look for: "Invalid Material References: X"
   If X > 0, see what's wrong
   ```

2. **Add Component to Page**
   ```razor
   <ReferenceValidationDebug />
   ```
   Shows live status of all references

3. **Check JSON File**
   ```json
   - Is "materiaalId" present in assemblage?
   - Does material exist in "materialen" dict?
   - Are GUIDs matching?
   ```

4. **Validate Programmatically**
   ```csharp
   var report = ProjectState.ValidateCurrentProjectReferences();
   if (!report.IsHealthy)
   {
       // See what's wrong in report.Issues
   }
   ```

---

## ?? Checklist: Waarom Materiaal NULL?

Controleer in volgorde:

- [ ] Is `SetProject()` aangeroepen na JSON-load?
- [ ] Roept `SetProject()` `RestoreNavigationProperties()` aan?
- [ ] Roept `RestoreNavigationProperties()` `.RestoreReferencesAfterDeserialization(project)` aan?
- [ ] Heeft assemblage `MateriaalId` (niet Guid.Empty)?
- [ ] Staat materiaal in `project.Materialen` dictionary?
- [ ] Matchen de GUIDs?
- [ ] Logt console errors?

---

## ?? Hoe te gebruiken in Development

### Add to your Project Page:

```razor
@using Construct.Domain.Common

<div class="debug-section">
    <ReferenceValidationDebug />
</div>

<YourAssemblageComponent Model="CurrentAssemblage" />
```

### In Development appsettings.json:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Construct.Domain": "Debug"
    }
  }
}
```

### View console output:
- Browser DevTools ? Console tab
- Shows validation report on load
- Shows any reference issues

---

## ? Happy Path Example

```
Load JSON ? ProjectStateService.SetProject()
  ?
RestoreNavigationProperties()
  ?
For each Assemblage: RestoreReferencesAfterDeserialization()
  ?? RestoreMaterialReference()
  ?  ?? _materiaalRef.Restore(project.Materialen)  ? Materiaal loaded!
  ?? (if Bordes) RestoreAssemblageReferences()
     ?? _trap1Ref.Restore(project.Assemblages)  ? Trap loaded!
  ?
ValidateProjectReferences()
  ?
Report: "? All references OK!"
Browser Console: "? All entity references restored successfully!"
```

---

## ?? Validation Report Fields

```
TotalAssemblages: int
TotalMaterials: int
ValidMaterialReferences: int
InvalidMaterialReferences: int
ValidAssemblageReferences: int
InvalidAssemblageReferences: int
Issues: List<ReferenceCheckResult>
IsHealthy: bool (true if no invalid refs)
```

---

## ?? Next Actions

1. **Test in Browser**
   - Load a project
   - Check console (F12)
   - Should see validation report

2. **Add Debug Component**
   - Add `<ReferenceValidationDebug />` to your page
   - Click "Validate References" button
   - See live diagnostics

3. **Fix Issues** (if any)
   - Check JSON structure
   - Verify MateriaalId assignment
   - Check that materials are in dictionary

4. **Commit & Push**
   - All reference restoration now automated
   - No manual linking needed
