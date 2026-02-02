using Construct.Domain.Entities;

namespace Construct.WebUI.Server.Shared.ViewModels
{
    public class AssemblagesViewModel
    {
        public AssemblagesViewModel() { }

        public List<AssemblageEntity> Assemblages { get; set; } = [];


    }

    public class AssemblageViewModel
    {
        public AssemblageViewModel() { }





        public AssemblageViewModel(Guid guid, string? merk, string? naam)
        {
            Guid = guid;
            Merk = merk;
            Naam = naam;
        }

        public AssemblageEntity? AssemblageEntity { get; set; }

        public string? MainPart { get; set; }

        public Guid Guid { get; set; } = Guid.NewGuid();

        public string? Merk { get; set; }
        public string? Naam { get; set; }

        public string? Onderdeel { get; set; }

        public enum AssemblageTypeEnum
        {
            prefab_beton_assemblage,
            staal_assemblage,
            hout_assemblage,

        }

    }
}
