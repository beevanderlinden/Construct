# Testplan: `Eurocode.BetonConstructies` — stap voor stap

> **Doel:** Systematische unit- en integratietests per Eurocode-onderdeel,  
> zodat rekenregels onafhankelijk van de assemblage (`SteekTrapEntity`, `BordesEntity`)  
> verifieerbaar en regressiebestendig zijn.

---

## Mapstructuur (voorstel)

```
Construct.Tests/
├── Eurocode/
│   ├── BetonConstructies/
│   │   ├── H3_Materialen/
│   │   │   ├── BetonContextTests.cs
│   │   │   └── BetonStaalContextTests.cs
│   │   ├── H4_Duurzaamheid/
│   │   │   ├── BetonDekkingContextTests.cs
│   │   │   └── ConstructieklasseTests.cs
│   │   ├── H6_UGT/
│   │   │   ├── BendingResultsTests.cs
│   │   │   └── DwarskrachtWapContextTests.cs
│   │   ├── H7_BGT/
│   │   │   ├── GrenswaardeSlankheidTests.cs
│   │   │   ├── ScheurwijdteContextTests.cs
│   │   │   └── DoorbuigingValidatieContextTests.cs
│   │   ├── H8_Detailleren/
│   │   │   └── WapeningContextTests.cs
│   │   ├── PrefabRegels/
│   │   │   ├── OpleggingContextTests.cs
│   │   │   └── UitkragingContextTests.cs
│   │   └── Brandwerendheid/
│   │       └── PlaatBrandwerendheidTests.cs
│   ├── StaalConstructies/       ← toekomstig
│   ├── HoutConstructies/        ← toekomstig
│   └── Grondslagen/             ← toekomstig
└── Factories/
    └── SteekTrapFactory.cs   ← (bestaand)
```

---

## Prioriteit

| Prioriteit | Reden |
|---|---|
| **Hoog** | Direct gebruikt in steektrap/bordes-berekening; fouten zijn meteen zichtbaar in productie |
| **Middel** | Ondersteunend; minder kans op regressie maar waardevol voor documentatie |
| **Laag** | Hulpfuncties, enums, weergave — zelden veranderend |

---

## Stap 1 — §3 Materialen: `BetonContext` ⭐ Hoog

**Bestand:** `H3_Materialen/BetonContextTests.cs`  
**Klasse:** `Eurocode.BetonConstructies.BetonContext`

### Tests

| Testnaam | Wat wordt gecontroleerd | Referentie |
|---|---|---|
| `C2025_HeeftCorrecteFck` | `fck = 20 N/mm²` | EC2 tabel 3.1 |
| `C4555_HeeftCorrecteFck` | `fck = 45 N/mm²` | EC2 tabel 3.1 |
| `C3037_HeeftCorrecteFcm` | `fcm = fck + 8 = 38 N/mm²` | EC2 §3.1.2(4) |
| `C3037_HeeftCorrecteFctm` | `fctm = 0.30 × fck^(2/3) ≈ 2.9 N/mm²` | EC2 §3.1.2(4) |
| `C3037_HeeftCorrecteEcm` | `Ecm = 22 × (fcm/10)^0.3 ≈ 32.8 GPa` | EC2 §3.1.3(2) |
| `PartieleFactor_IsStandaard15` | `γ_c = 1.5` | EC2 §2.4.2.4 |
| `AlphaCC_IsStandaard085` | `α_cc = 0.85` (NL bijlage) | NEN-EN 1992-1-1 NB |
| `Fcd_BerekendCorrect` | `fcd = α_cc × fck / γ_c` | EC2 §3.1.6(1) |

---

## Stap 2 — §3 Materialen: `BetonStaalContext` ⭐ Hoog

**Bestand:** `H3_Materialen/BetonStaalContextTests.cs`  
**Klasse:** `Eurocode.BetonConstructies.BetonStaalContext`

### Tests

| Testnaam | Wat wordt gecontroleerd | Referentie |
|---|---|---|
| `B500B_HeeftCorrecteFyk` | `fyk = 500 N/mm²` | EC2 §3.2.2 |
| `B500B_HeeftCorrecteFyd` | `fyd = fyk / γ_s = 500/1.15 ≈ 434.8 N/mm²` | EC2 §3.2.7(2) |
| `B500B_HeeftCorrecteEs` | `Es = 200 000 N/mm²` | EC2 §3.2.7(4) |

---

## Stap 3 — §3 Materialen: `BetonContextKruipEnKrimpCalculator` 🔶 Middel

