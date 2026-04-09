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

        private string? _plaatsnaam;
        //[Required(ErrorMessage = "{0} is verplicht")]
        [Display(Name = "Plaatsnaam")]
        [MinLength(2, ErrorMessage = "{0} minimaal {1} leestekens")]
        [MaxLength(30, ErrorMessage = "{0} maximaal {1} leestekens")]
        public string? Plaatsnaam 
        { 
            get => _plaatsnaam;
            set => _plaatsnaam = string.IsNullOrWhiteSpace(value) ? null : value; 
        }



        /// <summary>
        /// Eurocode 0 wordt toegepast op het hele project
        /// </summary>
        public GrondslagenContext Grondslagen { get; set; } = new()
        {
            OntwerpLevensduur = OntwerpLevensduurEnum.Vijftig,
            Gevolgklasse = GevolgklasseEnum.CC2
        };

        /// <summary>
        /// Minimale brandwerendheid in minuten voor alle vloeren in het project (REI).
        /// Waarde 0 = geen eis.
        /// </summary>
        public int MinimaleREI { get; set; } = 0;




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
