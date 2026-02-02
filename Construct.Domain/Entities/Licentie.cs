namespace Construct.Domain.Entities
{
    public class Licentie
    {
        public Guid Id { get; set; }
        public Guid BedrijfId { get; set; }
        public Bedrijf? Bedrijf { get; set; }

        public int MaxGebruikers { get; set; }
        public DateTime GeldigTot { get; set; }
    }

}
