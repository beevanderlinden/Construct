using Construct.Domain.Entities.Parts;
using Eurocode.BetonConstructies;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Construct.Domain.Entities
{
    /// <summary>
    /// Beton-specifieke assemblage met betondekking en wapening.
    /// Gebruikt voor SteekTrapEntity, BordesEntity, etc.
    /// Ondersteunt compositie via MainPart (plaat/balk) en optionele SubParts.
    /// </summary>
    public abstract class BetonAssemblageEntity : AssemblageEntity
    {
        /// <summary>
        /// Hoofdonderdeel van deze assemblage (meestal een plaat of balk).
        /// NIEUW: Part-based architectuur voor flexibele compositie.
        /// </summary>
        public ConstructionPart? MainPart { get; set; }

        /// <summary>
        /// Sub-onderdelen (randbalken, versterkingen, etc.).
        /// NIEUW: Ondersteunt toekomstige uitbreiding met balken en versterkingen.
        /// </summary>
        public List<ConstructionPart> SubParts { get; set; } = [];

        /// <summary>
        /// Dekking context voor dekking en duurzaamheid aan de onderzijde en bovenzijde van een beton-element.
        /// BELANGRIJK: Deze wordt gedeeld met MainPart indien PlatePart!
        /// ⚠️ Alleen relevant voor betonconstructies!
        /// </summary>
        public DekkingContext PlaatDekking { get; set; } = new();

        /// <summary>
        /// Het betonmateriaal van dit assemblage.
        /// Cast van base.Materiaal naar BetonContext voor type-safety.
        /// </summary>
        [JsonIgnore]
        public BetonContext? Beton
        {
            get => Materiaal as BetonContext;
            set => Materiaal = value;
        }


       

        /// <summary>
        /// ✅ Initialiseer PlaatDekking met correcte referenties naar Grondslagen en Beton.
        /// Roep deze methode aan na het instellen van Materiaal en ProjectInfo.
        /// NIEUW: Synct ook PlaatDekking naar MainPart indien PlatePart.
        /// </summary>
        protected void InitializePlaatDekking()
        {
            if (ProjectInfo?.Grondslagen == null || Beton == null)
            {
                Console.WriteLine($"⚠️ [{GetType().Name}] Kan PlaatDekking niet initialiseren: ProjectInfo.Grondslagen of Beton is null");
                return;
            }

            // ✅ Initialiseer Boven als deze null is
            if (PlaatDekking.Boven == null)
            {
                PlaatDekking.Boven = new BetonDekkingContext(ProjectInfo.Grondslagen, Beton)
                {
                    IsKwaliteitsBeheersing = true,
                    IsPlaatGeometrie = true,
                    SelectedMilieuklassen = [MilieuklasseEnum.XC1],
                    DekkingToe = 20
                };
            }
            else
            {
                // Update alleen de referenties
                PlaatDekking.Boven.Grondslagen = ProjectInfo.Grondslagen;
                PlaatDekking.Boven.Beton = Beton;
                PlaatDekking.Boven.IsKwaliteitsBeheersing = true;  
                PlaatDekking.Boven.IsPlaatGeometrie = true;
            }

            // ✅ Initialiseer Onder als deze null is
            if (PlaatDekking.Onder == null)
            {
                PlaatDekking.Onder = new BetonDekkingContext(ProjectInfo.Grondslagen, Beton)
                {
                    IsKwaliteitsBeheersing = true,
                    IsPlaatGeometrie = true,
                    SelectedMilieuklassen = [MilieuklasseEnum.XC1],
                    DekkingToe = 20
                };
            }
            else
            {
                // Update alleen de referenties
                PlaatDekking.Onder.Grondslagen = ProjectInfo.Grondslagen;
                PlaatDekking.Onder.Beton = Beton;
                PlaatDekking.Onder.IsKwaliteitsBeheersing = true;
                PlaatDekking.Onder.IsPlaatGeometrie = true;
            }

            // ✅ Initialiseer en bereken de parent DekkingContext
            PlaatDekking.Init();
            PlaatDekking.BerekenEnValideer();

            // ✅ NIEUW: Sync PlaatDekking naar MainPart indien PlatePart
            if (MainPart is SlabPart plate)
            {
                plate.PlaatDekking = this.PlaatDekking; // Gedeelde referentie!
                Console.WriteLine($"✅ [{GetType().Name}] MainPart.PlaatDekking gesynchroniseerd");
            }
        }

        public override void RestoreReferencesAfterDeserialization(ProjectEntity project)
        {
            base.RestoreReferencesAfterDeserialization(project);

            // ✅ BETON-SPECIFIEK: Zorg dat Materiaal altijd een BetonContext is
            if (Materiaal != null && Materiaal is not BetonContext)
            {
                Console.WriteLine($"⚠️ [{GetType().Name}] Materiaal is geen BetonContext! Type: {Materiaal.GetType().Name}");
                // Zoek een passend beton materiaal
                var betonMateriaal = project.Materialen.Values
                    .OfType<BetonContext>()
                    .FirstOrDefault();

                if (betonMateriaal != null)
                {
                    Materiaal = betonMateriaal;
                    MateriaalId = betonMateriaal.Id;
                }
            }

            // ✅ INTELLIGENTE FALLBACK: Als materiaal niet gevonden op basis van ID,
            // zoek dan een bestaand materiaal met dezelfde eigenschappen, of het eerste beschikbare beton
            if (Materiaal == null && MateriaalId.HasValue)
            {
                Console.WriteLine($"⚠️ [{GetType().Name}] Materiaal met ID {MateriaalId} niet gevonden in project.");
                
                // Probeer het eerste beschikbare BetonContext te vinden (ongeacht kwaliteit)
                var eersteBeton = project.Materialen.Values
                    .OfType<BetonContext>()
                    .FirstOrDefault();
                
                if (eersteBeton != null)
                {
                    Console.WriteLine($"✅ Bestaand beton materiaal gevonden ({eersteBeton.Naam}). Gebruik deze.");
                    Materiaal = eersteBeton;
                    MateriaalId = eersteBeton.Id;
                }
                else
                {
                    Console.WriteLine("⚠️ Geen bestaand beton materiaal gevonden. Maak C20/25 aan (conservatief).");
                    var nieuwBeton = new BetonContext("C20/25"); // Conservatieve keuze
                    Materiaal = nieuwBeton;
                    MateriaalId = nieuwBeton.Id;
                    project.Materialen[nieuwBeton.Id] = nieuwBeton;
                }
            }
            
            // ✅ EXTRA FALLBACK: Als materiaal nog steeds null (zonder MateriaalId)
            if (Materiaal == null)
            {
                Console.WriteLine($"⚠️ [{GetType().Name}] Geen betonmateriaal gevonden. Maak C20/25 aan.");
                var nieuwBeton = new BetonContext("C20/25");
                Materiaal = nieuwBeton;
                MateriaalId = nieuwBeton.Id;
                project.Materialen[nieuwBeton.Id] = nieuwBeton;
            }

            // ✅ NIEUW: Herstel MainPart referenties
            if (MainPart != null)
            {
                MainPart.ParentAssemblage = this;
                MainPart.Material = this.Materiaal; // Shared reference!
                
                if (MainPart is SlabPart plate)
                {
                    plate.PlaatDekking = this.PlaatDekking; // Shared reference!
                    Console.WriteLine($"✅ [{GetType().Name}] MainPart referenties hersteld (PlatePart)");
                    
                    // ✅ FIX: Controleer of Boven en Onder BasisWapening dezelfde referentie delen (na deserialisatie)
                    // Dit kan gebeuren door ReferenceHandler.IgnoreCycles bij JSON serialisatie
                    if (plate.PlaatWapening?.Boven?.BasisWapening != null && 
                        plate.PlaatWapening?.Onder?.BasisWapening != null &&
                        ReferenceEquals(plate.PlaatWapening.Boven.BasisWapening, plate.PlaatWapening.Onder.BasisWapening))
                    {
                        Console.WriteLine($"⚠️ [{GetType().Name}] PROBLEEM: Boven.BasisWapening en Onder.BasisWapening zijn hetzelfde object! Maak kopie...");
                        // Maak een diepe kopie van Onder.BasisWapening
                        plate.PlaatWapening.Onder.BasisWapening = plate.PlaatWapening.Onder.BasisWapening.Clone();
                        Console.WriteLine($"✅ [{GetType().Name}] Onder.BasisWapening gekloond. Nu zijn het verschillende objecten.");
                    }

                    // ✅ FIX: Hetzelfde voor VerdeelWapening
                    if (plate.PlaatWapening?.Boven?.VerdeelWapening != null && 
                        plate.PlaatWapening?.Onder?.VerdeelWapening != null &&
                        ReferenceEquals(plate.PlaatWapening.Boven.VerdeelWapening, plate.PlaatWapening.Onder.VerdeelWapening))
                    {
                        Console.WriteLine($"⚠️ [{GetType().Name}] PROBLEEM: Boven.VerdeelWapening en Onder.VerdeelWapening zijn hetzelfde object! Maak kopie...");
                        plate.PlaatWapening.Onder.VerdeelWapening = plate.PlaatWapening.Onder.VerdeelWapening.Clone();
                        Console.WriteLine($"✅ [{GetType().Name}] Onder.VerdeelWapening gekloond.");
                    }
                }
            }

            // ✅ NIEUW: Herstel SubParts referenties
            foreach (var subPart in SubParts)
            {
                subPart.ParentAssemblage = this;
                subPart.Material = this.Materiaal; // Shared reference!
            }
        }
    }
}
