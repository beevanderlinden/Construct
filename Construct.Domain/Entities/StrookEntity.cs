using CommonLibrary;
using CommonLibrary.Interfaces;
using Eurocode.Belastingen;
using Eurocode.BetonConstructies;
using Mechanica.SimpleBeam;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tekla.Structures.Model;
using BEAM = Mechanica.SimpleBeam;

namespace Construct.Domain.Entities
{


    public class StrookEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public BEAM.SBLigger Beam { get; set; } = new();
        public string Naam { get; set; } = "strook 1";
        
        // een strook heeft een profiel
        public Profielen.Beton.BetonProfiel Profiel { get; set; } = new(1000, 200);
        public PlaatWapening Wapening { get; set; } = new();
        



        // strook heeft een BEAM voor berekening

        // een strook gebruikt de belastingcontext van het Father object
        public AssemblageEntity? Father { get; set; }


        public void ApplyBijlegWapening()
        {
            var pw = this.PlaatWapening;

            // BendingResults
            foreach (var br in BendingResults)
            {
                if (br.AsApplied < br.AsRequired)
                {
                    // leg het verschil bij altijd
                    var bijlegReq = br.AsRequired - br.AsApplied;
                    string bijleg = "1r8";
                    if (bijlegReq < 50) bijleg = "1r8";
                    else if (bijlegReq < 100) bijleg = "2r8";
                    else if (bijlegReq < 150) bijleg = "3r8";
                    else if (bijlegReq < 200) bijleg = "3r10";
                    else if (bijlegReq < 250) bijleg = "3r12";
                    else if (bijlegReq < 300) bijleg = "3r12";
                    else if (bijlegReq < 350) bijleg = "3r16";


                   
                    if (br.Moment < 0)
                    {
                        pw.Onder.BasisWapening.Tekst = pw.Onder.BasisWapening.Tekst + $"+{bijleg}";
                        // is nu akkoord?
                        if (br.AsApplied >= br.AsRequired)
                        {
                            // akkoord
                        }
                        else
                        {
                            
                        }
                        
                    }
                        else if (br.Moment > 0)
                    {
                        pw.Boven.BasisWapening.Tekst = pw.Boven.BasisWapening.Tekst + $"+{bijleg}";
                    }
                    
                       

                }
            }
            
        }

        public void BerekenStrook()
        {
            // beam bijwerken
            Beam.LoadContext = Father?.Belastingen;
            Beam.Materiaal = Father?.Materiaal;
            

            Beam.ComputeReactions();
            Beam.Compute();

            // haal de absolute Vz op
            var v1Max = Beam.ResultCollectionLegacy.Values.Max(x => Math.Abs(x.LeftReaction));
            var v2Max = Beam.ResultCollectionLegacy.Values.Max(x => Math.Abs(x.RightReaction));
            var mMin = Beam.ResultCollectionLegacy.Values.Min(x => x.MomentDiagram.Min(p => p.M));

            // juiste krachten
            Snedekrachten.Vz = Math.Max(v1Max, v2Max);

            Snedekrachten.My = mMin;

            // bijwerken toetsen
            UpdateForceCollectionOpt(beam: Beam);
            
            
            
            UpdateWapeningOpt();
            UpdateBendingResults(); // testfase


            UpdateScheurwijdteCollectie();
            UpdateDwarskrachtCollectie();


            ApplyBijlegWapening();

            //var resultaten = new List<BeamResult>();

            //if (Father == null) return;

            //foreach (var comb in Father!.Belastingen.BelastingCombinaties)
            //{
            // hier roep je jouw methode aan
            //    var result = Beam.ComputeForCombination(comb);

            // resultaat bewaren
            //    resultaten.Add(result);
            //}

            // voorbeeld: tonen
            //foreach (var r in resultaten)
            //{
            //var mMin = r.MomentDiagram.Min(m=>m.M);
            //var mMax = r.MomentDiagram.Max(m=>m.M);

            //var mMin = r.MomentDiagram.Aggregate((a, b) => a.M < b.M ? a : b);
            //var mMax = r.MomentDiagram.Aggregate((a, b) => a.M > b.M ? a : b);


            //Console.WriteLine($"{r.CombinationName} → Mmin = {mMin.M:0.0} (x= {mMin.x:0.000}, " +
            //    $"Mmax = {mMax.M:0.0} (x={mMax.x:0.000})");
            //}

            // veldmoment Frequent

            //var mFreqEdEntry = resultaten. 
            //var frequentResults = resultaten
            //    .Where(r => r.Combination.Type == BelastingCombinatieTypeEnum.Frequent); 

            // Stap 2: pak alle MomentDiagram entries
            //var allMoments = frequentResults
            //    .SelectMany(r => r.MomentDiagram)
            //    .ToList();

            // Stap 3: vind minimale moment + positie
            //var mFreqEdEntry = allMoments.Aggregate((a, b) => a.M < b.M ? a : b);

            //Console.WriteLine($"M Frequent Ed: M = {mFreqEdEntry.M:0.0} kNm op x = {mFreqEdEntry.x:0.000}");



        }





