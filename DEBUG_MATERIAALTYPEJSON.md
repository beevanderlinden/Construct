# DEBUGGING: MateriaalType in JSON

## Wat Te Testen

### 1. **Zorg voor CLEAN START**
- [ ] Delete localStorage van je browser (F12 ? Application ? LocalStorage ? delete)
- [ ] Close browser tabs
- [ ] Stop de applicatie
- [ ] Clean & Rebuild solution
- [ ] Start applicatie opnieuw

### 2. **Maak NIEUW project**
- [ ] Klik "Nieuw" project
- [ ] Voeg 1 Bordes toe
- [ ] **SAVE** (niet close, direct save)
- [ ] Open InspectSaveFile
- [ ] **Check JSON** - zoek naar `"MateriaalType"` of `"Type"`

### 3. **Als MateriaalType ER STAAT:**
? **Converter werkt!**
- [ ] Sluiten en opnieuw openen ? moet werken
- [ ] Project.Materialen moet gevuld zijn

### 4. **Als MateriaalType ER NIET STAAT:**
? **Converter werkt nog niet**
- [ ] Check console output (F12 ? Console)
- [ ] Zoek naar: `?? BaseMateriaalWrapper.Write() for type: BetonContext`
- [ ] Zoek naar: `?? Wrote MateriaalType: Beton`

## Waarschijnlijke Oorzaak

Je laadt waarschijnlijk een **oud project** dat was opgeslagen VOORDAT we de converter toevoegden.

**Oplossing:** 
1. Maak een GEHEEL NIEUW project
2. Voeg GEEN bestaande bordes toe
3. Maak alles van nul

## Debug Output Die Je Zou Moeten Zien

**Bij Save:**
```
?? BaseMateriaalWrapper.Write() for type: BetonContext
?? Wrote MateriaalType: Beton
```

**Bij Load:**
```
?? BaseMateriaalDictionaryConverter.Read() START
?? Array format detected, items: 1
?? Found MateriaalType: Beton
?? Deserialiseren Beton
? [0] Materiaal geladen: BetonContext
```

Post deze console output en we weten precies waar het misgaat!
