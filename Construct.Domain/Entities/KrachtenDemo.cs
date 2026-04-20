using CommonLibrary.Helpers;
using Eurocode.Belastingen;
//using Kaskon.Toolbox.PrefabModels;
using Microsoft.AspNetCore.Components;

namespace Construct.Domain.Entities
{
    public class KrachtenDemo
    {
        public double Mk { get; set; }
        public double MEd { get; set; }
        public double Mfreq { get; set; }
        public double Mqp { get; set; }
        public double Vqp { get; set; }
        public double Vk { get; set; }
        public double VEd { get; set; }
        public double Vfreq { get; set; }


        public double Mbg1 { get; set; }
        public double Mbg2 { get; set; }

        public double Mk1, Mk2, Mk3;
        public double Vk1, Vk2, Vk3;
        public double MomA, MomB, MomC;
        public double DwarskrachtA, DwarskrachtB, DwarskrachtC;
        //public double DwarskrachtUgtA, DwarskrachtUgtB;

        //public double FactorG { get; set; }
        //public double FactorQ { get; set; }
        public BelastingCombinatie MaatgevendeCombinatieFundamenteel { get; set; } = default!;

        public BelastingCombinatie MaatgevendeCombinatieFrequent = default!;
        public BelastingCombinatie MaatgevendeCombinatieKarakteristiek { get; set; } = default!;


        // dit is straks niet meer nodig, bij toepassing ligger/raamwerk
        public double Gk => EigenGewicht + Afwerking;
        public double EigenGewicht { get; set; }
        public double Afwerking { get; set; }
        public double Lijnlast_qk { get; set; }
        public double Puntlast_Qk { get; set; }
        public double L { get; set; }

        public double H { get; set; } = 0;

        public double LijnlastG { get; set; }
        public double LijnlastFrequent { get; set; }
        public double LijnlastQuasiPermanent { get; set; }



        public string GetGeometrieTekst()
        {
            return $"L~t~ = {L:0.### m}";
        }

        private string GetTekst_Gk()
        {
            if (Afwerking == 0)
                return $"{Gk:0.## kN/m¹}";
            else
                return $"{EigenGewicht:0.##} + {Afwerking:0.##} = {Gk:0.##} kN/m¹";
        }
        private string GetTekst_qk()
        {
            return $"{Lijnlast_qk:0.## kN/m¹}";
        }
        private string GetTekst_Qk()
        {
            return $"{Puntlast_Qk:0.## kN}";
        }


        private string GetBelastingTekst()
        {
            List<string> results = [];

            results.Add(GetTekst_Gk());
            results.Add(GetTekst_qk());
            results.Add(GetTekst_Qk());

            //results.Add($"q~k~ = {Qk:0.## kN/m¹}");
            //results.Add($"Q~k~ = {P:0.## kN}");

            return string.Join(", ", results);
        }

        public MarkupString GetGeometrieEnBelastingMarkupString()
        {
            return MarkupHelper.ToMarkupString(GetGeometrieTekst() + ", " + GetBelastingTekst());
        }

        public MarkupString GetGeometrieMarkupString()
        {
            return MarkupHelper.ToMarkupString(GetGeometrieTekst());
        }

        public MarkupString GetBelastingMarkupString()
        {
            return MarkupHelper.ToMarkupString(GetBelastingTekst());
        }

        public List<(string naam, MarkupString)> GetBelastingMarkupStrings()
        {
            List<(string naam, MarkupString)> results = [];

            results.Add(("permanent q~g,k~", MarkupHelper.ToMarkupString(GetTekst_Gk())));
            results.Add(("veranderlijk q~q,k~", MarkupHelper.ToMarkupString(GetTekst_qk())));
            results.Add(("veranderlijk F~q,k~", MarkupHelper.ToMarkupString(GetTekst_Qk())));

            return results;
        }

    }



}
