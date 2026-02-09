using Eurocode.HoutConstructies;
using System.Text.Json.Serialization;

namespace Construct.Domain.Entities
{
    /// <summary>
    /// Hout-specifieke assemblage zonder betondekking.
    /// Gebruikt voor LiggerEntity (hout), KolomEntity (hout), etc.
    /// </summary>
    public abstract class HoutAssemblageEntity : AssemblageEntity
    {
        /// <summary>
        /// Het houtmateriaal van dit assemblage.
        /// Cast van base.Materiaal naar HoutContext voor type-safety.
        /// </summary>
        [JsonIgnore]
        public HoutContext? Hout
        {
            get => Materiaal as HoutContext;
            set => Materiaal = value;
        }

        public override void RestoreReferencesAfterDeserialization(ProjectEntity project)
        {
            base.RestoreReferencesAfterDeserialization(project);

            // ? HOUT-SPECIFIEK: Zorg dat Materiaal altijd een HoutContext is
            if (Materiaal != null && Materiaal is not HoutContext)
            {
                Console.WriteLine($"?? [{GetType().Name}] Materiaal is geen HoutContext! Type: {Materiaal.GetType().Name}");
                // Zoek een passend hout materiaal
                var houtMateriaal = project.Materialen.Values
                    .OfType<HoutContext>()
                    .FirstOrDefault();

                if (houtMateriaal != null)
                {
                    Materiaal = houtMateriaal;
                    MateriaalId = houtMateriaal.Id;
                }
            }

            // ? Fallback: Als nog steeds geen materiaal, maak C24
            if (Materiaal == null)
            {
                Console.WriteLine($"?? [{GetType().Name}] Geen houtmateriaal gevonden. Maak C24 aan.");
                var nieuwHout = new HoutContext { Kwaliteit = Houtkwaliteit.C24 };
                Materiaal = nieuwHout;
                MateriaalId = nieuwHout.Id;
                project.Materialen[nieuwHout.Id] = nieuwHout;
            }
        }
    }
}
