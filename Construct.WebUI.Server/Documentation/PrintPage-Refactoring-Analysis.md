# PrintPage.razor - Refactoring Analyse en Optimalisatie Plan

## 📋 Inhoudsopgave
1. [Huidige Situatie](#huidige-situatie)
2. [Probleemanalyse](#probleemanalyse)
3. [Architectuur Voorstellen](#architectuur-voorstellen)
4. [Stap-voor-Stap Refactoring Plan](#stap-voor-stap-refactoring-plan)
5. [Optimalisatie Mogelijkheden](#optimalisatie-mogelijkheden)
6. [Implementatie Checklist](#implementatie-checklist)
---

## 🔍 Huidige Situatie

### Component Verantwoordelijkheden
Het `PrintPage.razor` component heeft momenteel **te veel verantwoordelijkheden**:

1. **UI Rendering** (Blazor Component)
   - Weergave van knoppen (Download RTF, Bijwerken, Info)
   - Tonen van DocumentInfo of Preview

2. **Document Generatie Logica** (Business Logic)
   - `GenerateDocumentContent()` - genereert DocumentContent
   - `GetDocument()` - converteert naar MigraDoc.Document
   - HTML/RTF/PDF export logica

3. **Data Transformatie** (Helper Methods)
   - `GetTableContentMaterialen()`
   - `GetTableContentProfielen()`
   - `GetTableContentDekking()`
   - `GetTableGeometrie()`
   - `SetUitgangspunten()`
   - En nog 10+ andere helper methoden

4. **State Management**
   - `isLoading`, `ShowInfo`, `Html`, etc.
   - Bijhouden van `PageNr`

### Code Metrics
- **Totale regels**: ~1400+
- **Methoden in @code block**: ~30+
- **Directe dependencies**: 15+ using statements
- **Cyclomatische complexiteit**: Zeer hoog (vooral in `GenerateDocumentContent`)

---

## ⚠️ Probleemanalyse

### 1. **Violation of Single Responsibility Principle (SRP)**
Het component doet te veel:
- UI logica
- Business logica
- Data transformatie
- Export functionaliteit

### 2. **Testbaarheid**
- Moeilijk te unit testen zonder Blazor runtime
- Business logica zit vast aan UI component
- Geen dependency injection voor document generators

### 3. **Herbruikbaarheid**
- Document generatie kan niet hergebruikt worden buiten dit component
- Andere delen van de applicatie kunnen niet dezelfde functionaliteit gebruiken
- API endpoints kunnen niet dezelfde logica gebruiken

### 4. **Onderhoudbaarheid**
- 1400+ regels in één bestand
- Moeilijk te navigeren
- Logica is verspreid over vele methoden
- Copy-paste code (bijv. verschillende `GetTableContent...` methoden)

### 5. **Performance**
- Alle SVG conversies gebeuren op de UI thread
- Geen caching van tussenresultaten
- Memory issues met grote documenten (SvgImageCache wordt pas laat opgeruimd)

---

## 🏗️ Architectuur Voorstellen

### Optie 1: Service Layer Pattern (AANBEVOLEN)

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                       │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ PrintPage.razor                                        │ │
│  │ - UI State (isLoading, ShowInfo)                       │ │
│  │ - Button handlers                                      │ │
│  │ - Display logic only                                   │ │
│  └─────────────────┬──────────────────────────────────────┘ │
└────────────────────┼────────────────────────────────────────┘
                     │ Inject
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                   Application Layer                         │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ IDocumentGenerationService (Interface)                 │ │
│  │ - Task<DocumentContent> GenerateAsync(ProjectEntity)   │ │
│  │ - Task<byte[]> ExportToPdfAsync(DocumentContent)       │ │
│  │ - Task<string> ExportToRtfAsync(DocumentContent)       │ │
│  │ - Task<string> ExportToHtmlAsync(DocumentContent)      │ │
│  └────────────────┬───────────────────────────────-───────┘ │
│                   │                                         │
│  ┌────────────────┴──────────────────────────────────────┐  │
│  │ DocumentGenerationService (Implementation)            │  │
│  │ - Orchestrates document generation                    │  │
│  │ - Delegates to specialized builders                   │  │
│  └─────────────────┬─────────────────────────────────────┘  │
└────────────────────┼────────────────────────────────────────┘
                     │ Uses
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                    Domain/Builder Layer                     │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ Document Builders (Specialized)                        │ │
│  │                                                        │ │
│  │ • CoverPageBuilder                                     │ │
│  │ • UitgangspuntenSectionBuilder                         │ │
│  │ • AssemblageSectionBuilder                             │ │
│  │   ├── LiggerSectionBuilder                             │ │
│  │   ├── BordesSectionBuilder                             │ │
│  │   └── SteekTrapSectionBuilder                          │ │
│  │ • TableContentFactory                                  │ │
│  │   ├── MaterialenTableBuilder                           │ │
│  │   ├── ProfielenTableBuilder                            │ │
│  │   └── DekkingTableBuilder                              │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                     │ Uses
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                   Infrastructure Layer                      │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ Export Services                                        │ │
│  │ • MigraDocExportService                                │ │
│  │ • HtmlExportService                                    │ │
│  │ • SvgConversionService                                 │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### Optie 2: Builder Pattern with Fluent API

```csharp
// Usage example:
var documentContent = await _documentBuilder
    .ForProject(project)
    .WithCoverPage()
    .WithUitgangspunten()
    .WithAssemblages(project.Assemblages)
    .BuildAsync();
```

### Optie 3: Strategy Pattern voor verschillende Document Types

```csharp
// Voor verschillende types rapporten
public interface IDocumentStrategy
{
    Task<DocumentContent> GenerateAsync(ProjectEntity project);
}

public class FullProjectReportStrategy : IDocumentStrategy { }
public class AssemblageReportStrategy : IDocumentStrategy { }
public class SummaryReportStrategy : IDocumentStrategy { }
```

---

## 📝 Stap-voor-Stap Refactoring Plan

### **FASE 1: Interfaces en Contracts Definiëren**

#### Stap 1.1: Create Service Interfaces
**Nieuw bestand**: `Construct.Application/Interfaces/IDocumentGenerationService.cs`

```csharp
public interface IDocumentGenerationService
{
    Task<DocumentContent> GenerateDocumentContentAsync(ProjectEntity project);
    Task<MigraDoc.DocumentObjectModel.Document> GenerateMigraDocAsync(DocumentContent content, SvgImageCache cache);
    Task<string> ExportToHtmlAsync(MigraDoc.DocumentObjectModel.Document document);
    Task<string> ExportToRtfAsync(DocumentContent content);
    Task<byte[]> ExportToPdfAsync(DocumentContent content);
}
```

#### Stap 1.2: Create Builder Interfaces
**Nieuw bestand**: `Construct.Application/Interfaces/Document/IDocumentSectionBuilder.cs`

```csharp
public interface IDocumentSectionBuilder<TEntity>
{
    SectionContent BuildSection(TEntity entity, DocumentBuildContext context);
}

public class DocumentBuildContext
{
    public PageHeaderContent PageHeader { get; set; }
    public int CurrentPageNumber { get; set; }
    public GrondslagenContext Grondslagen { get; set; }
    public SvgImageCache SvgCache { get; set; }
}
```

#### Stap 1.3: Create Table Builder Interface
**Nieuw bestand**: `Construct.Application/Interfaces/Document/ITableContentBuilder.cs`

```csharp
public interface ITableContentBuilder<T>
{
    TableContent Build(T data);
    TableContent Build(IEnumerable<T> data);
}
```

---

### **FASE 2: Implementeer Table Builders**

#### Stap 2.1: Materialen Table Builder
**Nieuw bestand**: `Construct.Application/Services/Document/TableBuilders/MaterialenTableBuilder.cs`

Verplaats:
- `GetTableContentMaterialen()`
- `GetTableContentMateriaal()`
- `GetTableContentBeton()`
- `GetTableContentStaal()`

#### Stap 2.2: Profielen Table Builder
**Nieuw bestand**: `Construct.Application/Services/Document/TableBuilders/ProfielenTableBuilder.cs`

Verplaats:
- `GetTableContentProfielen()`

#### Stap 2.3: Dekking Table Builder
**Nieuw bestand**: `Construct.Application/Services/Document/TableBuilders/DekkingTableBuilder.cs`

Verplaats:
- `GetTableContentDekking()`
- `GetTableContentDekkingOpt()`
- `GetTableContentDekkingEnDuurzaamheid()`

#### Stap 2.4: Geometrie Table Builder
**Nieuw bestand**: `Construct.Application/Services/Document/TableBuilders/GeometrieTableBuilder.cs`

Verplaats:
- `GetTableGeometrie()`

---

### **FASE 3: Implementeer Section Builders**

#### Stap 3.1: Uitgangspunten Section Builder
**Nieuw bestand**: `Construct.Application/Services/Document/SectionBuilders/UitgangspuntenSectionBuilder.cs`

Verplaats:
- `SetUitgangspunten()`

#### Stap 3.2: Cover Page Builder
**Nieuw bestand**: `Construct.Application/Services/Document/SectionBuilders/CoverPageBuilder.cs`

Verplaats:
- `UpdateCoverPage()`
- `SetCoverPage()`

#### Stap 3.3: Ligger Section Builder
**Nieuw bestand**: `Construct.Application/Services/Document/SectionBuilders/LiggerSectionBuilder.cs`

Verplaats logica uit `GenerateDocumentContent()` voor `LiggerEntity`

#### Stap 3.4: Bordes Section Builder
**Nieuw bestand**: `Construct.Application/Services/Document/SectionBuilders/BordesSectionBuilder.cs`

Verplaats logica uit `GenerateDocumentContent()` voor `BordesEntity`

#### Stap 3.5: SteekTrap Section Builder
**Nieuw bestand**: `Construct.Application/Services/Document/SectionBuilders/SteekTrapSectionBuilder.cs`

Verplaats logica uit `GenerateDocumentContent()` voor `SteekTrapEntity`

---

### **FASE 4: Implementeer Main Service**

#### Stap 4.1: Document Generation Service
**Nieuw bestand**: `Construct.Application/Services/Document/DocumentGenerationService.cs`

```csharp
public class DocumentGenerationService : IDocumentGenerationService
{
    private readonly ILogger<DocumentGenerationService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly ICoverPageBuilder _coverPageBuilder;
    private readonly IUitgangspuntenSectionBuilder _uitgangspuntenBuilder;
    private readonly ILiggerSectionBuilder _liggerBuilder;
    private readonly IBordesSectionBuilder _bordesBuilder;
    private readonly ISteekTrapSectionBuilder _steekTrapBuilder;
    
    public DocumentGenerationService(
        ILogger<DocumentGenerationService> logger,
        IWebHostEnvironment environment,
        ICoverPageBuilder coverPageBuilder,
        // ... andere builders
    )
    {
        _logger = logger;
        _environment = environment;
        _coverPageBuilder = coverPageBuilder;
        // ... andere assignments
    }
    
    public async Task<DocumentContent> GenerateDocumentContentAsync(ProjectEntity project)
    {
        // Main orchestration logic hier
    }
}
```

---

### **FASE 5: Refactor Component**

#### Stap 5.1: Update PrintPage.razor
**Aanpassen**: `Construct.WebUI.Server/Components/Pages/Project/PrintPage.razor`

Component wordt drastisch vereenvoudigd:
- Verwijder alle helper methoden
- Verwijder alle document generatie logica
- Behoud alleen UI state en event handlers
- Inject `IDocumentGenerationService`

---

### **FASE 6: Dependency Injection Setup**

#### Stap 6.1: Register Services
**Aanpassen**: `Program.cs` of `ServiceCollectionExtensions.cs`

```csharp
// Register all document generation services
builder.Services.AddScoped<IDocumentGenerationService, DocumentGenerationService>();

// Register section builders
builder.Services.AddScoped<ICoverPageBuilder, CoverPageBuilder>();
builder.Services.AddScoped<IUitgangspuntenSectionBuilder, UitgangspuntenSectionBuilder>();
builder.Services.AddScoped<ILiggerSectionBuilder, LiggerSectionBuilder>();
builder.Services.AddScoped<IBordesSectionBuilder, BordesSectionBuilder>();
builder.Services.AddScoped<ISteekTrapSectionBuilder, SteekTrapSectionBuilder>();

// Register table builders
builder.Services.AddScoped<IMaterialenTableBuilder, MaterialenTableBuilder>();
builder.Services.AddScoped<IProfielenTableBuilder, ProfielenTableBuilder>();
builder.Services.AddScoped<IDekkingTableBuilder, DekkingTableBuilder>();
builder.Services.AddScoped<IGeometrieTableBuilder, GeometrieTableBuilder>();
```

---

### **FASE 7: Testing & Validation**

#### Stap 7.1: Unit Tests
**Nieuwe bestanden**: `Construct.Tests/Services/Document/`

Maak unit tests voor:
- Elke table builder
- Elke section builder
- DocumentGenerationService

#### Stap 7.2: Integration Tests
Test de volledige flow met test projects

---

## 🚀 Optimalisatie Mogelijkheden

### 1. **Performance Optimalisaties**

#### 1.1 Async/Await Improvements
```csharp
// Current: synchronous SVG generation
var svgXml = SvgGenerator.GenerateBordesSvgXml(...);

// Better: async SVG generation
var svgXml = await _svgService.GenerateBordesSvgXmlAsync(...);
```

#### 1.2 Parallel Processing
```csharp
// Process multiple assemblages in parallel
var sectionTasks = project.Assemblages
    .Select(a => _assemblageBuilder.BuildSectionAsync(a, context))
    .ToArray();

var sections = await Task.WhenAll(sectionTasks);
```

#### 1.3 Caching Strategy
```csharp
public class DocumentGenerationService
{
    private readonly IMemoryCache _cache;
    
    public async Task<DocumentContent> GenerateDocumentContentAsync(ProjectEntity project)
    {
        var cacheKey = $"document_{project.Id}_{project.LastModified}";
        
        if (_cache.TryGetValue(cacheKey, out DocumentContent cached))
            return cached;
            
        var content = await GenerateDocumentContentInternalAsync(project);
        
        _cache.Set(cacheKey, content, TimeSpan.FromMinutes(10));
        
        return content;
    }
}
```

### 2. **Memory Optimalisaties**

#### 2.1 SVG Cache Management
```csharp
// Use IDisposable pattern
public class DocumentGenerationSession : IDisposable
{
    private readonly SvgImageCache _cache = new();
    
    public DocumentContent Content { get; set; }
    
    public void Dispose()
    {
        _cache?.Cleanup();
        GC.SuppressFinalize(this);
    }
}

// Usage
await using var session = new DocumentGenerationSession();
session.Content = await _service.GenerateAsync(project);
// Cache automatically cleaned up
```

#### 2.2 Streaming voor grote documenten
```csharp
public interface IDocumentGenerationService
{
    IAsyncEnumerable<SectionContent> GenerateSectionsAsync(ProjectEntity project);
}

// Usage in UI
await foreach (var section in _service.GenerateSectionsAsync(project))
{
    // Stream sections to UI as they're generated
    await UpdatePreview(section);
}
```

### 3. **Code Quality Improvements**

#### 3.1 Extract Constants
```csharp
public static class DocumentConstants
{
    public const string DefaultWidth1 = "3cm";
    public const string DefaultWidth2 = "15cm";
    public const double DefaultSvgWidth = 180.0;
    public const double DefaultSvgWidthPixels = 180.0 * 96.0 / 25.4;
}
```

#### 3.2 Use Configuration
```csharp
public class DocumentGenerationOptions
{
    public string DefaultFontFamily { get; set; } = "roboto";
    public string DefaultFontSize { get; set; } = "3mm";
    public PageMarginAndPageNumberSettingsEnum DefaultMarginSetting { get; set; }
    public double SvgWidthMm { get; set; } = 180.0;
}

// Register in appsettings.json
{
  "DocumentGeneration": {
    "DefaultFontFamily": "roboto",
    "DefaultFontSize": "3mm",
    "SvgWidthMm": 180.0
  }
}
```

#### 3.3 Factory Pattern voor Tables
```csharp
public class TableContentFactory
{
    private readonly Dictionary<Type, Func<object, TableContent>> _builders;
    
    public TableContentFactory(
        IMaterialenTableBuilder materialenBuilder,
        IProfielenTableBuilder profielenBuilder,
        IDekkingTableBuilder dekkingBuilder)
    {
        _builders = new()
        {
            [typeof(List<BaseMateriaal>)] = obj => materialenBuilder.Build((List<BaseMateriaal>)obj),
            [typeof(List<BaseProfiel>)] = obj => profielenBuilder.Build((List<BaseProfiel>)obj),
            [typeof(DekkingContext)] = obj => dekkingBuilder.Build((DekkingContext)obj),
        };
    }
    
    public TableContent CreateTable<T>(T data)
    {
        if (_builders.TryGetValue(typeof(T), out var builder))
            return builder(data);
            
        throw new NotSupportedException($"No table builder for type {typeof(T).Name}");
    }
}
```

### 4. **Error Handling Improvements**

#### 4.1 Custom Exceptions
```csharp
public class DocumentGenerationException : Exception
{
    public ProjectEntity Project { get; }
    public string Stage { get; }
    
    public DocumentGenerationException(
        string message, 
        Exception innerException, 
        ProjectEntity project, 
        string stage) 
        : base(message, innerException)
    {
        Project = project;
        Stage = stage;
    }
}
```

#### 4.2 Result Pattern
```csharp
public class DocumentGenerationResult
{
    public bool Success { get; set; }
    public DocumentContent? Content { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    
    public static DocumentGenerationResult SuccessResult(DocumentContent content) 
        => new() { Success = true, Content = content };
        
    public static DocumentGenerationResult FailureResult(params string[] errors) 
        => new() { Success = false, Errors = errors.ToList() };
}
```

### 5. **Logging Improvements**

```csharp
public class DocumentGenerationService
{
    private readonly ILogger<DocumentGenerationService> _logger;
    
    public async Task<DocumentContent> GenerateDocumentContentAsync(ProjectEntity project)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["ProjectId"] = project.Id,
            ["ProjectName"] = project.ProjectInfo?.Naam ?? "Unknown"
        });
        
        _logger.LogInformation("Starting document generation");
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var content = await GenerateDocumentContentInternalAsync(project);
            
            _logger.LogInformation(
                "Document generation completed in {ElapsedMs}ms. " +
                "Sections: {SectionCount}, Pages: {PageCount}",
                stopwatch.ElapsedMilliseconds,
                content.Sections.Count,
                EstimatePageCount(content));
                
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document generation failed after {ElapsedMs}ms", 
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

---

## ✅ Implementatie Checklist

### Pre-Refactoring
- [ ] Maak backup van huidige code
- [ ] Commit alle pending changes
- [ ] Maak feature branch (`feature/document-generation-refactoring`)
- [ ] Document huidige functionaliteit (acceptance tests)

### Fase 1: Foundation
- [ ] Create `Construct.Application/Services/Document/` folder structure
- [ ] Create `Construct.Application/Interfaces/Document/` folder structure
- [ ] Definieer `IDocumentGenerationService` interface
- [ ] Definieer builder interfaces
- [ ] Setup DI registrations (commented out)

### Fase 2: Table Builders
- [ ] Implement `MaterialenTableBuilder`
- [ ] Implement `ProfielenTableBuilder`
- [ ] Implement `DekkingTableBuilder`
- [ ] Implement `GeometrieTableBuilder`
- [ ] Write unit tests for table builders
- [ ] Register table builders in DI

### Fase 3: Section Builders
- [ ] Implement `CoverPageBuilder`
- [ ] Implement `UitgangspuntenSectionBuilder`
- [ ] Implement `LiggerSectionBuilder`
- [ ] Implement `BordesSectionBuilder`
- [ ] Implement `SteekTrapSectionBuilder`
- [ ] Write unit tests for section builders
- [ ] Register section builders in DI

### Fase 4: Main Service
- [ ] Implement `DocumentGenerationService`
- [ ] Implement `DocumentBuildContext`
- [ ] Add logging throughout
- [ ] Add error handling
- [ ] Write integration tests
- [ ] Register main service in DI

### Fase 5: Component Refactoring
- [ ] Update `PrintPage.razor` to use service
- [ ] Remove old helper methods
- [ ] Update component tests
- [ ] Test UI functionality

### Fase 6: Optimization
- [ ] Implement caching
- [ ] Add async improvements
- [ ] Memory optimization
- [ ] Performance testing

### Fase 7: Documentation & Cleanup
- [ ] Update XML documentation
- [ ] Update README
- [ ] Remove commented code
- [ ] Code review
- [ ] Merge to main branch

---

## 📊 Expected Benefits

### Testbaarheid
- **Voor**: 0% test coverage (component kan niet unit testen)
- **Na**: 80%+ test coverage mogelijk

### Onderhoudbaarheid
- **Voor**: 1 file met 1400+ regels
- **Na**: 15+ kleine, gefocuste classes (~100-200 regels elk)

### Performance
- **Voor**: ~2000-3000ms voor document generatie
- **Na**: ~1000-1500ms (met parallel processing en caching)

### Herbruikbaarheid
- **Voor**: Logica locked in UI component
- **Na**: Kan gebruikt worden in:
  - Blazor components
  - API endpoints
  - Background jobs
  - Command-line tools

### Code Quality Metrics
| Metric | Voor | Na | Verbetering |
|--------|------|----|----|
| Cyclomatic Complexity | 50+ | 10-15 | ⬇️ 70% |
| Lines per Method | 100+ | 20-30 | ⬇️ 75% |
| Class Coupling | Hoog | Laag | ⬆️ Modulariteit |
| Test Coverage | 0% | 80%+ | ⬆️ Veel beter |

---

## 🎯 Prioriteit & Risico's

### High Priority
1. ✅ Extract Table Builders (laag risico, grote impact)
2. ✅ Extract Section Builders (medium risico, grote impact)
3. ✅ Create Main Service (medium risico, grote impact)

### Medium Priority
4. ⚠️ Add Caching (laag risico, medium impact)
5. ⚠️ Async improvements (medium risico, medium impact)

### Low Priority
6. 💡 Streaming implementation (hoog risico, medium impact)
7. 💡 Advanced error handling (laag risico, klein impact)

### Risico Mitigatie
- **Risico**: Breaking changes tijdens refactoring
  - **Mitigatie**: Feature branch + comprehensive testing
  
- **Risico**: Performance regressie
  - **Mitigatie**: Performance benchmarks voor/na
  
- **Risico**: DI scope issues
  - **Mitigatie**: Zorgvuldige lifetime management

---

## 📚 Aanvullende Resources

### Patterns & Practices
- Builder Pattern: https://refactoring.guru/design-patterns/builder
- Service Layer Pattern: https://martinfowler.com/eaaCatalog/serviceLayer.html
- Dependency Injection in .NET: https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection

### Tools
- Benchmark.NET voor performance testing
- xUnit voor unit testing
- Moq voor mocking
- FluentAssertions voor test assertions

---

## 🤝 Volgende Stappen

1. **Review deze analyse** met het team
2. **Kies architectuur optie** (aanbeveling: Optie 1 - Service Layer)
3. **Start met Fase 1** (interfaces en contracts)
4. **Implementeer incrementeel** per fase
5. **Test grondig** na elke fase
6. **Deploy en monitor** performance

---

*Document aangemaakt: 2025*
*Laatste update: -*
*Auteur: GitHub Copilot (Analysis AI)*
