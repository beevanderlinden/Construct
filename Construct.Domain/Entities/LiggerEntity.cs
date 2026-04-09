using CommonLibrary.Interfaces;
using CommonLibrary.Models;
using Eurocode.HoutConstructies;
using Eurocode.StaalConstructies;
using Kaskon.Toolbox.PrefabModels;
using Mechanica.LiggerSB;
using Profielen.Staal;
using System.Text.Json.Serialization;
//using Kaskon.Toolbox.PrefabModels;

//using Mechanica.LiggerSB;

namespace Construct.Domain.Entities
{
    public class VrijRolEntity : AssemblageEntity
    {
        // test voor vrij opgegelde ligger (met rol aan einde)
        // geen combinaties
        // geen gevallen
        // alleen maar een lijnlast en/of puntlast
        // 1 profiel
        // 1 materiaal
        // resultaten: V, M, θ en w op verschillende posities (max moment, max doorbuiging, etc.)

        VgmVrijLijnlast Vgm { get; set; } = new(10, 2, 1, 1);






    }


    public class LiggerEntity : AssemblageEntity
    {
        public LiggerEntity()
        {
            // stalen ligger
            this.AssemblageType = AssemblageTypeEnum.StaalAssemblage;
            // ⚠️ REMOVED: this.Materiaal = new StaalContext() { StaalKwaliteit = StaalKwaliteitEnum.S235};
            // Materiaal zal worden ingesteld via JSON-deserialisatie of AddAssemblage.razor
            
            this.Naam = "stalen ligger";
            this.Merk = "SL-?";


            Profiel = new ProfielIH(Doorsneden.HEA200);
            
            // Let op! belastingcombinatie-generator moet nog worden aangezet
            this.Belastingen.GenereerBelastingCombinaties(
                this.Belastingen, 
                this.Belastingen.BelastingGevallen, 
                this.Belastingen.CombinatiesTypes);

            // BEAM
            Beam = new()
            {
                Length = 4.0,
                LoadContext = this.Belastingen,
                Profiel = this.Profiel,
                Materiaal = this.Materiaal,  // Kan null zijn totdat Materiaal wordt ingesteld
                
                EI = this.Profiel.Iy * 1e-12 * (this.Materiaal?.E ?? 210e3) * 1e3,  // Default E als Materiaal null
            };

            // 
            //Beam.Profiel.Materiaal = new StaalContext();

            Beam.Loads.Add(
                new Mechanica.SimpleBeam.DistributedLoad(
                    this.Belastingen.BelastingGevallen.FirstOrDefault(),
                    "DL1g", 0, 4.0, -10));

            Beam.Loads.Add(
               new Mechanica.SimpleBeam.DistributedLoad(
                   this.Belastingen.BelastingGevallen.LastOrDefault(),
                   "DL1q", 0, 4.0, -5));




            Beam.Compute();

            


        }

        // properties
        /// <summary>
        /// De BEAM is de rekenkern van de ligger
        /// geen serialisatie omdat deze in de berekening wordt opgebouwd
        /// geen snede-toetsen e.d. alleen mechica.
        /// </summary>
        public Mechanica.SimpleBeam.SBLigger Beam { get; set; }

        [JsonIgnore]
        public CommonLibrary.Models.BaseProfiel Profiel { get; set; }


        public List<EurocodeResultaat> EurcodeResultaten { get; set; } = [];





        
        // eigen implementaties 
        public override void Bijwerken()
        {
            // voorlopig even alles bijwerken (dit zou niet meer hoeven straks, controleer dit...)
            //this.Profiel.Materiaal = this.Materiaal;
            this.Beam.Profiel = this.Profiel;



            this.Beam.Compute();

            // ligger 
            var maxMoment = Beam.ResultCollection.GetMaxMoment();
            var forcesMyMax = maxMoment?.Forces ?? new CommonLibrary.Models.InternalForces { My = 0 };
            
            var maxShear = Beam.ResultCollection.GetMaxShear();
            var forcesVz = maxShear?.Forces ?? new CommonLibrary.Models.InternalForces { Vz = 0 };



            // vul de toetsen in op basis van de BEAM resultaten en materiaal
            if (Beam.Profiel is BaseProfiel houtProfiel && Beam.Materiaal is HoutContext hout)
            {
                this.EurcodeResultaten.Clear();
                
                // Gebruik Middellang als default belastingsduur
                var houtMy = new Eurocode.HoutConstructies.BendingMyToets(BelastingsduurKlasse.Middellang)
                {
                    Positie = $"{(maxMoment?.Position ?? 0).ToString("F3")}"
                };
                this.EurcodeResultaten.Add(houtMy.Check(forcesMyMax, houtProfiel, hout));


                var houtVz = new Eurocode.HoutConstructies.ShearVzCheck(BelastingsduurKlasse.Middellang)
                {
                    Positie = $"{(maxShear?.Position ?? 0).ToString("F3")}"
                };
                this.EurcodeResultaten.Add(houtVz.Check(forcesVz, houtProfiel, hout));
            }


            if (Beam.Profiel is IStaalProfiel staalProfiel && Beam.Materiaal is StaalContext staal)
            {
                // ik wil dit hieronder in de Eurocode.StaalConstructies namespace zetten
                // GenereerToetsenVoorLigger
                // later uitbreiden met hout.
                // beton is een andere case omdat dat meestal in wapeningsstaal wordt getoetst.
                // hier even een voorbeeld van 1 toets:

                // wis bestaande
                this.EurcodeResultaten.Clear();

                var toetsMy = new Eurocode.StaalConstructies.BendingMyToets(sectionClass: 1)
                {
                    Positie = $"{(maxMoment?.Position ?? 0).ToString("F3")}"
                };
                this.EurcodeResultaten.Add(toetsMy.Check(forcesMyMax, staalProfiel, staal));

                // Vz
                var toetsVz = new Eurocode.StaalConstructies.ShearVzCheck() 
                { 
                    Positie = $"{(maxShear?.Position ?? 0).ToString("F3")}"
                };

                this.EurcodeResultaten.Add(toetsVz.Check(forcesVz, staalProfiel, staal));
            }
        }


    }
}
