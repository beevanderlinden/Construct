namespace Construct.Domain.Entities
{
    /// <summary>
    /// Bepaalt hoe het programma omgaat met wapeningsinvoer bij herberekening.
    /// </summary>
    public enum WapeningAfhandelingEnum
    {
        /// <summary>
        /// Geen automatische aanpassing. Programma controleert de invoer maar past de wapening nooit aan.
        /// Als de invoer leeg is, wordt de wapening automatisch bepaald.
        /// </summary>
        Gebruiker = 0,

        /// <summary>
        /// (Default) Geen optimalisatie. Programma controleert de ingevoerde wapening
        /// en verhoogt deze alleen als de ingevoerde wapening onvoldoende is.
        /// Als de invoer leeg is, wordt de wapening automatisch bepaald.
        /// </summary>
        AlleenVerhogen = 1,

        /// <summary>
        /// Volledige optimalisatie. Programma berekent altijd de economisch optimale wapening,
        /// ook als de huidige wapening zwaarder is dan nodig (wapening kan ook verlagen).
        /// Als de invoer leeg is, wordt de wapening automatisch bepaald.
        /// </summary>
        Optimaliseer = 2,
    }
}
