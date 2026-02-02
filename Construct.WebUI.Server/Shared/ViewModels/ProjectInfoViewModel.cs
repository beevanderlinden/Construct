using Eurocode.Grondslagen;

using System.ComponentModel.DataAnnotations;

namespace Construct.WebUI.Server.Shared.ViewModels
{
    public class ProjectInfoViewModel
    {
        [Required(ErrorMessage = "{0} is verplicht")]
        [Display(Name = "Projectnaam")]
        [MinLength(1, ErrorMessage = "{0} minimaal {1} leestekens")]
        [MaxLength(40, ErrorMessage = "{0} maximaal {1} leestekens")]
        public string? Naam { get; set; }


        [Required(ErrorMessage = "{0} is verplicht")]
        [Display(Name = "Projectnummer")]
        [MinLength(4, ErrorMessage = "{0} minimaal {1} leestekens")]
        [MaxLength(20, ErrorMessage = "{0} maximaal {1} leestekens")]
        public string? Nummer { get; set; }


        [Required(ErrorMessage = "{0} is verplicht")]
        [Display(Name = "Plaatsnaam")]
        [MinLength(2, ErrorMessage = "{0} minimaal {1} leestekens")]
        [MaxLength(30, ErrorMessage = "{0} maximaal {1} leestekens")]
        public string? Plaatsnaam { get; set; }



        /// <summary>
        /// Eurocode 0 wordt toegepast op het hele project
        /// </summary>
        public GrondslagenContext Grondslagen { get; set; } = new()
        {
            OntwerpLevensduur = OntwerpLevensduurEnum.Vijftig,
            Gevolgklasse = GevolgklasseEnum.CC2
        };




        /// <summary>
        /// <see langword="override"/> functie
        /// </summary>
        /// <returns>Projectinfo als string</returns>
        public override string ToString()
        {
            return $"{Nummer} {Naam} te {Plaatsnaam?.ToUpper()}";
        }






    }


}