**Bestand:** `H3_Materialen/BetonContextTests.cs` (uitbreiden)  
**Klasse:** `Eurocode.BetonConstructies.BetonContextKruipEnKrimpCalculator`

### Tests

| Testnaam | Wat wordt gecontroleerd |
|---|---|
| `KruipGetal_IsPositief` | `φ > 0` voor standaard invoer |
| `KruipGetal_NeeemtToeMetLangereLastduur` | `φ(t=∞) > φ(t=28d)` |
| `Krimp_IsNegatief` | `ε_cs < 0` (inkrimping) |

---

## Stap 4 — §4 Duurzaamheid: `BetonDekkingContext` ⭐ Hoog

**Bestand:** `H4_Duurzaamheid/BetonDekkingContextTests.cs`  
**Klasse:** `Eurocode.BetonConstructies.BetonDekkingContext`

### Tests

| Testnaam | Wat wordt gecontroleerd | Referentie |
|---|---|---|
| `XC1_HeeftCorrecteMinimaleDekking` | `cmin,dur` voor XC1 | EC2 tabel 4.4N |
| `XC4_HeeftHogereMinimaleDekking_DanXC1` | `cmin(XC4) > cmin(XC1)` | EC2 tabel 4.4N |
| `DekkingToe_IsMinPlusDeltaDev` | `ctoe = cmin + Δcdev` | EC2 §4.4.1.3 |
| `KwaliteitsBeheersing_VerlaagdDeltaDev` | `Δcdev` kleiner bij kwaliteitsbeheersing | NL bijlage |
| `PlaatGeometrie_Beeïnvloedt_Constructieklasse` | klasse wijzigt bij `IsPlaatGeometrie` | EC2 §4.4.1.2 |
| `IsValidated_TrueAlsDekking_Voldoet` | `IsValidated = true` bij correcte dekking | — |
| `IsValidated_FalseAlsDekkingTeKlein` | `IsValidated = false` bij onderschrijding | — |

---

## Stap 5 — §4 Duurzaamheid: `Constructieklasse` 🔶 Middel

**Bestand:** `H4_Duurzaamheid/ConstructieklasseTests.cs`  
**Klasse:** `Eurocode.BetonConstructies.Constructieklasse`

### Tests

| Testnaam | Wat wordt gecontroleerd |
|---|---|
| `DefaultKlasse_IsS4` | standaard constructieklasse = S4 |
| `CC3_VerhoogtConstructieklasse` | CC3 → klasse S4→S5 of hoger |
| `PlaatGeometrie_VerlaagdKlasse` | `IsPlaatGeometrie = true` → klasse daalt |

---

## Stap 6 — §6 UGT: `BendingResults` ⭐ Hoog

**Bestand:** `H6_UGT/BendingResultsTests.cs`  
**Klasse:** `Eurocode.BetonConstructies.BendingResults`

Dit is de meest kritische klasse; fouten hier werken direct door in alle assemblages.

### Tests

| Testnaam | Wat wordt gecontroleerd | Invoer (voorbeeld) |
|---|---|---|
| `AsRequired_CorrectVoorBekendeInvoer` | `AsRequired ≈ referentiewaarde` | MEd=20 kNm, b=1000, h=150, C30/37 |
| `AsApplied_GreaterOrEqualAsRequired_WanneerAkkoord` | `AsApplied ≥ AsRequired` | — |
| `IsValidated_True_BijVoldoendeWapening` | `IsValidated = true` | wapening boven grens |
| `IsValidated_False_BijOnvoldoendeWapening` | `IsValidated = false` | wapening onder grens |
| `NuttigeHoogte_D_BerekendCorrect` | `d = h - c - ø/2` | h=150, c=25, ø=8 |
| `AsRequired_NulBijGeenMoment` | `AsRequired = 0` bij `MEd = 0` | — |
| `Minimumwapening_WordtToegepast` | `AsRequired ≥ As,min` | licht belaste plaat |

---

## Stap 7 — §6 UGT: `DwarskrachtWapContext` ⭐ Hoog

**Bestand:** `H6_UGT/DwarskrachtWapContextTests.cs`  
**Klasse:** `Eurocode.BetonConstructies.DwarskrachtWapContext`

### Tests

| Testnaam | Wat wordt gecontroleerd | Referentie |
|---|---|---|
| `VRdc_CorrectVoorBekendeInvoer` | `VRd,c ≈ referentie` | EC2 §6.2.2(1) |
| `IsValidated_True_BijVEd_KleinerDan_VRdc` | geen wapening nodig | — |
| `IsValidated_False_BijVEd_GroterDan_VRdc` | wapening nodig | — |
| `VRdc_NeeemtToeMetGroterD` | grotere nuttige hoogte → hogere VRd,c | — |