        public List<SectionForcesAtPositie> ForceCollection { get; set; } = [];
        public List<SectionForcesAtPositie> ForceCollectionFrequent { get; set; } = [];

        public SectionForces Snedekrachten = new();


        public IEnumerable<IEurocodeContext> Toetsen { get; set; } = [];

        [Obsolete("gebruik collectie")]
        public DwarskrachtWapContext BetonV { get; set; } = new();

        public List<DwarskrachtWapContext> DwarskrachtCollectie { get; set; } = [];
        public List<BendingResults> BendingResults { get; set; } = [];

        public List<ScheurwijdteContext> ScheurwijdteCollectie { get; set; } = [];

        public DoorbuigingValidatieContext Doorbuiging { get; set; }  = new();



        public BendingResults BetonM { get; set; } = new();
        public ScheurwijdteContext BetonSw { get; set; } = new();
        

        [Obsolete("vervang door PlaatWapening")]
        public WapeningContext WapBoven { get; set; } = new();
        [Obsolete("vervang door PlaatWapening")]

        public WapeningContext WapOnder { get; set; } = new();

        /// <summary>
        /// Plaatwapening van de strook
        /// Bevat Boven en Onder wapening
        /// Voor beide zijdes is er hoofd+verdeelwapening 
        /// </summary>
        public PlaatWapening PlaatWapening { get; set; } = new();





        

        public void UpdateWapeningOpt()
        {
            if (Father == null) 
                return;

            WapBoven.ReferentieDekking = Father.PlaatDekking.Boven.DekkingToe;
            WapBoven.Tekst = "r6-150";
            
            WapOnder.ReferentieDekking = Father.PlaatDekking.Onder.DekkingToe;
            WapOnder.Tekst = "r8-150";

            PlaatWapening.Boven ??= new();
            PlaatWapening.Onder ??= new();
            

            //PlaatWapening.Boven.DekkingBuitensteLaag = Father.PlaatDekking.Boven;
            //PlaatWapening.Boven.BasisWapening.Tekst = "r6-150";
            //PlaatWapening.Boven.VerdeelWapening = null;
            //PlaatWapening.Onder.DekkingBuitensteLaag = Father.PlaatDekking.Onder;
            //PlaatWapening.Onder.BasisWapening.Tekst = "r8-150";
            //PlaatWapening.Onder.VerdeelWapening = null;




        }


        
       
        
        public void UpdateForceCollectionOpt(SBLigger beam)
        {
            ForceCollection.Clear();
            ForceCollectionFrequent.Clear();
            var vA = beam.ResultCollectionLegacy.Values.Select(r => Math.Abs(r.LeftReaction)).ToList();
            var vB = beam.ResultCollectionLegacy.Values.Select(r => Math.Abs(r.RightReaction)).ToList();

            // GROOTSTE NEGATIEVE MOMENT + POSITIE
            var minMomentEntry = beam.ResultCollectionLegacy.Values
                .SelectMany(r => r.MomentDiagram)
                .Aggregate((a, b) => a.M < b.M ? a : b);

            // Punt C (grootste veldmoment)
            SectionForces fC = new(my: minMomentEntry.M);
            ForceCollection.Add(new(fC, minMomentEntry.x));

            // Punt A (0)
            var mA = beam.ResultCollectionLegacy.Values.Select(r => r.MomentDiagram.First().M).Max();
            var mToev = Math.Abs(.15 * -minMomentEntry.M);
            if (beam.StartSupport == SupportType.Pin)
                mA = Math.Max(mA, mToev);
            
           
            SectionForces fA = new(my: mA, vz: vA.Max());
            ForceCollection.Add(new(fA, 0));

            // Punt B (lengte)
            var mB = beam.ResultCollectionLegacy.Values.Select(r => r.MomentDiagram.Last().M).Max();
            if (beam.EndSupport == SupportType.Pin)
                mB = Math.Max(mB, mToev);

            
            SectionForces fB = new(my: mB, vz: vB.Max());
            ForceCollection.Add(new(fB, beam.Length));


            // GROOTSTE FREQUENTE MOMENT
            var mFreqEdEntry = beam.ResultCollectionLegacy.Values
                .Where(x=>x.Combination.Type == BelastingCombinatieTypeEnum.Frequent)
                .SelectMany(r => r.MomentDiagram)
                .Aggregate((a, b) => a.M < b.M ? a : b);
            SectionForces fMFr = new(my: mFreqEdEntry.M);
            ForceCollectionFrequent.Add(new(fMFr, mFreqEdEntry.x));

        }
        


