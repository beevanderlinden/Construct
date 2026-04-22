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
            DefaultGebruiksklasse = GebruiksklasseEnum.A_gemeenschappelijke_vloeren,
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
            DefaultGebruiksklasse = GebruiksklasseEnum.A_gemeenschappelijke_vloeren,
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

    /// <summary>
    /// Maakt project RD-506383 "De Bloemenkamer aan Raadhuisstraat 73" SPRANG-CAPELLE aan
    /// met trappen TR01 en TR02 en bordes BD01.
    /// BD01 wordt aan beide zijden aangesloten op TR01 respectievelijk TR02.
    /// </summary>
    public static (SteekTrapEntity tr01, SteekTrapEntity tr02, BordesEntity bd01, ProjectEntity project) CreateProject_RD506383()
    {
        var project = new ProjectEntity
        {
            ProjectInfo =
            {
                Nummer = "RD-506383",
                Naam = "De Bloemenkamer aan Raadhuisstraat 73",
                Plaatsnaam = "SPRANG-CAPELLE",
                Grondslagen =
                {
                    NationaleBijlage = NationaleBijlageEnum.NL,
                    Gevolgklasse = GevolgklasseEnum.CC2b,
                    OntwerpLevensduur = OntwerpLevensduurEnum.Vijftig,
                },
            },
            DefaultGebruiksklasse = GebruiksklasseEnum.A_gemeenschappelijke_vloeren,
        };

        var beton = project.VoegMateriaalToe(new BetonContext("C45/55"));

        // TR01: lengte=3500, op=184, aan=220, schil=150
        var tr01 = new SteekTrapEntity(project.ProjectInfo)
        {
            Id = Guid.NewGuid(),
            Merk = "TR01",
            Materiaal = beton,
            MateriaalId = beton.Id,
            Gebruiksklasse = project.DefaultGebruiksklasse,
            OptredeMaat = 184,
            AantredeMaat = 220,
            HeeftBoventand = true,
            Breedte = 1100,
        };
        tr01.SchilDikte = 150;
        tr01.GebruikEigenLengte = true;
        tr01.LengteTotaalEigenOpgave = 3500;
        tr01.Init(project.ProjectInfo);
        tr01.Belastingen.GenereerBelastingCombinaties(
            tr01.Belastingen,
            tr01.Belastingen.BelastingGevallen,
            tr01.Belastingen.CombinatiesTypes);
        
        // stel in minimaal #8-150 in voor dit project
        if (tr01.PlaatWapening is PlaatWapening pw)
        {
            if (pw.Onder?.BasisWapening is WapeningContext basis)
            {
                basis.TekstOndergrens = "r8-150";
            }
            if (pw.Onder?.VerdeelWapening is WapeningContext verdeel)
            {
                verdeel.TekstOndergrens = "r8-150";
            }
        }
        tr01.LengteBoven = tr01.AantredeMaat + tr01.WelMaat;



        project.Assemblages.Add(tr01);

        // TR02: lengte=1900, op=184, aan=220, schil=150
        var tr02 = new SteekTrapEntity(project.ProjectInfo)
        {
            Id = Guid.NewGuid(),
            Merk = "TR02",
            Materiaal = beton,
            MateriaalId = beton.Id,
            Gebruiksklasse = project.DefaultGebruiksklasse,
            OptredeMaat = 184,
            AantredeMaat = 220,
            HeeftBoventand = true,
            Breedte = 1100,
        };
        tr02.SchilDikte = 150;
        tr02.GebruikEigenLengte = true;
        tr02.LengteTotaalEigenOpgave = 1900;
        tr02.Init(project.ProjectInfo);
        tr02.Belastingen.GenereerBelastingCombinaties(
            tr02.Belastingen,
            tr02.Belastingen.BelastingGevallen,
            tr02.Belastingen.CombinatiesTypes);


        if (tr02.PlaatWapening is PlaatWapening tr02pw)
        {
            if (tr02pw.Onder?.BasisWapening is WapeningContext basis)
            {
                basis.TekstOndergrens = "r8-150";
            }
            if (tr02pw.Onder?.VerdeelWapening is WapeningContext verdeel)
            {
                verdeel.TekstOndergrens = "r8-150";
            }
        }


        project.Assemblages.Add(tr02);

        // BD01: lengte=2550, breedte=1280
        var bd01 = new BordesEntity
        {
            ProjectInfo = project.ProjectInfo,
            Id = Guid.NewGuid(),
            Merk = "BD01",
            Breedte = 1280,
            Lengte = 2550,
            Materiaal = beton,
            MateriaalId = beton.Id,
            Gebruiksklasse = project.DefaultGebruiksklasse,
            
        };

       

        bd01.Init(project.ProjectInfo);

        if (bd01.PlaatWapening?.Onder is PlaatWapeningGroep pwOnder)
        {
            pwOnder.BasisWapening.TekstOndergrens = "8-150";
            if (pwOnder.VerdeelWapening is not null)
            {
                pwOnder.VerdeelWapening.TekstOndergrens = "8-150";
            }
        }
        if (bd01.PlaatWapening?.Boven is PlaatWapeningGroep pwBoven)
        {
            pwBoven.BasisWapening.TekstOndergrens = "8-150";
            if (pwBoven.VerdeelWapening is not null)
            {
                pwBoven.VerdeelWapening.TekstOndergrens = "8-150";
            }
        }

        bd01.Trap1.AansluitendElement = tr02;
        bd01.Trap1.AansluitendElementId = tr02.Id;
        bd01.Trap1.Randafstand = 150;

        bd01.Trap2.AansluitendElement = tr02;
        bd01.Trap2.AansluitendElementId = tr02.Id;
        bd01.Trap2.Randafstand = 150;

        project.Assemblages.Add(bd01);

        return (tr01, tr02, bd01, project);
    }

    /// <summary>
    /// Maakt project RD-506807 "9 app. Rijsweg 60" MALDEN aan met trap TR01+02.
    /// Lengte=1375, Op=185, Aan=220, Schil=100, DragendeBomen=true, Breedte=900, BomenHoogte=300.
    /// </summary>
    public static (SteekTrapEntity trap, ProjectEntity project) CreateProject_RD506807()
    {
        var project = new ProjectEntity
        {
            ProjectInfo =
            {
                Nummer = "RD-506807",
                Naam = "9 app. Rijsweg 60",
                Plaatsnaam = "MALDEN",
                Grondslagen =
                {
                    NationaleBijlage = NationaleBijlageEnum.NL,
                    Gevolgklasse = GevolgklasseEnum.CC2b,
                    OntwerpLevensduur = OntwerpLevensduurEnum.Vijftig,
                },
            },
            DefaultGebruiksklasse = GebruiksklasseEnum.A_gemeenschappelijke_vloeren,
        };

        var beton = project.VoegMateriaalToe(new BetonContext("C45/55"));

        var trap = new SteekTrapEntity(project.ProjectInfo)
        {
            Id = Guid.NewGuid(),
            Merk = "TR01+02",
            Materiaal = beton,
            MateriaalId = beton.Id,
            Gebruiksklasse = project.DefaultGebruiksklasse,
            OptredeMaat = 185,
            AantredeMaat = 220,
            Breedte = 900,
            HeeftBoventand = true,
        };

        trap.SchilDikte = 100;
        trap.GebruikEigenLengte = true;
        trap.LengteTotaalEigenOpgave = 1375;
        trap.DragendeTrapBomen = true;
        trap.TrapBomenHoogte = 300;
        trap.Init(project.ProjectInfo);

        trap.Belastingen.GenereerBelastingCombinaties(
            trap.Belastingen,
            trap.Belastingen.BelastingGevallen,
            trap.Belastingen.CombinatiesTypes);

        project.Assemblages.Add(trap);

        return (trap, project);
    }
}
