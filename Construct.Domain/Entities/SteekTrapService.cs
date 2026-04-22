using Eurocode.Belastingen;
//using Kaskon.Toolbox.PrefabModels;

namespace Construct.Domain.Entities
{
    public class SteekTrapService
    {
        public KrachtenDemo GetDemoKrachten(SteekTrapEntity steekTrap, string onderdeel = "schil")
        {

            double eg = steekTrap.GetGk();
            double qG = steekTrap.GetPermanenteBelasting();
            if (steekTrap.GebruikEigenOpgaveVoorEigenGewicht)
            {
                // gebruik opgave gebruiker
                qG = steekTrap.EigenGewichtPerM2;
            }
            else
            {
                // geen opgave -> controleer of eg gewijzigd is
                if (eg != steekTrap.EigenGewichtPerM2)
                    steekTrap.EigenGewichtPerM2 = eg;
            }
            double lijnlast_qk = 1;
            double puntlast_Qk = 1;
            double L = 1;
            double H = 1;
            double qAfw = 0;

            switch (onderdeel)
            {
                default:
                case "schil":
                    qAfw = steekTrap.AfwerkingVlaklast;
                    lijnlast_qk = steekTrap.GetOpgelegdeBelasting()?.Vlaklast ?? 10;
                    puntlast_Qk = steekTrap.GetOpgelegdeBelasting()?.Puntlast ?? 10;
                    L = steekTrap.LtProjZ / 1000.0;
                    H = steekTrap.HoogteTotaal / 1000.0;
                    break;
                    case "boom":
                    // hier de boombelasting berekenen
                    qAfw = steekTrap.AfwerkingVlaklast;
                    lijnlast_qk = steekTrap.GetOpgelegdeBelasting()?.Vlaklast ?? 10;
                    puntlast_Qk = steekTrap.GetOpgelegdeBelasting()?.Puntlast ?? 10;
                    L = steekTrap.LtProjZ / 1000.0;
                    H = steekTrap.HoogteTotaal / 1000.0;
                    // pas de belastingen 
                    double belastingBreedteMeter = steekTrap.Breedte / 2.0 / 1000.0;
                    qG *= belastingBreedteMeter;
                    lijnlast_qk *= belastingBreedteMeter;
                    break;
                case "spiegel":
                    // hier de spiegelbelasting berekenen
                    qAfw = steekTrap.AfwerkingVlaklast;
                    lijnlast_qk = steekTrap.GetOpgelegdeBelasting()?.Vlaklast ?? 10;
                    puntlast_Qk = steekTrap.GetOpgelegdeBelasting()?.Puntlast ?? 10;
                    L = (steekTrap.Breedte - steekTrap.TrapBoomBreedte) / 1000.0;
                    H = 0;
                    break;
            }

            var krachten = GetKrachtenDemo(-qG, -lijnlast_qk, -puntlast_Qk, L, -qAfw, steekTrap.Belastingen, H);

            // bijwerken
            steekTrap.UpdateSnedekrachten(krachten.MEd, krachten.Mfreq, krachten.VEd);


           
            
            steekTrap.Krachten = krachten;


            return krachten;

        }


