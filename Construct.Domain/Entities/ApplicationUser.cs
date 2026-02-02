using Microsoft.AspNetCore.Identity;

namespace Construct.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public Guid BedrijfId { get; set; }
        public Bedrijf? Bedrijf { get; set; }

        public string? Voornaam { get; set; }
        public string? Achternaam { get; set; }

        public bool IsAdmin { get; set; }
    }

}