---

## Stap 8 — §7 BGT: `GrenswaardeSlankheidContext` ⭐ Hoog

**Bestand:** `H7_BGT/GrenswaardeSlankheidTests.cs`  
**Klasse:** `Eurocode.BetonConstructies.GrenswaardeSlankheidContext`

### Tests

| Testnaam | Wat wordt gecontroleerd | Referentie |
|---|---|---|
| `Slankheid_LambdaCorrectBerekend` | `λ = L / d` | EC2 §7.4.2 |
| `IsValidated_True_BijLambda_KleinerDanGrens` | `λ ≤ λ_lim` | — |
| `IsValidated_False_BijLambda_GroterDanGrens` | `λ > λ_lim` | — |
| `GrenswaardeNeeemtToeMetMeerWapening` | hogere `ρ` → hogere `λ_lim` | EC2 §7.4.2(2) |

---

## Stap 9 — §7 BGT: `ScheurwijdteContext` ⭐ Hoog

**Bestand:** `H7_BGT/ScheurwijdteContextTests.cs`  
**Klasse:** `Eurocode.BetonConstructies.ScheurwijdteContext`

### Tests

| Testnaam | Wat wordt gecontroleerd | Referentie |
|---|---|---|
| `Wk_PositiefBijPositiefMoment` | `wk > 0` bij trekkracht onderzijde | EC2 §7.3.4 |
| `Wk_NulBijGeenMoment` | `wk ≈ 0` bij `MEd = 0` | — |
| `Wmax_XC1_Is03mm` | `wmax = 0.3 mm` voor XC1, NL bijlage | NL NB tabel |
| `Wmax_XC4_Is02mm` | `wmax = 0.2 mm` voor XC4 (buitenomgeving) | NL NB tabel |
| `IsValidated_True_BijWk_KleinerDanWmax` | `IsValidated = true` | — |
| `IsValidated_False_BijWk_GroterDanWmax` | `IsValidated = false` | — |
| `NauwdereWapening_VerlaagdWk` | dichter hoh → kleinere `wk` | — |

---

## Stap 10 — §7 BGT: `DoorbuigingValidatieContext` ⭐ Hoog

**Bestand:** `H7_BGT/DoorbuigingValidatieContextTests.cs`  
**Klasse:** `Eurocode.BetonConstructies.DoorbuigingValidatieContext`

Deels al gedekt in `ProjectRD506001Tests`. Hier losse unit tests voor de context zelf.

### Tests

| Testnaam | Wat wordt gecontroleerd | Referentie |
|---|---|---|
| `Wbijk_PositiefBijNeerwaartsBelasting` | `Wbijk > 0` | EC2 §7.4.3 |
| `Wmax_CorrectBerekend` | `Wmax = Wbijk + W2 - Wc` | EC2 §7.4.1 |
| `IsValidated_True_BijVoldoendeSchilDikte` | dikke schil → `UC ≤ 1.0` | — |
| `IsValidated_False_BijTeDunneSchil` | dunne schil → `UC > 1.0` | — |
| `DikkereSchil_GeeftMindereDeflectie` | `W(h=160) ≤ W(h=150)` | — |
| `LangereSpanning_GeeftMeerDeflectie` | `W(L=5000) > W(L=4000)` | — |
| `UcBijk_KleinerDanEen_WanneerValidated` | `UC_bijk < 1.0` ↔ `IsValidated` | — |

---

## Stap 11 — §8 Detailleren: `WapeningContext` ⭐ Hoog

**Bestand:** `H8_Detailleren/WapeningContextTests.cs`  
**Klasse:** `Eurocode.BetonConstructies.WapeningContext`

### Tests

| Testnaam | Wat wordt gecontroleerd | Invoer |
|---|---|---|
| `Parse_StaafformulierMetR_CorrectAs` | `As` correct | `"r8-150"` |
| `Parse_StaafformulierZonderPrefix_CorrectAs` | `As` correct | `"8-150"` |
| `Parse_StaafformulierMetFi_CorrectAs` | `As` correct | `"Ø8-150"` |
| `Parse_AantalStaven_CorrectAs` | `As` correct | `"4x16"` |
| `As_NeeemtToeMetKleinerHoh` | `As(8-100) > As(8-150)` | — |
| `As_NeeemtToeMetGroterDiameter` | `As(10-150) > As(8-150)` | — |
| `GemiddeldeDiameter_CorrectBerekend` | `d_eq` correct | samengestelde staaf |
| `ZRef_WordtGezet_NaSetZRef` | `ZRef > 0` na `SetZRef()` | h=150, c=25 |

