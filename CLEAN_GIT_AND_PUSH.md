# Schoon Git -> Push naar GitHub (stappenplan)

Dit stappenplan in PowerShell helpt je om een schone lokale repository te maken en succesvol naar GitHub te pushen zonder dat oude grote pack-bestanden (>
100 MB) de push blokkeren.

LET OP: dit verwijdert alle lokale Git-history. Maak een backup als je dat wilt.

---

## 0) Backup (optioneel maar aanbevolen)

```powershell
cd C:\MySource
# kopieer projectmap (snelste veilige backup)
Copy-Item Construct Construct.backup -Recurse
```

---

## 1) Stop tools die bestanden vasthouden
- Sluit Visual Studio / VSCode / Explorer-vensters op de projectmap.

---

## 2) Zoek naar geneste `.git` mappen (vaak oorzaak)

```powershell
cd C:\MySource\Construct
# toont alle .git mappen (ook genest)
Get-ChildItem -Recurse -Directory -Force -Filter ".git" | Select-Object FullName
```

Als je iets ziet zoals `C:\MySource\Construct\Construct.git` of `C:\MySource\Construct\somefolder\.git` ? dat moet weg.

```powershell
# voorbeeld verwijderen (zorg dat pad correct is!)
Remove-Item "C:\MySource\Construct\Construct.git" -Recurse -Force
```

---

## 3) Verwijder grote build / publish outputs lokaal

```powershell
cd C:\MySource\Construct
# verwijder bekende build mappen
Get-ChildItem -Recurse -Directory -Force -Include bin,obj,publish | ForEach-Object { Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue }
# verwijder losse grote bestanden
Remove-Item .\publish.zip -Force -ErrorAction SilentlyContinue
```

Controleer welke (nog) grote bestanden aanwezig zijn:

```powershell
Get-ChildItem -Recurse -File | Where-Object { $_.Length -gt 10MB } | Sort-Object Length -Descending | Select-Object FullName, @{n='SizeMB';e={[math]::Round($_.Length/1MB,2)}}
```

Verwijder of verplaats elke vermelde grote file als die niet in de repo moet.

---

## 4) Voeg (of controleer) `.gitignore` in de root

Maak of update `.gitignore` zodat build artefacten NIET getrackt worden:

```text
# .gitignore (aanbevolen minimale inhoud)
bin/
obj/
publish/
publish.zip
.vs/
*.zip
*.dll
*.exe
node_modules/
```

Commit de .gitignore later (na stap 6).

---

## 5) Verwijder lokale Git-historie (maak een schone repo)

> Dit verwijdert alle commits en branches lokaal. Remote op GitHub behoudt zijn repo totdat je pusht.

```powershell
cd C:\MySource\Construct
# Zorg dat je geen ongeslagen werk hebt - stash of commit
git status
# Als je wijzigingen wilt bewaren:
# git stash
# OF commit ze eerst

# Verwijder oude .git
Remove-Item .git -Recurse -Force -ErrorAction SilentlyContinue
```

---

## 6) Initialiseer nieuwe repo en voeg remote toe

Maak een lege repo op GitHub via de website (naam `Construct`) of gebruik de bestaande lege repo.

```powershell
cd C:\MySource\Construct
git init
# voeg GitHub remote toe (gebruik HTTPS of SSH)
# HTTPS voorbeeld:
git remote add origin https://github.com/beevanderlinden/Construct.git
# (of) SSH voorbeeld:
# git remote add origin git@github.com:beevanderlinden/Construct.git
```

---

## 7) Add, commit en push

Voeg alleen de gewenste bestanden toe (je .gitignore voorkomt dat bin/obj wordt toegevoegd).

```powershell
git add .
git commit -m "Initial commit - clean repository"
# Zet branchnaam (master of main) en push
git branch -M master
git push -u origin master
```

Als `git push` alsnog faalt met hetzelfde pack error, STOP en voer stap 8 uit.

---

## 8) Diagnose: controleer lokale .git pack grootte

```powershell
# toon pack files binnen .git
Get-ChildItem .git\objects\pack -Filter "*.pack" | ForEach-Object { "$($_.Name): $([math]::Round($_.Length/1MB,2)) MB" }
# totale grootte .git
(Get-Item .git -Recurse | Measure-Object -Sum Length).Sum / 1MB
```

Als hier nog een .pack > 100 MB staat, dan is `Remove-Item .git` niet gelukt of er is nog een geneste .git die je over het hoofd zag. Herhaal stap 2 en 5.

---

## 9) Als je grote bestanden in repo-history wilt behouden: overweeg Git LFS
Als je grote assets (video, grote dlls) TIJDENS development nodig hebt, gebruik Git LFS:

- Installeer Git LFS: https://git-lfs.github.com/
- `git lfs install`
- `git lfs track "*.dll"` (voorbeeld)
- Voeg `.gitattributes` commit en push

Maar dit lost geen probleem op met reeds gepushte pack files — die moet je eerst verwijderen (history rewrite).

---

## 10) Laatste checks als push faalt

1. Controleer remote is correct:
```powershell
git remote -v
```
2. Controleer of GitHub repo leeg is (verwijder & maak opnieuw lege repo via website indien nodig).
3. Voer de volledige procedure hierboven opnieuw.

---

## Veelvoorkomende oorzaken samengevat
- Geneste `*.git` mappen (bv. `Construct.git`) in projectmap ? verwijder
- Build/publish mappen gecopied naar project ? verwijder en voeg toe aan .gitignore
- Oude history met grote bestanden ? verwijder `.git` en herstart met een nieuwe repo (als history niet nodig)
- GitHub cache van oude repo (wanneer remote niet leeg is) ? verwijder repo op GitHub en maak opnieuw

---

Als je wilt, kan ik dit automatisch uitvoeren voor jouw workspace via de terminalcommando's hierboven — laat me weten of ik ze moet uitvoeren (ik kan de exacte PowerShell commando's genereren die je kunt kopiëren/plakken). Succes! ??