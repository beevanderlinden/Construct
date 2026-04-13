# Laad Defaults Functionaliteit

## Overzicht
De "Laad defaults" knop op de homepage stelt gebruikers in staat om vooraf gedefinieerde projecten te laden vanuit een speciale defaults-map.

## Locatie
- **Component**: `Construct.WebUI.Server/Components/Shared/LoadDefaultsComponent.razor`
- **Gebruikt in**: `Construct.WebUI.Server/Components/Pages/Home.razor`
- **Defaults map**: `wwwroot/defaults/`

## Gebruik

### Voor gebruikers:
1. Open de homepage (`/`)
2. Klik op de **"Laad defaults"** knop (blauwe knop met folder icoon)
3. Selecteer een default project uit de lijst
4. Het project wordt geladen en u wordt doorgestuurd naar de project info pagina

### Voor beheerders/ontwikkelaars:

#### Default projecten toevoegen:
1. Plaats `.cprj` bestanden in de `wwwroot/defaults/` map
2. De bestanden moeten geldige Project JSON zijn
3. De applicatie laadt automatisch alle `.cprj` bestanden uit deze map

#### Projectinfo weergave:
De lijst toont voor elk default project:
- **Titel**: De project naam (uit `ProjectInfo.Naam`) of de bestandsnaam als fallback
- **Subtitel**: Het project nummer (uit `ProjectInfo.Nummer`)
- **Metadata**: Bestandsgrootte en laatste wijzigingsdatum

## Technische details

### Afhankelijkheden:
```csharp
@inject IJSRuntime JS
@inject IProjectStateService ProjectState
@inject IToastService ToastService
@inject NavigationManager NavigationManager
@inject IWebHostEnvironment Env
```

### Workflow:
1. **Knop klik** ? opent een FluentDialog
2. **Map scan** ? leest alle `.cprj` bestanden uit `wwwroot/defaults/`
3. **Deserialisatie** ? parse elk bestand als `ProjectEntity`
4. **Weergave** ? lijst van gevonden projecten in de dialog
5. **Selectie** ? gebruiker klikt op een project
6. **Laden** ? project wordt ge deserialiseerd, geïnitialiseerd met `InitAll()` en geladen via `ProjectState.SetProject()`
7. **Navigatie** ? redirect naar `/project/info`

### Foutafhandeling:
- Als de defaults map niet bestaat, wordt deze automatisch aangemaakt
- Corrupte bestanden worden overgeslagen (met console logging)
- Gebruikersvriendelijke foutmeldingen via toast notifications

## Voorbeeldstructuur

```
wwwroot/
  defaults/
    ??? Voorbeeld_Steektrap.cprj
    ??? Standaard_Bordes.cprj
    ??? Template_Kolom.cprj
```

## Toekomstige verbeteringen
- [ ] Categorisatie van defaults (steektrap, bordes, etc.)
- [ ] Preview van het gekozen default project
- [ ] Mogelijkheid om huidige project als default op te slaan
- [ ] Import vanuit externe bron (URL, netwerklocatie)
