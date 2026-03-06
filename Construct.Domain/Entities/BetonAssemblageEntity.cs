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

            // ✅ Fallback: Als nog steeds geen materiaal, maak C45/55
            if (Materiaal == null)
            {
                Console.WriteLine($"⚠️ [{GetType().Name}] Geen betonmateriaal gevonden. Maak C45/55 aan.");
                var nieuwBeton = new BetonContext("C45/55");
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
