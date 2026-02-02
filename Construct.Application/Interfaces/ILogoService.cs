namespace Construct.Application.Interfaces
{
    public interface ILogoService
    {
        /// <summary>
        /// Vind een logo op basis van een e-mailadres.
        /// </summary>
        string FindLogoByEmail(string email);

        /// <summary>
        /// Vind een logo op basis van een domein (bijv. ah.nl).
        /// </summary>
        string FindLogoByDomain(string domain);

        /// <summary>
        /// Leeg de interne cache (bijvoorbeeld na upload van nieuwe logo's)
        /// </summary>
        void ClearCache();
    }
}