        public void UpdateBendingResults()
        {
            if (Father == null) 
                return;

            var beton = Father.Materiaal as BetonContext;

            BendingResults.Clear();
            foreach (var fx in ForceCollection.OrderBy(fc=>fc.Pos))
            {
                BendingResults br = new()
                {
                    Beton = beton ?? new(),
                    Profiel = this.Profiel,
                    PosLabel = fx.Pos.ToString("0.000", CultureInfo.InvariantCulture),
                    //br.PosLabelVisible = true;
                    Wapening = fx.Forces.My < 0 ? PlaatWapening.Onder.BasisWapening : PlaatWapening.Boven.BasisWapening,
                    Snedekrachten = fx.Forces
                };
                BendingResults.Add(br);
            }
        }
        public void UpdateDwarskrachtCollectie()
        {
            if (Father == null) return;
            var beton = Father.Materiaal as BetonContext;
            DwarskrachtCollectie.Clear();
            foreach (var fx in ForceCollection.OrderBy(fc=>fc.Pos))
            {
                DwarskrachtWapContext dw = new()
                {
                    Beton = beton ?? new(),
                    Snedekrachten = fx.Forces,
                    PosLabel = fx.Pos.ToString("0.000", CultureInfo.InvariantCulture),
                    Profiel = this.Profiel,
                    AsLangs = this.WapOnder.As,
                    NutHoogte = this.Profiel.Hoogte - this.WapOnder.ZRef,
                    //PosLabel = fx.Pos.ToString("0.000", CultureInfo.InvariantCulture),

                };
                dw.BerekenEnValideer();
                DwarskrachtCollectie.Add(dw);
            }
        }



        public void UpdateScheurwijdteCollectie()
        {
            if (Father == null) return;

            var beton = Father.Materiaal as BetonContext;

            ScheurwijdteCollectie.Clear();
            foreach (var fx in ForceCollectionFrequent)
            {
                ScheurwijdteContext sw = new()
                {
                    Beton = beton ?? new(),
                    Wapening = WapOnder,
                    Snedekrachten = fx.Forces,
                    Profiel = this.Profiel,
                    Dekking = this.Father.PlaatDekking.Onder,
                    PosLabel = fx.Pos.ToString("0.000", CultureInfo.InvariantCulture),
                    
                };

                sw.BerekenEnValideer();
                ScheurwijdteCollectie.Add(sw);

            }
        }





        



        




    }


    public class SectionForcesAtPositie
    {
        public SectionForces Forces { get; set; }
        public double Pos { get; set; }

        public SectionForcesAtPositie(SectionForces forces, double pos)
        {
            Forces = forces;
            Pos = pos;
        }
    }

   
    


    public class DekkingContext : BaseEurocodeContext
    {
        public event Action? OnChanged;

        private BetonDekkingContext _onder = new();
        public BetonDekkingContext Onder
        {
            get => _onder;
            set { _onder = value; OnChanged?.Invoke(); }
        }

        private BetonDekkingContext _boven = new();
        public BetonDekkingContext Boven
        {
            get => _boven;
            set { _boven = value; OnChanged?.Invoke(); }
        }

        protected override void Bereken()
        {

        }

        protected override bool Valideer()
        {
            if (!Onder.BerekenEnValideer())
                return false;

            if (!Boven.BerekenEnValideer())
                return false;
            
            
            return true;
        }
    }

}
