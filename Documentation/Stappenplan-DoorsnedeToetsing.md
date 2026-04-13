# Stappenplan: Doorsnede Toetsing Component

## 📋 Inhoudsopgave
1. [Doel & Scope](#doel--scope)
2. [Gebruikersflow (Stappen)](#gebruikersflow-stappen)
3. [Domein & Architectuur](#domein--architectuur)
4. [Toetsen Matrix](#toetsen-matrix)
5. [Resultatenweergave & Tabellen](#resultatenweergave--tabellen)
6. [TableColumn Strategie](#tablecolumn-strategie)
7. [Component Structuur](#component-structuur)
8. [Copy/Paste Functionaliteit](#copypaste-functionaliteit)
9. [Implementatie Volgorde](#implementatie-volgorde)
10. [Open Punten & Beslissingen](#open-punten--beslissingen)

---

## 🎯 Doel & Scope

Een standalone Blazor-component voor het toetsen van een doorsnede aan de Eurocode. De gebruiker doorloopt een vaste stappenvolgorde: materiaaltype → materiaal → profiel → (wapening bij beton) → interne krachten → toetskeuze → resultaten.

**In scope**
- EC2 (beton): momentwapening (UGT), dwarskracht (UGT)
- EC3 (staal): buiging My, dwarskracht Vz, gecombineerde toets
- EC5 (hout): buiging My, dwarskracht Vz
- Tabelweergave van resultaten per toets
- Kopiëren van tabelinhoud (TSV, klaar voor Excel)

**Buiten scope (later)**
- EC3 stabiliteitstoetsen (KIP/KNIK) → apart stappenplan
- BGT/doorbuiging, scheurwijdte
- Normaalkracht / torsie combinaties
- Meerdere doorsneden in één berekening

---

## 🚶 Gebruikersflow (Stappen)

```
[1] Materiaaltype     → Beton | Hout | Staal
        ↓
[2] Materiaal         → bijv. C30/37, C24, S355
        ↓
[3] Profiel           → afmetingen / profielkeuze
        ↓
[4] Wapening *        → langswapening + beugels + dekking   (* alleen beton)
        ↓
[5] Interne krachten  → één of meer belastingscombinaties (naam, My, Vz, …)
        ↓
[6] Toetskeuze        → vink de gewenste Eurocode-toetsen aan
        ↓
[7] Resultaten        → tabel per toets, kopieerknop
```

De stappen worden getoond als een horizontale stepper of als verticale tabs (conform het huidige `DoorsneeDemoComponent` layout). De gebruiker kan altijd terug naar een vorige stap. Wijzigingen in een vroeger stap wissen de berekende resultaten.

---

## 🏗️ Domein & Architectuur

### Bestaande bouwstenen (hergebruiken)

| Klasse / Type | Project | Rol |
|---|---|---|
| `BaseEurocodeToets` | `CommonLibrary` | Abstracte basis voor EC3/EC5 toetsen |
| `EurocodeResultaat` | `CommonLibrary` | Retourtype van `BaseEurocodeToets.Check()` |
| `InternalForces` | `CommonLibrary` | Record met N, My, Mz, Vy, Vz, T |
| `TableColumnAttribute` | `CommonLibrary` | Attribuut voor tabelkolom-metadata |
| `StaalContext` | `Eurocode3` | Materiaal staal (Fy, GammaM0, …) |
| `HoutContext` | `Eurocode5` | Materiaal hout (fmd, fvd, …) |
| `BetonContext` | `Eurocode2` | Materiaal beton + wapeningsstaal |
| `BendingMyToets` | `Eurocode3` | EC3 buigtoets (al aanwezig) |
| `ShearVzCheck` (EC3) | `Eurocode3` | EC3 dwarskrachttoets (al aanwezig) |
| `ShearVzCheck` (EC5) | `Eurocode5` | EC5 dwarskrachttoets (al aanwezig) |
| `BendingResults` | `Eurocode2` | EC2 momentwapening context (al aanwezig, heeft `[TableColumn]`) |
| `DwarskrachtWapContext` | `Eurocode2` | EC2 dwarskracht context (al aanwezig) |
| `WapeningContext` | `Eurocode2` | Langswapening (tekst + As) |
| `BeugelWapeningContext` | `Eurocode2` | Beugels (Ø, s, Asw) |
| `BetonProfiel` | `BetonProfielen` | b × h doorsnede |
| `ProfielIH` | `StaalProfielen` | I/H-profiel geometrie + sectie-eigenschappen |
| `BaseProfiel` | `CommonLibrary` | Basis voor rechthoekige hout-doorsnede |
| `DoorsneeDemoMateriaalType` | `Construct.WebUI.Server` | Enum Beton / Hout / Staal |

### Nieuw te bouwen

| Wat | Waar | Toelichting |
|---|---|---|
| `ToetsSelectieModel` | `Construct.WebUI.Server` (of `CommonLibrary`) | Wraps een `BaseEurocodeToets` met een `bool Geselecteerd` flag |
| `DoorsnedeToetsContext` | `Construct.WebUI.Server` | Houdt alle invoer bijeen (materiaal, profiel, krachten, geselecteerde toetsen, resultaten) |
| `DoorsnedeToetsingComponent.razor` | `Construct.WebUI.Server` | Hoofd-component met stappenbeheer |
| `EurocodeToetsResultatenTable.razor` | `Construct.WebUI.Server` | Generieke tabel via `[TableColumn]` reflectie voor EC3/EC5 |
| `BetonToetsResultatenTabel.razor` | `Construct.WebUI.Server` | Tabel voor EC2 resultaten per toetstype |
| `[TableColumn]` uitbreiden op `EurocodeResultaat` | `CommonLibrary` | Kolommen voor staal/hout-resultaattabel |
| `[TableColumn]` uitbreiden op `DwarskrachtWapContext` | `Eurocode2` | Kolommen voor EC2 dwarskrachttabel |

---

## ✅ Toetsen Matrix

### EC3 – Staal

| ID | Klasse | Titel | Artikel | Formule | Relevant wanneer |
|----|--------|-------|---------|---------|-----------------|
| `EC3_My` | `BendingMyToets` | Buiging om y-as | 6.2.5 | (6.12/6.13) | `|My| > 0` |
| `EC3_Vz` | `ShearVzCheck` | Dwarskracht Vz | 6.2.6 | (6.17) | `|Vz| > 0` |
| `EC3_MV` | `CombinedMVCheck` *(nieuw)* | Buiging + dwarskracht gecombineerd | 6.2.8 | (6.29) | `|My| > 0 && |Vz| > 0.5·VRd` |

### EC5 – Hout

| ID | Klasse | Titel | Artikel | Formule | Relevant wanneer |
|----|--------|-------|---------|---------|-----------------|
| `EC5_My` | `BendingMyCheck` *(nieuw)* | Buiging om y-as | 6.1.6 | (6.11) | `|My| > 0` |
| `EC5_Vz` | `ShearVzCheck` | Dwarskracht Vz | 6.1.7 | (6.13) | `|Vz| > 0` |

### EC2 – Beton

EC2-toetsen werken niet via `BaseEurocodeToets.Check()` maar via hun eigen context-objecten. Ze worden afzonderlijk afgehandeld.

| ID | Context klasse | Titel | Relevant wanneer |
|----|----------------|-------|-----------------|
| `EC2_M` | `BendingResults` | Momentwapening (UGT) | `|My| > 0` |
| `EC2_V` | `DwarskrachtWapContext` | Dwarskrachtweerstand (UGT) | `|Vz| > 0` |

---

## 📊 Resultatenweergave & Tabellen

### Staal & Hout: generieke `EurocodeResultaat`-tabel

Eén gecombineerde tabel met één rij per toets × belastingscombinatie.  
Kolommen via `[TableColumn]` op `EurocodeResultaat`:

```csharp
// Toe te voegen aan EurocodeResultaat in CommonLibrary/Models/BaseEurocodeToets.cs
[TableColumn(Label = "Pos",     Symbol = "Pos.")]
public string Positie { get; init; } = "...";

[TableColumn(Label = "Norm",    Symbol = "Norm")]
public string Norm { get; init; } = "?";

[TableColumn(Label = "Artikel", Symbol = "Art.")]
public string Artikel { get; init; } = "";

[TableColumn(Label = "Formule", Symbol = "Form.")]
public string Formule { get; init; } = "";

[TableColumn(Label = "F_Ed",    Symbol = "<i>F<sub>Ed</sub></i>", StringFormat = "0.##", Unit = "")]
public double Waarde { get; init; }

[TableColumn(Label = "F_Rd",    Symbol = "<i>F<sub>Rd</sub></i>", StringFormat = "0.##", Unit = "")]
public double Toelaatbaar { get; init; }

[TableColumn(Label = "UC",      Symbol = "UC", StringFormat = "0.00")]
public double Benutting => Waarde / Toelaatbaar;

[TableColumn(Label = "σ / τ",   Symbol = "σ/τ (N/mm²)", StringFormat = "0.#", Unit = "N/mm²")]
public double StaalSpanning => Benutting * Fy;
```

Visueel:

| Pos | Norm | Artikel | Formule | F_Ed | F_Rd | UC | σ/τ (N/mm²) |
|-----|------|---------|---------|------|------|----|-------------|
| Steunpunt | EC3 | 6.2.5 | (6.12) | 80.0 kNm | 120.5 kNm | **0.66** ✅ | 157 |
| Steunpunt | EC3 | 6.2.6 | (6.17) | 100.0 kN | 180.0 kN | **0.56** ✅ | 133 |

De UC-cel krijgt kleurcodering: groen (≤ 0.90), oranje (0.90–1.00), rood (> 1.00).

---

### Beton: tabellen per toetstype

#### EC2 Momentwapening — `BendingResults`

Gebruik bestaande `[TableColumn]`-attributen op `BendingResults`. Minimaal te tonen kolommen:

| Pos | Zijde | M_Ed (kNm) | d (mm) | x (mm) | x/d | z (mm) | A_s,req (mm²) | A_s,prov (mm²) | Wapening | Opmerking |
|-----|-------|-----------|--------|--------|-----|--------|----------------|----------------|----------|-----------|

Zijde = "Boven" of "Onder" (bepaald door tekenconventie My).

De rijen: per belastingscombinatie × zijde → 2 rijen per kracht (boven + onder).

#### EC2 Dwarskrachtweerstand — `DwarskrachtWapContext`

Toe te voegen `[TableColumn]`-attributen op de relevante properties van `DwarskrachtWapContext`:

```csharp
// Uit te breiden in DwarskrachtWapContext.cs
[TableColumn(Symbol = "<i>V<sub>Ed</sub></i>",     Unit = "kN", StringFormat = "0.##")]
public double Ved { get; ... }

[TableColumn(Symbol = "<i>V<sub>Rd,c</sub></i>",   Unit = "kN", StringFormat = "0.##")]
public double DwarskrachtWeerstandBeton { get; ... }

[TableColumn(Symbol = "<i>V<sub>Rd,s</sub></i>",   Unit = "kN", StringFormat = "0.##")]
public double DwarskrachtWeerstandStaal { get; ... }

[TableColumn(Symbol = "<i>V<sub>Rd,max</sub></i>", Unit = "kN", StringFormat = "0.##")]
public double DwarskrachtWeerstandMax { get; ... }

[TableColumn(Symbol = "<i>A<sub>sw,ben</sub></i>", Unit = "mm²/m", StringFormat = "0")]
public double AswBenPerMeter { get; ... }

[TableColumn(Symbol = "<i>A<sub>sw,toe</sub></i>", Unit = "mm²/m", StringFormat = "0")]
public double AswToegepast { get; ... }

[TableColumn(Symbol = "UC", StringFormat = "0.00")]
public double UnityCheck => Ved / Math.Max(DwarskrachtWeerstandBeton, DwarskrachtWeerstandStaal);
```

Tabel:

| Pos | V_Ed (kN) | V_Rd,c | V_Rd,s | V_Rd,max | A_sw,ben | A_sw,toe | UC |
|-----|-----------|--------|--------|----------|----------|----------|----|

---

## 🏷️ TableColumn Strategie

### Aanpak

Het `EurocodeToetsResultatenTable<T>` component leest via reflectie de properties met `[TableColumn]`-attribuut en bouwt dynamisch de kolomkoppen en rijen op. Dit werkt identiek aan de bestaande `ResultsDetailPopup`.

```razor
@* EurocodeToetsResultatenTable.razor *@
@typeparam T
@inject IJSRuntime JS

<div class="toets-table-wrapper">
    <div class="table-toolbar">
        <span class="table-title">@Titel</span>
        <button class="btn-copy" @onclick="KopieerNaarKlembord" title="Kopieer naar klembord">📋</button>
    </div>
    <table class="toets-table">
        <thead>
            <tr>
                @foreach (var col in _columns)
                {
                    <th>@((MarkupString)col.Header)</th>
                }
            </tr>
        </thead>
        <tbody>
            @foreach (var row in Rijen ?? [])
            {
                <tr class="@GetRowClass(row)">
                    @foreach (var col in _columns)
                    {
                        <td class="@col.Alignment">@col.GetValue(row)</td>
                    }
                </tr>
            }
        </tbody>
    </table>
</div>
```

Parameters:
- `IReadOnlyList<T> Rijen`
- `string Titel`
- `bool ToonKopieerKnop = true`
- `Func<T, string>? RowCssClass` — voor UC-kleurcodering

### Kolom-definitie record (intern)

```csharp
private record ColumnDef(string Header, string Alignment, Func<T, string> GetValue);
```

Opgebouwd via:
```csharp
var props = typeof(T)
    .GetProperties()
    .Where(p => p.GetCustomAttribute<TableColumnAttribute>() is { Visible: not false })
    .OrderBy(p => p.GetCustomAttribute<TableColumnAttribute>()!.Order);
```

> **Opmerking:** `TableColumnAttribute` heeft momenteel geen `Order`-property. Toevoegen als `public int Order { get; set; } = 0` in `CommonLibrary`.

---

## 🧱 Component Structuur

```
Construct.WebUI.Server\Components\Shared\DoorsnedeToetsing\
├── DoorsnedeToetsingComponent.razor          ← hoofd-component (stappenbeheer)
├── DoorsnedeToetsingComponent.razor.css
│
├── Steps\
│   ├── StapMateriaalSelectie.razor           ← stap 2: materiaal (StaalKwaliteitEnum / Houtkwaliteit / BetonsterkteklasseEnum)
│   ├── StapProfielSelectie.razor             ← stap 3: profiel (koppelen aan bestaande KitProfielSelectie indien van toepassing)
│   ├── StapWapening.razor                    ← stap 4: wapening + dekking (beton only, hergebruik bestaande controls)
│   ├── StapInterneKrachten.razor             ← stap 5: krachtenlijst (naam, My, Vz, N, …)
│   └── StapToetskeuze.razor                  ← stap 6: checkbox per toets + relevantiebadge
│
├── Resultaten\
│   ├── EurocodeToetsResultatenTable.razor    ← generiek [TableColumn]-tabel voor EC3/EC5
│   ├── BetonMomentResultaten.razor           ← EC2 BendingResults-tabel
│   └── BetonDwarskrachtResultaten.razor      ← EC2 DwarskrachtWapContext-tabel
│
└── Models\
    ├── ToetsSelectieModel.cs                 ← wraps BaseEurocodeToets + bool Geselecteerd + bool IsRelevant
    └── DoorsnedeToetsContext.cs              ← centrale state (materiaal, profiel, krachten, resultaten)
```

### `DoorsnedeToetsContext`

```csharp
public class DoorsnedeToetsContext
{
    public DoorsneeDemoMateriaalType MateriaalType { get; set; }

    // Stap 2 – Materiaal
    public StaalContext?  StaalMateriaal  { get; set; }
    public HoutContext?   HoutMateriaal   { get; set; }
    public BetonContext?  BetonMateriaal  { get; set; }

    // Stap 3 – Profiel
    public IProfiel?      Profiel         { get; set; }   // ProfielIH / BetonProfiel / BaseProfiel

    // Stap 4 – Wapening (beton)
    public WapeningContext?      WapeningBoven  { get; set; }
    public WapeningContext?      WapeningOnder  { get; set; }
    public BeugelWapeningContext? Beugels       { get; set; }

    // Stap 5 – Krachten
    public List<KrachtEntry> Krachten { get; set; } = [];

    // Stap 6 – Toetsen
    public List<ToetsSelectieModel> BeschikbareToetsen { get; set; } = [];

    // Stap 7 – Resultaten
    public List<EurocodeResultaat>      StaalResultaten  { get; set; } = [];
    public List<BendingResults>         BetonMoment      { get; set; } = [];
    public List<DwarskrachtWapContext>  BetonDwarskracht { get; set; } = [];

    public IMateriaal? GetMateriaal() => MateriaalType switch
    {
        DoorsneeDemoMateriaalType.Staal => StaalMateriaal,
        DoorsneeDemoMateriaalType.Hout  => HoutMateriaal,
        DoorsneeDemoMateriaalType.Beton => BetonMateriaal,
        _                               => null
    };
}
```

### `ToetsSelectieModel`

```csharp
public class ToetsSelectieModel
{
    public BaseEurocodeToets Toets        { get; init; } = null!;
    public bool              Geselecteerd { get; set; }  = true;
    public bool              IsRelevant   { get; set; }  = true;   // bijgewerkt na invullen krachten

    public string Titel   => Toets.Titel;
    public string Norm    => Toets.Norm;
    public string Artikel => Toets.Artikel;

    /// <summary>Werkt IsRelevant bij voor alle krachten in de lijst.</summary>
    public void UpdateRelevantie(IEnumerable<InternalForces> krachten)
        => IsRelevant = krachten.Any(Toets.IsRelevant);
}
```

---

## 📋 Copy/Paste Functionaliteit

### Formaat

Tab-separated values (TSV) zodat plakken in Excel direct een tabel geeft.

```
Pos\tNorm\tArtikel\tFormule\tF_Ed\tF_Rd\tUC\tσ/τ (N/mm²)\n
Steunpunt\tEC3\t6.2.5\t(6.12)\t80.0\t120.5\t0.66\t157\n
```

### Implementatie in `EurocodeToetsResultatenTable`

```csharp
private async Task KopieerNaarKlembord()
{
    var sb = new StringBuilder();
    // Header
    sb.AppendLine(string.Join("\t", _columns.Select(c => c.PlainHeader)));
    // Rijen
    foreach (var row in Rijen ?? [])
        sb.AppendLine(string.Join("\t", _columns.Select(c => c.GetPlainValue(row))));

    await JS.InvokeVoidAsync("navigator.clipboard.writeText", sb.ToString());
}
```

Een `PlainHeader` (zonder HTML) en `GetPlainValue` (numeriek, geen eenheidssymbolen) worden apart bijgehouden naast de HTML-variant.

---

## 📐 Stap 6 – Toetskeuze UI

```
┌─────────────────────────────────────────────────────────────┐
│  Toetsen selectie                                           │
│                                                             │
│  ☑  EC3 §6.2.5  Buiging My          [relevant: My = 80 kNm]│
│  ☑  EC3 §6.2.6  Dwarskracht Vz      [relevant: Vz = 100 kN]│
│  ☐  EC3 §6.2.8  My + Vz gecombineerd [niet relevant: Vz ≤ 0.5·VRd]│
│                                                             │
│  [niet-relevante toetsen tonen]  [Bereken →]                  │
└─────────────────────────────────────────────────────────────┘
```

- Toetsen die niet relevant zijn (via `IsRelevant`) worden grijs weergegeven met een badge "Niet van toepassing".
- De gebruiker kan ze alsnog handmatig aanvinken.
- De knop **Bereken** voert alle aangevinkte toetsen uit voor alle krachten en vult de resultaten.

---

## 🗂️ Implementatie Volgorde

### Fase 1 – Infrastructuur (CommonLibrary / Eurocode libs)

- [ ] **1.1** `TableColumnAttribute`: voeg `Order` property toe  
- [ ] **1.2** `EurocodeResultaat`: voeg `[TableColumn]` attributen toe op `Positie`, `Norm`, `Artikel`, `Formule`, `Waarde`, `Toelaatbaar`, `Benutting`, `StaalSpanning`  
- [ ] **1.3** `DwarskrachtWapContext`: voeg `[TableColumn]` attributen toe op `Ved`, `DwarskrachtWeerstandBeton`, `DwarskrachtWeerstandStaal`, `DwarskrachtWeerstandMax`, `AswBenPerMeter`, `AswToegepast` + voeg `UnityCheck`-property toe  
- [ ] **1.4** EC5 `BendingMyCheck` klasse aanmaken (analoog aan EC3 `BendingMyToets`)

### Fase 2 – Modellen (Construct.WebUI.Server)

- [ ] **2.1** `ToetsSelectieModel.cs` aanmaken  
- [ ] **2.2** `DoorsnedeToetsContext.cs` aanmaken  
- [ ] **2.3** `KrachtEntry` verplaatsen van `DoorsneeDemoComponent` naar gedeeld model (of duplicaat accepteren)

### Fase 3 – Generieke tabelcomponent

- [ ] **3.1** `EurocodeToetsResultatenTable.razor` bouwen  
  - Reflectie op `[TableColumn]`  
  - UC-kleurcodering  
  - Kopieerknop (TSV naar klembord)

### Fase 4 – Beton-specifieke tabelcomponenten

- [ ] **4.1** `BetonMomentResultaten.razor` — gebruik bestaande `[TableColumn]` op `BendingResults`  
- [ ] **4.2** `BetonDwarskrachtResultaten.razor` — gebruik nieuw toegevoegde `[TableColumn]` op `DwarskrachtWapContext`

### Fase 5 – Stap-componenten

- [ ] **5.1** `StapMateriaalSelectie.razor` — dropdowns voor elk materiaaltype  
- [ ] **5.2** `StapProfielSelectie.razor` — hergebruik `KitProfielSelectie` voor staal, vrije invoer voor beton/hout  
- [ ] **5.3** `StapWapening.razor` — hergebruik controls uit `DoorsneeDemoComponent`  
- [ ] **5.4** `StapInterneKrachten.razor` — hergebruik `KrachtEntry`-lijst uit `DoorsneeDemoComponent`  
- [ ] **5.5** `StapToetskeuze.razor` — toets-checkboxes met relevantie-badges

### Fase 6 – Hoofd-component

- [ ] **6.1** `DoorsnedeToetsingComponent.razor` — stappenbeheer, context-state, berekeningslogica  
- [ ] **6.2** Pagina-route aanmaken (`/toetsing/doorsnede` of via `DoorsneeDemoComponent` parameter)

### Fase 7 – Testen & Verfijning

- [ ] **7.1** Unit tests voor relevantielogica (`ToetsSelectieModel.UpdateRelevantie`)  
- [ ] **7.2** UI test: doorloop alle stappen per materiaaltype  
- [ ] **7.3** Controleer TSV-kopie werkt in Excel  
- [ ] **7.4** Edge cases: 0-krachten, ontbrekend profiel, beton zonder wapening

---

## ❓ Open Punten & Beslissingen

| # | Vraag | Voorstel |
|---|-------|---------|
| 1 | Stap-layout: stepper (lineair) of tabmenu (zoals demo)? | Tabmenu — consistent met bestaande component |
| 2 | `KrachtEntry` hergebruiken vanuit demo of dupliceren? | Verplaatsen naar `CommonLibrary.Models` of `Construct.WebUI.Server\Models\Shared` |
| 3 | EC3 gecombineerde toets My+Vz: nu als aparte toets of automatisch? | Aparte toets `EC3_MV`, alleen relevant als beide aanwezig én Vz > 0.5·VRd |
| 4 | EC2: één tabelrij per kracht×zijde of per kracht (boven+onder naast elkaar)? | Rij per zijde (boven/onder), want dat sluit aan op bestaande BendingResults layout |
| 5 | Beton-toetsen: `BaseEurocodeToets` uitbreiden zodat ook EC2 via dezelfde interface loopt? | Nee — te complex voor betonwapening; EC2 blijft eigen context-objecten gebruiken |
| 6 | `Order` property in `TableColumnAttribute`: via constructor of init-only? | Init-only property `public int Order { get; init; } = 0` |
| 7 | Resultaten bewaren bij navigatie (bijv. naar andere stap terug)? | Resultaten bewaren in `DoorsnedeToetsContext` totdat invoer wijzigt |
