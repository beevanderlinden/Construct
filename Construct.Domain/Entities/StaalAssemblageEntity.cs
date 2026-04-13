using Eurocode.StaalConstructies;
using System.Text.Json.Serialization;

namespace Construct.Domain.Entities
{
    /// <summary>
    /// Staal-specifieke assemblage zonder betondekking.
    /// Gebruikt voor LiggerEntity (staal), KolomEntity (staal), etc.
    /// </summary>
    public abstract class StaalAssemblageEntity : AssemblageEntity
    {
        /// <summary>
        /// Het staalmateriaal van dit assemblage.
        /// Cast van base.Materiaal naar StaalContext voor type-safety.
        /// </summary>
        [JsonIgnore]
        public StaalContext? Staal
        {
            get => Materiaal as StaalContext;
            set => Materiaal = value;
        }

        public override void RestoreReferencesAfterDeserialization(ProjectEntity project)
        {
            base.RestoreReferencesAfterDeserialization(project);

            // ? STAAL-SPECIFIEK: Zorg dat Materiaal altijd een StaalContext is
            if (Materiaal != null && Materiaal is not StaalContext)
            {
                Console.WriteLine($"?? [{GetType().Name}] Materiaal is geen StaalContext! Type: {Materiaal.GetType().Name}");
                // Zoek een passend staal materiaal
                var staalMateriaal = project.Materialen.Values
                    .OfType<StaalContext>()
                    .FirstOrDefault();

                if (staalMateriaal != null)
                {
                    Materiaal = staalMateriaal;
                    MateriaalId = staalMateriaal.Id;
                }
            }

            // ? Fallback: Als nog steeds geen materiaal, maak S235
            if (Materiaal == null)
            {
                Console.WriteLine($"?? [{GetType().Name}] Geen staalmateriaal gevonden. Maak S235 aan.");
                var nieuwStaal = new StaalContext { StaalKwaliteit = StaalKwaliteitEnum.S235 };
                Materiaal = nieuwStaal;
                MateriaalId = nieuwStaal.Id;
                project.Materialen[nieuwStaal.Id] = nieuwStaal;
            }
        }
    }
}
