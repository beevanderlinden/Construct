Doel

Kort: analyseren en voorstellen hoe `PlaatWapeningGroep` zodanig voorbereid kan worden dat dezelfde structuur ook gebruikt kan worden voor `BalkWapeningGroep`, waarbij enige nuance is dat de `VerdeelWapening` voor balken een andere concrete type moet worden (bv. `BeugelWapeningGroep`). Geen code wijzigen nu — alleen analyse en plan.

Huidige situatie

- `PlaatWapeningGroep` is een concrete klasse met properties:
  - `BasisWapening` : `WapeningContext`
  - `VerdeelWapening` : `WapeningContext?` (optioneel)
  - `BijlegWapening` : `WapeningContext?`
  - `DekkingBuitensteLaag` : `BetonDekkingContext`
  - `LaagHoofdwapening` : int (setter roept `UpdateReferentieDekkingen()`)
- `UpdateReferentieDekkingen()` zet `ReferentieDekking` op de aanwezige `WapeningContext` objecten.
- Voor balken (BalkWapening) moet `VerdeelWapening` in plaats van een `WapeningContext` een `BeugelWapeningGroep` zijn.

Randvoorwaarden / eisen

- Minimaliseer breaking changes (backwards compatibility belangrijk).  
- Houd runtime gedrag identiek voor bestaande plaat-gevallen.  
- Maak uitbreidbaar zodat balk-specifieke logica (bijv. beugels) kan leven naast plaat-logica zonder duplicatie.
- Behoud of verbeter testbaarheid en duidelijkheid (domain logic blijft in domain-assembly).

Ontwerpmogelijkheden (kort)

Optie A — Geen verandering (leave-as-is)
- Voordeel: geen werk, geen risico.
- Nadeel: balk-specifieke `VerdeelWapening` moet elders of ad-hoc behandeld; duplicatie of if-checks in UI/servicelogica.

Optie B — Subclassing (Concrete subtypes)
- Maak `BalkWapeningGroep : PlaatWapeningGroep`.
- Override of verberg `VerdeelWapening` met `new BeugelWapeningGroep? VerdeelWapening` of introduceer nieuwe property `BeugelWapening`.
- Voordeel: snel, expliciet.
- Nadeel: schaduwproperty (`new`) of breaking change bij gebruik via `PlaatWapeningGroep` referece; type-safety beperkt tenzij overal concrete gebruikt.

Optie C — Composition + Interface / abstract base
- Introduceer interface/abstraction `IWapeningGroup` of `BaseWapeningGroep` met door common properties (`DekkingBuitensteLaag`, `BasisWapening`, `VerdeelWapening` als interface type) en concrete implementaties `PlaatWapeningGroep` en `BalkWapeningGroep`.
- `VerdeelWapening` wordt van type `IWapening` of `IVerdeelWapening` (een interface waarop zowel `WapeningContext` als `BeugelWapeningGroep` kunnen implementeren of adaptors)
- Voordeel: flexibel en type-safe; goede long-term schaalbaarheid.
- Nadeel: vereist refactor van domain types and code that consumes `PlaatWapeningGroep`.

Optie D — Discriminated union / wrapper type
- Maak `VerdeelWapeningWrapper` die intern case-handling doet (plaat => WapeningContext, balk => BeugelWapeningGroep). Consumers gebruiken wrapper API.
- Voordeel: beperkt refactor, backwards compatible if wrapper exposes previous API.
- Nadeel: extra indirection en API-vetheid.

Aanbevolen aanpak (hogere prioriteit en rationale)

Aanbevolen: Optie C (Composition + interface) met pragmatische migratie via adaptors.
Rationale:
- Toekomstbestendig en type-safe: meerdere vormen van `VerdeelWapening` zijn mogelijk zonder type casting hottubs.  
- Helpt UI/Componenten: dezelfde rendering componenten kunnen op interface vertrouwen en concrete rendering voor plaat vs balk verschaffen.
- Migratie kan gefaseerd (tafel van stappen hieronder).

Grof plan van aanpak (taakstappen)

1) Inventarisatie (lichte impact analyse)
   - Zoek alle referenties naar `PlaatWapeningGroep`, `VerdeelWapening` en `WapeningContext`.  
   - Noteer serialization/json usages, DB mappings, (de)serializers en tests die afhankelijk zijn van concrete types.

