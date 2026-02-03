# Debug Feature: Inspect Save File (.cprj)

## ?? Purpose

A debug page to inspect and validate `.cprj` (gzip-compressed JSON) files for debugging serialization/deserialization issues.

## ?? Location

**Menu:** File ? Inspect Save File  
**URL:** `/File/inspect-save-file`

## ? Features

1. **Upload .cprj Files** - Select any `.cprj` save file
2. **Automatic Decompression** - Transparently decompresses gzip content
3. **JSON Pretty-Print** - Formats JSON for easy reading
4. **File Info** - Shows file size, type, and metadata
5. **Copy to Clipboard** - One-click copy of full JSON content

## ?? How to Use

1. Go to **File ? Inspect Save File** in the menu
2. Click **Upload** and select a `.cprj` file from your computer
3. The JSON content will automatically display in the right panel
4. Review the structure and content for debugging
5. Use **Copy** button to copy JSON for further analysis

## ?? What to Look For

### Check These Issues:

1. **Materiaal Deduplication**
   - Look for: `"Materiaal": { "Id": "..." }` appearing multiple times
   - Expected: Same Guid should be shared across assemblages
   - Location: Search for `MateriaalJson` in JSON

2. **BelastingCombinaties**
   - Look for: `"BelastingCombinaties": [...]`
   - Expected: Should contain combination data (if saved)
   - Known Issue: May be regenerated on load

3. **VerbindingAansluitendElement**
   - Look for: `"Trap1"`, `"Trap2"` in BordesEntity
   - Expected: Should have `Randafstand` and other properties
   - Check: `Trap1AansluitendElementGuid` (reference to connected element)

4. **ProjectInfo**
   - Look for: `"ProjectInfo": { "Naam": "...", "Grondslagen": {...} }`
   - Expected: Should be present and properly formed

### Example Healthy JSON Structure:
```json
{
  "ProjectInfo": {
    "Naam": "My Project",
    "Grondslagen": { ... }
  },
  "Assemblages": [
    {
      "$type": "Bordes",
      "Materiaal": {
        "Id": "abc-123",  // ? Same ID in multiple assemblages
        "MateriaalType": "Beton"
      },
      "Trap1": {
        "Randafstand": 100,
        "GebruikEigenOpgave": false
      }
    }
  ]
}
```

## ??? Technical Details

- **Framework:** Blazor Server-side
- **Rendering Mode:** InteractiveServer
- **Dependencies:** 
  - `System.IO.Compression` - GZip decompression
  - `System.Text.Json` - JSON parsing and formatting
- **Max File Size:** 10 MB

## ?? Implementation

**File:** `Construct.WebUI.Server/Components/Pages/File/InspectSaveFile.razor`

Features:
- Gzip decompression with error handling
- Pretty-print JSON formatting
- Client-side clipboard copy
- Responsive UI with file info display

---

**Build Status:** ? Successful