        public KrachtenDemo GetKrachtenDemo(double qG, double lijnlast_qk, double puntlast_Qk, double L, double qAfw, BelastingenContext belastingenContext, double H = 0)
        {
            KrachtenDemo returnItem = new();
            double a = 0.5 * L;

            // in het midden 
            var vergeetMijNietje1 = SteekTrapExtensions.GetVergeetMeNietje(SteekTrapExtensions.VergeetMeNietje.VrijVrijLijnlast, qG, L, a);
            var vergeetMijNietje2 = SteekTrapExtensions.GetVergeetMeNietje(SteekTrapExtensions.VergeetMeNietje.VrijVrijLijnlast, lijnlast_qk, L, a);
            var vergeetMijNietje3 = SteekTrapExtensions.GetVergeetMeNietje(SteekTrapExtensions.VergeetMeNietje.VrijVrijPuntlast, puntlast_Qk, L, a);
            var momentaanFactoren = belastingenContext?.BelastingGevallen?.FirstOrDefault(bg => bg.Type == BelastingGeval.BelastingGevalTypeEnum.Veranderlijk)?.MomentaanFactoren;

            var mk1 = vergeetMijNietje1.m;
            var mk2 = vergeetMijNietje2.m;
            var mk3 = vergeetMijNietje3.m;

            var momBg1 = mk1;
            var momBg2 = Math.Min(mk2, mk3); // meest negatief (sagging) is maatgevend

            var mk = vergeetMijNietje1.m + Math.Min(vergeetMijNietje2.m, vergeetMijNietje3.m); // meest negatief
            //mk = 0.0;

            var mfreq = 0.0;
            var mQp = 0.0;
            var vfreq = 0.0;
            var vQp = 0.0;

            var momA = 0.0;
            var momB = 0.0;
            var momC = 0.0;
            var dwaA = 0.0;
            var dwaB = 0.0;
            var dwaC = 0.0;

            var vk1 = -qG * L / 2;           // positief bij neerwaartse (negatieve) belasting
            var vk2 = -lijnlast_qk * L / 2;  // positief bij neerwaartse (negatieve) belasting
            var vk3 = -puntlast_Qk;           // positief bij neerwaartse (negatieve) belasting
            var vk = vk1 + Math.Max(vk2, vk3);
            vk = 0.0;

            var md = 0.0;
            var vd = 0.0;
            var bcOrdered = (belastingenContext?.BelastingCombinaties ?? []).OrderBy(c => c?.Type).ToList();
            var lastType = bcOrdered.FirstOrDefault()?.Type ?? default;
            foreach (var bc in bcOrdered)
            {
                if (bc.Type != lastType)
                {

                    momA = 0.0;
                    momB = 0.0;
                    momC = 0.0;
                    dwaA = 0.0;
                    dwaB = 0.0;
                    dwaC = 0.0;


                }
                var mom = 0.0;
                var dwa = 0.0;
                foreach (var item in bc.Items)
                {
                    if (item.Geval.Nr == 1)
                    {
                        mom += vergeetMijNietje1.m * item.FactorNetto;
                        dwa += vk1 * item.FactorNetto;
                    }
                    else if (item.Geval.Nr == 2)
                    {
                        mom += Math.Min(vergeetMijNietje2.m, vergeetMijNietje3.m) * item.FactorNetto; // meest negatief
                        dwa += Math.Max(vk2, vk3) * item.FactorNetto; // grootste reactie
                    }
                }

                switch (bc.Type)
                {
                    case BelastingCombinatieTypeEnum.Fundamenteel_A:
                        if (mom < momA) momA = mom; // meest negatief
                        if (dwa > dwaA) dwaA = dwa;
                        if (mom <= md)
                        {
                            md = mom;
                            returnItem.MaatgevendeCombinatieFundamenteel = bc;
                        }
                        break;
                    case BelastingCombinatieTypeEnum.Fundamenteel_B:
                        if (mom < momB) momB = mom; // meest negatief
                        if (dwa > dwaB) dwaB = dwa;
                        if (mom <= md)
                        {
                            md = mom;
                            returnItem.MaatgevendeCombinatieFundamenteel = bc;
                        }
                        break;
                    case BelastingCombinatieTypeEnum.Karakteristiek:
                        if (mom <= mk)
                        {
                            mk = mom;
                            returnItem.MaatgevendeCombinatieKarakteristiek = bc;
                        }
                        break;
                    case BelastingCombinatieTypeEnum.Frequent:
                        if (mom < momC) momC = mom; // meest negatief
                        if (dwa > dwaC) dwaC = dwa;
                        if (mom <= mfreq)
                        {
                            mfreq = mom;
                            returnItem.MaatgevendeCombinatieFrequent = bc;
                        }
                        if (dwa >= vfreq)
                        {
                            vfreq = dwa;
                        }
                        break;
                    case BelastingCombinatieTypeEnum.QuasiBlijvend:
                        if (mom < momC) momC = mom; // meest negatief
                        if (dwa > dwaC) dwaC = dwa;
                        if (mom <= mQp)
                        {
                            mQp = mom;
                            returnItem.MaatgevendeCombinatieFrequent = bc;
                        }
                        if (dwa >= vQp)
                        {
                            vQp = dwa;
                        }
                        break;
                }



                if (dwa > vd)
                    vd = dwa;

                lastType = bc.Type;
            }



            returnItem.Mk = mk;       // negatief = sagging
            returnItem.MEd = md;      // negatief = sagging
            returnItem.Mfreq = mfreq; // negatief = sagging
            returnItem.Mqp = mQp;     // negatief = sagging

            returnItem.Vk = vk;
            returnItem.VEd = vd;      // positief (reactie omhoog)
            returnItem.Vfreq = vfreq;
            returnItem.Vqp = vQp;


            returnItem.Mk1 = mk1; // negatief = sagging
            returnItem.Mk2 = mk2;
            returnItem.Mk3 = mk3;

            returnItem.Vk1 = vk1; // positief bij neerwaartse belasting
            returnItem.Vk2 = vk2;
            returnItem.Vk3 = vk3;

            returnItem.MomA = momA; // negatief = sagging
            returnItem.MomB = momB;
            returnItem.MomC = momC;

            returnItem.DwarskrachtA = dwaA;
            returnItem.DwarskrachtB = dwaB;
            returnItem.DwarskrachtC = dwaC;


            returnItem.EigenGewicht = -(qG - qAfw);  // positief voor leesbaarheid.
            returnItem.Afwerking = -qAfw;            // positief voor leesbaarheid.       

            returnItem.Lijnlast_qk = -lijnlast_qk;   // positief voor leesbaarheid.
            returnItem.Puntlast_Qk = -puntlast_Qk;   // positief voor leesbaarheid.
            returnItem.L = L;
            returnItem.H = H;

            returnItem.LijnlastG = -qG;                // negatief = neerwaarts
            returnItem.LijnlastFrequent = -(qG + lijnlast_qk * momentaanFactoren?.Mom1 ?? 1);
            returnItem.LijnlastQuasiPermanent = -(qG + lijnlast_qk * momentaanFactoren?.Mom2 ?? 1);
            
            returnItem.Mbg1 = momBg1;
            returnItem.Mbg2 = momBg2;


            return returnItem;

        }
    }



}
