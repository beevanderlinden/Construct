namespace Construct.Domain.Entities
{
    public class Bedrijf
    {
        public Guid Id { get; set; }
        public string Naam { get; set; } = string.Empty;

        public ICollection<ApplicationUser> Gebruikers { get; set; } = [];
        public Licentie? Licentie { get; set; }
    }

}
