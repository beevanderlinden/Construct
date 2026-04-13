using CommonLibrary.Models;

namespace Construct.Domain.Entities
{
    /// <summary>
    /// Mijn model voor documenten.
    /// 
    /// </summary>
    public class DocumentEntity
    {


        public string Title { get; set; } = "My Document";
        public string Subtitle { get; set; } = "My Subtitle";

        public string ProjectNumber { get; set; } = "0000";


        public string CompanyName { get; set; } = "Construct BV";
        public string DocumentNumber { get; set; } = "ber-01";



        public string Onderdeel { get; set; } = "TRAPPEN_EN_BORDESSEN";


        public string Author { get; set; } = "W.A. van Buuren";
        public string CheckedBy { get; set; } = "M. Zorigueta";


        // 📄 Documentversies
        public List<ExportFactory.MigraDocContentModels.Revision> Revisions { get; set; } = [
            new(){Date = DateTime.Now.AddDays(0), Name = "1", Description = "1e uitgave"},
            //new(){Date = DateTime.Now.AddDays(0), Name = "2", Description = "gewijzigd"},
            ];


        // 📄 Tekstblok met projectinformatie
        public List<LabelWithStringValue> ProjectLabels { get; set; } = [
            new LabelWithStringValue("projectnummer", "0000"),
            new LabelWithStringValue("projectnaam", "My Project"),
            new LabelWithStringValue("plaats", "My City"),
            ];

        // 📄 Tekstblok met documentinformatie
        public List<LabelWithStringValue> DocumentLabels { get; set; } = [
            new LabelWithStringValue("documentnummer", "ber-01"),
            new LabelWithStringValue("onderdeel", "Trappen"),
            new LabelWithStringValue("opgesteld door", "W.A. van Buuren"),
            new LabelWithStringValue("gecontroleerd door", "M. Zorrigueta"),
            //new LabelWithStringValue("versie", "1"),
            ];





    }







}
