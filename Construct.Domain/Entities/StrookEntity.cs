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

        /// <summary>
        /// Factor voor toevallige inklemming aan het begin (oplegging A).
        /// Standaard 0.15 (= 15% van het veldmoment).
        /// </summary>
        public double ToevalligeInklemmingBegin { get; set; } = 0.15;

        /// <summary>
        /// Factor voor toevallige inklemming aan het einde (oplegging B).
        /// Standaard 0.15 (= 15% van het veldmoment).
        /// </summary>
        public double ToevalligeInklemmingEind { get; set; } = 0.15;
        
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
                        if (pw.Onder?.BasisWapening != null) pw.Onder.BasisWapening.Tekst = pw.Onder.BasisWapening.Tekst + $"+{bijleg}";
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
                        if (pw.Boven?.BasisWapening != null) pw.Boven.BasisWapening.Tekst = pw.Boven.BasisWapening.Tekst + $"+{bijleg}";
                    }
                    
                       

                }
            }
            
        }

        public void BerekenStrook()
        {
            // beam bijwerken
            Beam.LoadContext = Father?.Belastingen;
            Beam.Materiaal = Father?.Materiaal;
            Beam.AccidentalFixityFactor = Math.Max(ToevalligeInklemmingBegin, ToevalligeInklemmingEind);

            Beam.ComputeReactions();
            Beam.Compute();


            // bijwerken toetsen
            UpdateForceCollectionOpt(beam: Beam);

            if (ForceCollection.Count == 0) return;

            // juiste krachten
            Snedekrachten.Vz = Math.Max(
                ForceCollection.Max(f=>f.Forces.Vz),
                Math.Abs(ForceCollection.Min(f=>f.Forces.Vz))
                );
            Snedekrachten.My = ForceCollection.Min(fc=>fc.Forces.My);

            UpdateWapeningOpt();
            UpdateBendingResults(); // testfase
            UpdateScheurwijdteCollectie();
            UpdateDwarskrachtCollectie();

            // TODO : Generieke aanpak bedenken voor het bijwerken van de wapening in zowel stroken als platen,
            // gebaseerd op de resultaten van de berekeningen en de vereisten van de wapening.
            // Idee: 
            // - Gebruik dezelfde PlaatWapening class voor zowel stroken als platen,
            // zodat we een uniforme structuur hebben voor het opslaan van wapeninginformatie.
            // - Bijvooorbeeld:
            //   - Plaat met 3 stroken (parallel):
            //      - elke strook heeft dezelfde PlaatWapening.
            //      - elke strook heeft een WapeningContext specifiek voor die strook.
            //          - strook 1: (basisstrook)
            //          - strook 2: (basisstrook + bijleg)
            //          - strook 3: (basisstrook + bijleg)
            //      

            // Maak een methode UpdateWapening() die zowel in StrookEntity als in PlaatEntity kan worden gebruikt.


            // DEBUG:
            // Hoe werkt het nu in BordesEntity:
            // - Basisstrook.Wapening wordt in de stroken gestopt.
            // - Ergens in bordes wordt de wapening bijgewerkt..
            // - uitzoeken hoe dit voor alle platen kan werken met in strook en plaat.
            // - virtual methode? die eventueel override heeft in concrete class zoals bijvoorbeeld Galerij/Balkon??

            // Stappenplan -> laat CP uitzoeken hoe nu werkt, wat beter kan en implementeer dit.

            // Daarna laat CP uizoeken hoe we BordesEnitity kunnen laten afstammen van PlaatEntity. 
            // Idee: Maak BordesplaatEntity (nieuw) 





            //ApplyBijlegWapening();

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

            // ✅ Check of Father een BetonAssemblageEntity is (alleen beton heeft PlaatDekking)
            if (Father is BetonAssemblageEntity betonFather)
            {
                //WapBoven.ReferentieDekking = betonFather.PlaatDekking.Boven.DekkingToe;
                //WapBoven.Tekst = "r6-150";
                
                //WapOnder.ReferentieDekking = betonFather.PlaatDekking.Onder.DekkingToe;
                //WapOnder.Tekst = "r8-150";
            }

            PlaatWapening.Boven ??= new();
            PlaatWapening.Onder ??= new();
            

            // Hoe dit werkt
            // this.Father is bijvoorbeeld een BetonAssemblage
            // this.PlaatWapening is bekend
                // NB. Mogelijk een reference (dus wijzigt ook de Father) of clone (geen wijziging)


            // 

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

            var fundType = BelastingCombinatieTypeEnum.Fundamenteel_A | BelastingCombinatieTypeEnum.Fundamenteel_B;
            var fundPunten = GetEnvelopePunten(beam, fundType);
            if (fundPunten.Count == 0)
                return;

            // Toevallige inklemming: gebaseerd op het min moment uit de envelop
            var mC = fundPunten.Min(p => p.Forces.My);
            var mToevBegin = Math.Abs(ToevalligeInklemmingBegin * -mC);
            var mToevEind  = Math.Abs(ToevalligeInklemmingEind  * -mC);

            // Punt A (x=0): corrigeer bij scharnierend begin
            var puntA = fundPunten.First(p => p.Pos == 0);
            if (beam.StartSupport == SupportType.Pin)
                puntA = new(new SectionForces(my: Math.Max(puntA.Forces.My, mToevBegin), vz: puntA.Forces.Vz), 0);

            // Punt B (x=L): corrigeer bij scharnierend einde
            var puntB = fundPunten.First(p => p.Pos == beam.Length);
            if (beam.EndSupport == SupportType.Pin)
                puntB = new(new SectionForces(my: Math.Max(puntB.Forces.My, mToevEind), vz: puntB.Forces.Vz), beam.Length);

            ForceCollection.Add(puntA);
            ForceCollection.Add(puntB);

            // Punt C: veldmoment op tussengelegen positie (indien aanwezig in envelop)
            var puntC = fundPunten.FirstOrDefault(p => p.Pos != 0 && p.Pos != beam.Length);
            if (puntC != null)
                ForceCollection.Add(puntC);

            // Frequente combinaties (zonder toevallige inklemming)
            ForceCollectionFrequent.AddRange(GetEnvelopePunten(beam, BelastingCombinatieTypeEnum.Frequent));
        }

        /// <summary>
        /// Haalt de maatgevende snedekrachten op uit de ResultCollection voor het opgegeven combinatietype.
        /// Geeft altijd punt A (x=0) en punt B (x=L) terug met extreme waarden.
        /// Geeft ook punt C terug als het minimum moment op een andere positie ligt.
        /// </summary>
        private static List<SectionForcesAtPositie> GetEnvelopePunten(SBLigger beam, BelastingCombinatieTypeEnum type)
        {
            var punten = new List<SectionForcesAtPositie>();
            var results = beam.ResultCollection.ForCombinationType(type).ToList();

            if (results.Count == 0)
                return punten;


            var shearColA = results.Select(r => r.ShearDiagram.FirstOrDefault(p => p.x == 0)).ToList();
            var momColA = results.Select(r => r.MomentDiagram.FirstOrDefault(p => p.x == 0)).ToList();
            var momColB = results.Select(r => r.MomentDiagram.FirstOrDefault(p => p.x == beam.Length)).ToList();


            // Punt A (x=0): maatgevend moment + dwarskracht aan het begin
            var mA = momColA.Max(p => p.M);
            var vzA = results.Max(r => Math.Abs(r.LeftReaction));
            punten.Add(new(new SectionForces(my: mA, vz: vzA), 0));

            // Punt B (x=L): maatgevend moment + dwarskracht aan het einde


            var mB  = momColB.Max(p => p.M);
            var vzB = results.Max(r => Math.Abs(r.RightReaction));
            punten.Add(new(new SectionForces(my: mB, vz: vzB), beam.Length));

            // Punt C: minimum moment op een tussengelegen positie
            var allMoments = results.SelectMany(r => r.MomentDiagram).ToList();
            if (allMoments.Count == 0)
                return punten;

            var minEntry = allMoments.MinBy(md => md.M);
            bool isAtA   = Math.Abs(minEntry.x) < 1e-6;
            bool isAtB   = Math.Abs(minEntry.x - beam.Length) < 1e-6;

            if (!isAtA && !isAtB && Math.Abs(minEntry.M) > 0.001)
            {
                var src = results.First(r => r.MomentDiagram.Any(md => Math.Abs(md.x - minEntry.x) < 1e-6));
                var (vzC, _) = src.ShearAt(minEntry.x);
                punten.Add(new(new SectionForces(my: minEntry.M, vz: vzC), minEntry.x));
            }

            return [.. punten.OrderBy(p=>p.Pos)];
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
                    Wapening = fx.Forces.My < 0 ? PlaatWapening?.Onder?.BasisWapening ?? new() : PlaatWapening?.Boven?.BasisWapening ?? new(),
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
#pragma warning disable CS0618
                    AsLangs = this.WapOnder.As,
                    NutHoogte = this.Profiel.Hoogte - this.WapOnder.ReferentieAfstand,
#pragma warning restore CS0618
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

            // ✅ Check of Father een BetonAssemblageEntity is
            var betonFather = Father as BetonAssemblageEntity;

            ScheurwijdteCollectie.Clear();
            foreach (var fx in ForceCollectionFrequent)
            {
                if (Math.Abs(fx.Forces.My) < 1e-6)
                    continue;

                var wap = WapOnder;
                if (fx.Forces.My > 0)
                {
                    wap = WapBoven;
                }
               


                ScheurwijdteContext sw = new()
                {
                    Beton = beton ?? new(),
                    #pragma warning disable CS0618
                                        Wapening = wap,
                    #pragma warning restore CS0618
                    Snedekrachten = fx.Forces,
                    Profiel = this.Profiel,
                    Dekking = betonFather?.PlaatDekking.Onder ?? new(),
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
        public override string Heading { get; set; } = "Dekking/Duurzaamheid";
        public bool IsInitialized { get; set; } = false;
        #pragma warning disable CS0067
                public event Action? OnChanged;
        #pragma warning restore CS0067

        private BetonDekkingContext? _onder = new();
        public BetonDekkingContext Onder
        {
            get => _onder ?? new();
            set => SetNestedProperty(ref _onder, value);
        }

        private BetonDekkingContext? _boven = new();
        public BetonDekkingContext Boven
        {
            get => _boven ?? new();
            set => SetNestedProperty(ref _boven, value);
        }

        public override void Init()
        {
            // Subscribe to child property changes
            if (_onder != null)
            {
                _onder.Init();
            }
            if (_boven != null)
            {
                _boven.Init();
            }
            
            base.Init(); // Roept SubscribeAllNestedProperties aan
        }

        protected override void Bereken()
        {
            // Bereken children eerst
            _onder?.BerekenEnValideer();
            _boven?.BerekenEnValideer();
        }

        protected override bool Valideer()
        {
            // Check de IsValidated property van children
            if (_onder != null && !_onder.IsValidated)
            {
                AddMeldingWaarschuwing("Dekking onder niet akkoord");
                return false;
            }

            if (_boven != null && !_boven.IsValidated)
            {
                AddMeldingWaarschuwing("Dekking boven niet akkoord");
                return false;
            }

            return true;
        }
    }

}
