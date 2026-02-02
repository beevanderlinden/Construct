using Eurocode.BetonConstructies;
using Eurocode.Grondslagen;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components.Extensions;

namespace Construct.WebUI.Server.Shared.Data
{
    public class EurocodeData
    {
        public List<string> Milieuklassen { get; } = [
            "X0",
            "XC1", "XC2", "XC3", "XC4",
            "XD1", "XD2", "XD3",
            "XS1", "XS2", "XS3",
            "XA1","XA2", "XA3",
            "XF1", "XF2", "XF3", "XF4"
        ];

        public List<MilieuklasseEnum> MilieuklasseEnums { get; } = [
            MilieuklasseEnum.XC1, MilieuklasseEnum.XC2, MilieuklasseEnum.XC3, MilieuklasseEnum.XC4,
            MilieuklasseEnum.XD1, MilieuklasseEnum.XD2, MilieuklasseEnum.XD3,
            MilieuklasseEnum.XS1, MilieuklasseEnum.XS2, MilieuklasseEnum.XS3,
            MilieuklasseEnum.XF1, MilieuklasseEnum.XF2, MilieuklasseEnum.XF3,   MilieuklasseEnum.XF4,
            MilieuklasseEnum.XA1, MilieuklasseEnum.XA2, MilieuklasseEnum.XA3,
        ];


        public List<Option<GevolgklasseEnum>> GetGevolgklasseOpties
        {
            get
            {
                var opties = Enum.GetValues(typeof(GevolgklasseEnum))
                    .Cast<GevolgklasseEnum>()
                    .Select(x => new Option<GevolgklasseEnum> { Value = x, Text = x.GetDescription() })
                    .ToList();
                return opties;
            }
        }




        /// <summary>
        /// Gevolgklasse CC 
        /// CC staat voor Consequence Class
        /// </summary>
        public List<string> Gevolgklassen { get; } = ["CC1", "CC2", "CC3"];

        /// <summary>
        /// Betrouwbaarheidsklass RC
        /// RC staat voor Reliability Class
        /// </summary>
        public List<string> Betrouwbaarheidsklassen { get; } = ["RC1", "RC2", "RC3"];



    }
}
