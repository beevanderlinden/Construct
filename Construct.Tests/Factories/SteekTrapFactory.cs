using Construct.Domain.Entities;
using Eurocode.Belastingen;
using Eurocode.BetonConstructies;
using Eurocode.Grondslagen;

namespace Construct.Tests.Factories;

public static class SteekTrapFactory
{
    /// <summary>
    /// Maakt een standaard SteekTrapEntity met een vers ProjectEntity en BetonContext (C20/25).
    /// Mirrors de logica van AddAssemblage.razor → GetSteekTrap().
    /// </summary>
    public static (SteekTrapEntity trap, ProjectEntity project) Create(
        int aantalTreden = 16,
        double schildikte = 120,
        double optrede = 185,
        double aantrede = 220,
        bool heeftBoventand = true)
    {
        var project = new ProjectEntity();
        var beton = project.VoegMateriaalToe(new BetonContext("C20/25"));

        var trap = new SteekTrapEntity(project.ProjectInfo)
        {
            Id = Guid.NewGuid(),
            Naam = "test steektrap",
            Merk = "TR-T1",
            Materiaal = beton,
            MateriaalId = beton.Id,
            Gebruiksklasse = project.DefaultGebruiksklasse,
            OptredeAantal1 = aantalTreden,
            OptredeMaat = optrede,
            AantredeMaat = aantrede,
            HeeftBoventand = heeftBoventand,
        };

        trap.SchilDikte = schildikte;

        // Init() aanroepen nadat alle properties zijn gezet (inclusief Materiaal en HeeftBoventand),
        // zodat MomentSchil, Dwarskracht, TandOplegging etc. de juiste waarden krijgen.
        // Dezelfde aanpak als RestoreReferencesAfterDeserialization().
        trap.Init(project.ProjectInfo);

        trap.LengteBoven = trap.AantredeMaat + trap.WelMaat;

        trap.Belastingen.GenereerBelastingCombinaties(
            trap.Belastingen,
            trap.Belastingen.BelastingGevallen,
            trap.Belastingen.CombinatiesTypes);

        return (trap, project);
    }

    /// <summary>
    /// Maakt project RD-505003 "12 app Hoflaan" STOLWIJK aan met 1 steektrap TR01/TR02.
    /// </summary>
    public static (SteekTrapEntity trap, ProjectEntity project) CreateProject_RD505003()
    {
        var project = new ProjectEntity
        {
            ProjectInfo =
            {
                Nummer = "RD-505003",
                Naam = "12 app Hoflaan",
                Plaatsnaam = "STOLWIJK",
                Grondslagen =
                {
                    NationaleBijlage = NationaleBijlageEnum.NL,
                    Gevolgklasse = GevolgklasseEnum.CC2b,
                    OntwerpLevensduur = OntwerpLevensduurEnum.Vijftig,
                },
                MinimaleREI = 60,
                FabrieksInstelling =
                {
                    FabrieksNaam = "MBS",
                    DiamBasis = 8,
                    DiamDetailWapening = 6,
                    HohBovengrens = 150,
                    HohOndergrensBasis = 100,
                },
            },
            DefaultGebruiksklasse = GebruiksklasseEnum.A_gemeenschappelijke_trappen,
        };

        project.DefaultDekking.Boven.IsPlaatGeometrie = true;
        project.DefaultDekking.Boven.IsKwaliteitsBeheersing = true;
        project.DefaultDekking.Boven.SelectedMilieuklassen = [MilieuklasseEnum.XC1];
        project.DefaultDekking.Boven.DekkingToe = 20;

        project.DefaultDekking.Onder.IsPlaatGeometrie = true;
        project.DefaultDekking.Onder.IsKwaliteitsBeheersing = true;
        project.DefaultDekking.Onder.SelectedMilieuklassen = [MilieuklasseEnum.XC1];
        project.DefaultDekking.Onder.DekkingToe = 20;

        var beton = project.VoegMateriaalToe(new BetonContext("C45/55"));

        var trap = new SteekTrapEntity(project.ProjectInfo)
        {
            Id = Guid.NewGuid(),
            Merk = "TR01/TR02",
            Materiaal = beton,
            MateriaalId = beton.Id,
            Gebruiksklasse = project.DefaultGebruiksklasse,
            OptredeAantal1 = 16,
            OptredeMaat = 188,
            AantredeMaat = 220,
            Breedte = 1200,
            WelMaatVertikaal = 60,
            WelOpgaveHoek = 15,
            HeeftBoventand = true,
        };

        trap.SchilDikte = 150;
        trap.Init(project.ProjectInfo);
        trap.LengteBoven = 235;

        trap.Belastingen.GenereerBelastingCombinaties(
            trap.Belastingen,
            trap.Belastingen.BelastingGevallen,
            trap.Belastingen.CombinatiesTypes);

        project.Assemblages.Add(trap);

        return (trap, project);
    }