---

## Stap 12 — Aanvullende regels prefab: `OpleggingContext` 🔶 Middel

**Bestand:** `PrefabRegels/OpleggingContextTests.cs`  
**Klasse:** `Eurocode.BetonConstructies.OpleggingContext`

### Tests

| Testnaam | Wat wordt gecontroleerd |
|---|---|
| `OpleggingsLengte_Berekend_VoldoetAanMinimum` | `a_netto ≥ a_min` |
| `IsValidated_True_BijVoldoendeOplegging` | akkoord bij voldoende lengte |
| `IsValidated_False_BijTeKorteOplegging` | niet akkoord bij te korte oplegging |

---

## Stap 13 — Aanvullende regels prefab: `UitkragingContext` 🔶 Middel

**Bestand:** `PrefabRegels/UitkragingContextTests.cs`  
**Klasse:** `Eurocode.BetonConstructies.UitkragingContext`

### Tests

| Testnaam | Wat wordt gecontroleerd |
|---|---|
| `AsRequired_CorrectVoorBekendeInvoer` | wapening tand/neus correct |
| `IsValidated_True_BijVoldoende` | tand/neus akkoord |
| `DwarskrachtNeus_KleinerDanMax` | `VEd ≤ VRd,max` (geen wapeningsbehoefte) |

---

## Stap 14 — Brandwerendheid: `PlaatBrandwerendheid` ⭐ Hoog (tabelvaste waarden)

**Bestand:** `Brandwerendheid/PlaatBrandwerendheidTests.cs`  
**Klasse:** `Eurocode.BetonConstructies.PlaatBrandwerendheid`  
**Referentie:** NEN-EN 1992-1-2:2004, Tabel 5.8

Dit is een pure tabelopzoekfunctie; ideaal voor parameter-driven tests.

### Tests

```csharp
// Tabel 5.8 referentiewaarden
[Theory]
[InlineData( 60,  20, 30)]    // hs=80mm, a=20mm → REI 30
[InlineData( 80,  20, 60)]    // hs=80mm, a=20mm → REI 60
[InlineData(100,  30, 90)]
[InlineData(120,  40, 120)]
[InlineData(150,  55, 180)]
[InlineData(175,  65, 240)]
public void GetRei_ReturnsCorrectValue(double h, double a, int expectedRei)
```

| Testnaam | Wat wordt gecontroleerd |
|---|---|
| `GetRei_ReturnsCorrectValue` (Theory) | Tabelwaarden REI 30–240 zijn correct |
| `GetRei_Returns0_BijTeDunneSchil` | `GetRei(50, 10) = 0` |
| `GetAfstand_BerekendCorrect_VoorRei60` | `a_min` voor REI 60 correct |
| `GetAfstand_NaN_BijOnbekendRei` | `double.NaN` bij onbekende REI |
| `GetAfstand_NaN_BijTeDunneSchil` | `double.NaN` als `hs < hs_min` voor gevraagde REI |

---

## Uitvoering — volgorde aanbevolen

```
Stap 14 → Stap 1 → Stap 2 → Stap 11 → Stap 4 → Stap 5
        → Stap 6 → Stap 7 → Stap 8   → Stap 9 → Stap 10
        → Stap 3 → Stap 12 → Stap 13
```

> Begin bij **Stap 14** (brandwerendheid): pure tabeltest, geen afhankelijkheden,  
> snel resultaat en goede introductie in het testpatroon.  
> Dan **§3 Materialen** als fundament voor alle volgende stappen.

---

## Conventies in de tests

- **xUnit + FluentAssertions** (conform bestaande tests)
- `[Fact]` voor enkelvoudige scenario's, `[Theory] + [InlineData]` voor tabelwaarden
- Tolerantie bij zwevendekommagetalsvergelijkingen: `.BeApproximately(expected, delta)`  
  — gebruik `delta = 0.5` voor N/mm²-waarden, `delta = 0.01` voor eenheidloze factoren
- Geen afhankelijkheid van `SteekTrapEntity` of `BordesEntity` — puur op Eurocode-klassen
- Elk testbestand bevat een `<summary>` met het Eurocode-artikel dat wordt getest

---

*Gegenereerd: zie `Construct.Tests/Docs/TestplanEurocodeBetonConstructies.md`*
