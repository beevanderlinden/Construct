using System;
using System.Text.Json.Serialization;

namespace Construct.Domain.Common
{
    /// <summary>
    /// Generieke helper voor het beheren van entity-referenties met ID-opslag en object-herstel.
    /// Serialiseert ALLEEN de Guid (EntityId), niet het object zelf (Entity).
    /// 
    /// Voorbeeld:
    /// <code>
    /// // Creatie
    /// var matRef = new MateriaalReference();
    /// matRef.Attach(betonContext);  // Zet Entity en EntityId
    /// 
    /// // Na JSON-deserialisatie
    /// matRef.Restore(guid => project.Materialen.TryGetValue(guid, out var mat) ? mat : null);
    /// var materiaal = matRef.Entity;  // ? Nu beschikbaar!
    /// </code>
    /// </summary>
    /// <typeparam name="T">Het entity-type waarvan deze reference een verwijzing beheert</typeparam>
    public abstract class EntityReference<T> where T : class
    {
        /// <summary>
        /// De Guid-referentie naar de entity. Dit wordt geserialiseerd.
        /// </summary>
        public Guid? EntityId { get; set; }

        /// <summary>
        /// Het werkelijke object. Dit wordt NIET geserialiseerd.
        /// Wordt ingesteld via Attach() of Restore().
        /// </summary>
        [JsonIgnore]
        public T? Entity { get; private set; }

        /// <summary>
        /// Koppelt een entity-object aan deze referentie.
        /// Gebruikt bij creatie/updates - werkt in beide richtingen.
        /// </summary>
        /// <param name="entity">De entity om aan te koppelen, of null om te verwijderen</param>
        public void Attach(T? entity)
        {
            Entity = entity;
            EntityId = entity != null ? GetEntityId(entity) : null;
        }

        /// <summary>
        /// Herstelt de entity-referentie via een resolver-functie.
        /// Gebruikt na JSON-deserialisatie om de object-graph samen te stellen.
        /// </summary>
        /// <param name="resolver">
        /// Functie die een Guid opzoekt en de bijbehorende entity retourneert.
        /// Voorbeeld: guid => project.Materialen.TryGetValue(guid, out var mat) ? mat : null
        /// </param>
        public void Restore(Func<Guid, T?> resolver)
        {
            if (EntityId.HasValue)
            {
                Entity = resolver(EntityId.Value);
            }
            else
            {
                Entity = null;
            }
        }

        /// <summary>
        /// Haalt de Guid van een entity op.
        /// Implementeer dit in afgeleide klassen voor het specifieke entity-type.
        /// </summary>
        /// <param name="entity">De entity waarvan de ID opgehaald moet worden</param>
        /// <returns>De Guid identifier van de entity</returns>
        protected abstract Guid GetEntityId(T entity);
    }
}