2) Ontwerp interfaces
   - Introduceer `IWapeningElement` of `IWapeningType` (of `IBaseWapening`) die minimale contracten biedt gebruikt door UI en UpdateReferentieDekkingen:
     - Eigenschappen: `double? ReferentieDekking { get; set; }`, `double GemiddeldeDiameter { get; }`, `double As { get; }` (zoals nodig).
   - Defineer `IVerdeelWapening` (als specifiek) indien benodigd.

3) Maak adaptors voor bestaande types
   - Implementeer `WapeningContext` -> `IBaseWapening` door interface implementatie of wrapper/adaptor zodat bestaande class weinig verandert.
   - Implementeer of maak `BeugelWapeningGroep` (nieuw) die dezelfde interface implementeert.

4) Introduceer abstract base `BaseWapeningGroep` of interface `IWapeningGroep`
   - Verplaats gedeelde logica (UpdateReferentieDekkingen, Dekking handelingen) naar base/utility methoden die met interfaces werken.
   - `PlaatWapeningGroep` en `BalkWapeningGroep` implementeren interface of erven base.

5) Gefaseerde code-migratie
   - Start met intern gebruik van interfaces in helper-methoden (niet meteen openbare API wijzigen).
   - Update domain logica: `UpdateReferentieDekkingen` herschrijven zodat het werkt met `IBaseWapening` abstractions.
   - Voeg `BalkWapeningGroep` toe (subclass of concrete implementation) dat `VerdeelWapening` als `BeugelWapeningGroep` aanbiedt.

6) UI en componenten
   - Pas componenten (`PlaatWapeningComponent`, `PlaatWapeningGroepComponent`, `BaseWapeningView`) aan om `IWapeningGroep`/`IBaseWapening` te accepteren (of overloads) zodat dezelfde component voor balken gebruikt kan worden.

7) Tests en validatie
   - Unit tests voor UpdateReferentieDekkingen beide scenario's (hoofd in 1e laag of 2e laag) met zowel `WapeningContext` als `BeugelWapeningGroep`.
   - UI smoke tests (manual) en regression scenarios (serialisatie/deserialize).

8) Backwards compatibility
   - Houd public API (data-contracts) compatibel zolang nodig: als `WapeningContext` niet veilig veranderd kan worden, gebruik adaptors/wrappers die `ModelBinding` en JSON blijven ondersteunen.

Risico's & mitigaties

- Risk: veel referenties aan concrete type ? mitigatie: pakeer refactor in kleine commits en gebruik adaptors.  
- Risk: (de)serialisatie problemen ? schrijf conversie/adaptors voor JSON (System.Text.Json Converters) indien nodig.  
- Risk: regressie in berekeningen ? uitgebreide unit-tests en visuele tests (SVG previews) toevoegen.

Concrete deliverables (kort)

- MD-ontwerp document (dit bestand).  
- Interface(s) `IBaseWapening`, `IWapeningGroep` en adaptors.  
- `BalkWapeningGroep` concrete implementatie.  
- Kleine refactor in `UpdateReferentieDekkingen` om met interfaces te werken.  
- Component updates zodat UI kan werken met beide implementaties.

Suggestie voor eerste (kleine) implementatiestap

- Voeg `IBaseWapening` interface en implementeer deze op `WapeningContext` (non-breaking: implementatie in dezelfde class file).  
- Pas `UpdateReferentieDekkingen` intern aan om via interface-properties te werken (nog steeds binnen `PlaatWapeningGroep`).  
- Schrijf unit-test voor `UpdateReferentieDekkingen` met bestaande `WapeningContext`.

Tijdinschatting (ruw)

- Inventarisatie & tests: 1 dag
- Interfaces + adaptor + unit tests: 1-2 dagen
- Base class / groepsrefactor + tests: 1-2 dagen
- UI aanpassingen en verificatie: 1 dag
- Buffer en integratie: 1 dag

Totale raming: ~5–7 werkdagen (afhankelijk van test-coverage en onvoorziene complexity).


Als volgende stap kan ik (op jouw bevel) een concrete commit/patch aanmaken met stap-1 (nieuwe interface `IBaseWapening` en implementatie op `WapeningContext`, en een unit-test voor `UpdateReferentieDekkingen`).
