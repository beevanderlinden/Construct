# Wapening Afhandeling – Implementatieplan

## Doel

Voeg een instelbare **wapening-afhandeling** toe op projectniveau, zodat de gebruiker controle heeft over hoe het programma omgaat met wapeningsinvoer bij herberekening.

---

## Opties

| Optie           | Omschrijving                                                                                             |
|-----------------|----------------------------------------------------------------------------------------------------------|
| `Gebruiker`     | Geen automatische aanpassing. Programma controleert invoer maar past niets aan.                         |
| `AlleenVerhogen`| *(default)* Programma verhoogt de wapening alleen als de ingevoerde wapening onvoldoende is.            |
| `Optimaliseer`  | Programma genereert altijd de optimale (economisch kleinste) wapening, ook als die lager uitvalt.       |

> **Opmerking:** Bij alle opties worden minimale wapeningseisen (ondergrenzen) altijd nageleefd.
> Als de invoer **leeg/null** is, berekent het programma altijd automatisch de wapening (ongeacht instelling).

---

## Flowschema – Wapening Afhandeling

```
┌──────────────────────────────────────────┐
│        Bijwerken element aangeroepen     │
└─────────────────────┬────────────────────┘
                      │
                      ▼
          ┌───────────────────────┐
          │  Invoer aanwezig?     │
          │  (Tekst niet leeg)    │
          └──────┬────────────────┘
                 │
        Nee ─────┤──── Ja
         │               │
         ▼               ▼
   ┌───────────┐   ┌──────────────────────────────────────────┐
   │Automatisch│   │          Controleer instelling           │
   │ invullen  │   └────┬──────────────┬───────────────┬──────┘
   └───────────┘        │              │               │
                        ▼              ▼               ▼
                ┌───────────┐  ┌────────────────┐  ┌──────────────┐
                │ Gebruiker │  │ AlleenVerhogen │  │ Optimaliseer │
                └────┬──────┘  └──────┬─────────┘  └─────┬────────┘
                     │                │                  │
                     ▼                ▼                  ▼
              ┌─────────────┐  ┌──────────────┐ ┌────────────────┐
              │ Controleer  │  │ Controleer   │ │ Herbereken     │
              │ (valideer   │  │ en verhoog   │ │ optimale       │
              │ alleen,     │  │ indien       │ │ wapening       │
              │ geen        │  │ onvoldoende) │ │ (ook verlagen) │
              │ aanpassing) │  └──────────────┘ └────────────────┘
              └─────────────┘                         
   
  NB. Er wordt altijd rekening gehouden met minimale wapeningseisen (ondergrenzen) bij alle opties.
  
  Tip: Maak de invoer leeg om altijd automatisch te laten bepalen, ongeacht instelling.

```

---

## Implementatiestappen

### Stap 1 – Enum aanmaken
- Bestand: `Construct.Domain\Entities\WapeningAfhandelingEnum.cs`
- Waarden: `Gebruiker`, `AlleenVerhogen` (default), `Optimaliseer`

### Stap 2 – ProjectInfoEntity uitbreiden
- Voeg `WapeningAfhandeling` property toe aan `ProjectInfoEntity`
- Default: `WapeningAfhandelingEnum.AlleenVerhogen`

### Stap 3 – WapeningOptimizer uitbreiden
- Voeg methode `BepaalNieuweWapening(string? huidigeTekst, double asRequired, WapeningContext wap, WapeningAfhandelingEnum instelling)` toe
- Implementeer logica per instelling:
  - `Gebruiker`: retourneer `huidigeTekst` ongewijzigd (valideer alleen)
  - `AlleenVerhogen`: verhoog alleen als `asProvided < asRequired`
  - `Optimaliseer`: altijd herberekenen (kan ook verlagen)
- Leeg/null check vooraan: als leeg → altijd automatisch bepalen

### Stap 4 – SteekTrapEntity aanpassen
- `OptimaliseerSchilWapening()` gebruikt `ProjectInfo.WapeningAfhandeling`
- Zelfde logica in iteratieve scheurwijdte- en doorbuigingslus

### Stap 5 – BordesEntity aanpassen
- `BepaalPlaatWapening` aanroepen met instelling

### Stap 6 – UI component
- Bestand: `Construct.WebUI.Server\Components\Shared\WapeningAfhandelingSelector.razor`
- Dropdown met de drie opties + korte toelichting per optie
- Toon flowschema als tooltip/popover

---

## Voorbeeldscenario's

| Scenario | Instelling | Wapening invoer | Resultaat |
|----------|-----------|-----------------|-----------|
| Aantrede wijzigt, r8-125 is nog voldoende | AlleenVerhogen | r8-125 | Blijft r8-125 |
| Aantrede wijzigt, r8-125 is niet meer voldoende | AlleenVerhogen | r8-125 | Verhoogd naar bijv. r8-100 |
| Gebruiker wil exact r10-150 | Gebruiker | r10-150 | Blijft r10-150 (validatie-melding indien onvoldoende) |
| Altijd optimaal | Optimaliseer | r8-125 | Herberekend naar optimale wapening |
| Invoer leeg | elk | (leeg) | Altijd automatisch berekend |