    /// <summary>
    /// Maakt project RD-505007 "Mountain Network" Nieuwegein aan met trap TR1-2, TR3-6 en bordes BD1-3.
    /// </summary>
    public static (SteekTrapEntity trap12, SteekTrapEntity trap36, BordesEntity bordes, ProjectEntity project) CreateProject_RD505007()
    {
        var project = new ProjectEntity
        {
            ProjectInfo =
            {
                Nummer = "RD-505007",
                Naam = "Mountain Network",
                Plaatsnaam = "Nieuwegein",
                Grondslagen =
                {
                    NationaleBijlage = NationaleBijlageEnum.NL,
                    Gevolgklasse = GevolgklasseEnum.CC2b,
                    OntwerpLevensduur = OntwerpLevensduurEnum.Vijftig,
                },
                MinimaleREI = 60,
            },
            DefaultGebruiksklasse = GebruiksklasseEnum.C5_bijeenkomst_grote_menigtes,
        };

        project.DefaultDekking.Boven.IsPlaatGeometrie = true;
        project.DefaultDekking.Boven.IsKwaliteitsBeheersing = true;
        project.DefaultDekking.Boven.SelectedMilieuklassen = [MilieuklasseEnum.XC1];

        project.DefaultDekking.Onder.IsPlaatGeometrie = true;
        project.DefaultDekking.Onder.IsKwaliteitsBeheersing = true;
        project.DefaultDekking.Onder.SelectedMilieuklassen = [MilieuklasseEnum.XC1];

        var beton = project.VoegMateriaalToe(new BetonContext("C45/55"));

        // Trap TR1-2: Schil=150, Op=190, Aan=190, Lengte=2750
        var trap12 = new SteekTrapEntity(project.ProjectInfo)
        {
            Id = Guid.NewGuid(),
            Merk = "TR1-2",
            Materiaal = beton,
            MateriaalId = beton.Id,
            Gebruiksklasse = project.DefaultGebruiksklasse,
            OptredeAantal1 = 16,
            OptredeMaat = 190,
            AantredeMaat = 190,
            HeeftBoventand = true,
        };
        trap12.SchilDikte = 150;
        trap12.GebruikEigenLengte = true;
        trap12.LengteTotaalEigenOpgave = 2750;
        trap12.Init(project.ProjectInfo);

        // waarom is dit nodig?
        // het zou bij de INIT al geregld moeteen zijn.

        trap12.Belastingen.GenereerBelastingCombinaties(
            trap12.Belastingen,
            trap12.Belastingen.BelastingGevallen,
            trap12.Belastingen.CombinatiesTypes);

        // 

        project.Assemblages.Add(trap12);

        // Trap TR3-6: Schil=100, Op=204, Aan=220, Lengte=2040
        var trap36 = new SteekTrapEntity(project.ProjectInfo)
        {
            Id = Guid.NewGuid(),
            Merk = "TR3-6",
            Materiaal = beton,
            MateriaalId = beton.Id,
            Gebruiksklasse = project.DefaultGebruiksklasse,
            OptredeAantal1 = 10,
            OptredeMaat = 204,
            AantredeMaat = 220,
            HeeftBoventand = true,
        };
        trap36.SchilDikte = 100;
        trap36.GebruikEigenLengte = true;
        trap36.LengteTotaalEigenOpgave = 2040;
        trap36.Init(project.ProjectInfo);
        trap36.Belastingen.GenereerBelastingCombinaties(
            trap36.Belastingen,
            trap36.Belastingen.BelastingGevallen,
            trap36.Belastingen.CombinatiesTypes);
        project.Assemblages.Add(trap36);

        // Bordes BD1-3: Dikte=240, Breedte=1100, Lengte=3700
        var bordes = new BordesEntity
        {
            ProjectInfo = project.ProjectInfo,
            Id = Guid.NewGuid(),
            Merk = "BD1-3",
            Breedte = 1100,
            Lengte = 3700,
            Dikte = 240,
            Materiaal = beton,
            MateriaalId = beton.Id,
            Gebruiksklasse = project.DefaultGebruiksklasse,
        };
        bordes.Init(project.ProjectInfo);

        bordes.Trap1.AansluitendElement = trap12;
        bordes.Trap1.AansluitendElementId = trap12.Id;
        bordes.Trap1.Randafstand = 0;

        bordes.Trap2.AansluitendElement = trap36;
        bordes.Trap2.AansluitendElementId = trap36.Id;
        bordes.Trap2.Randafstand = 0;

        project.Assemblages.Add(bordes);

        return (trap12, trap36, bordes, project);
    }

    /// <summary>
    /// Maakt project RD-506001 aan met trap TR1: LtProjZ=4000, afwerking=1.5 kN/m².
    /// Geometrie kan worden overschreven via optionele parameters.
    /// </summary>
    public static (SteekTrapEntity trap, ProjectEntity project) CreateProject_RD506001(
        double optrede = 193,
        double aantrede = 220,
        double schildikte = 150)
    {
        var project = new ProjectEntity
        {
            ProjectInfo =
            {
                Nummer = "RD-506001",
                Grondslagen =
                {
                    NationaleBijlage = NationaleBijlageEnum.NL,
                    Gevolgklasse = GevolgklasseEnum.CC2b,
                    OntwerpLevensduur = OntwerpLevensduurEnum.Vijftig,
                },
            },
            DefaultGebruiksklasse = GebruiksklasseEnum.A_gemeenschappelijke_trappen,
        };

        var beton = project.VoegMateriaalToe(new BetonContext("C45/55"));

        var trap = new SteekTrapEntity(project.ProjectInfo)
        {
            Id = Guid.NewGuid(),
            Merk = "TR1",
            Materiaal = beton,
            MateriaalId = beton.Id,
            Gebruiksklasse = project.DefaultGebruiksklasse,
            OptredeAantal1 = 16,
            OptredeMaat = optrede,
            AantredeMaat = aantrede,
            AfwerkingVlaklast = 1.5,
            HeeftBoventand = true,
        };

        trap.SchilDikte = schildikte;
        trap.GebruikEigenLengte = true;
        trap.LengteTotaalEigenOpgave = 4000;
        trap.Init(project.ProjectInfo);

        trap.Belastingen.GenereerBelastingCombinaties(
            trap.Belastingen,
            trap.Belastingen.BelastingGevallen,
            trap.Belastingen.CombinatiesTypes);

        project.Assemblages.Add(trap);

        return (trap, project);
    }
}
