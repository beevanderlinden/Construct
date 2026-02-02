using CommonLibrary;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Construct.WebUI.Server.Helpers
{
    public static class EurocodeColorHelper
    {
        /// <summary>
        /// Helper voor de kleur, gebaseerd op een eurocode toetsing
        /// </summary>
        /// <param name="contexten">De betreffende eurocode-onderdeel </param>
        /// <returns>De Accent-kleur indien geen fouten, De Warning-kleur indien waarschuwingen aanwezig.</returns>
        public static Color GetTabColor(params BaseEurocodeContext[] contexten)
        {
            return contexten.Any(c => c.Meldingen.Any(m => m.Type == MeldingType.Waarschuwing))
                 ? Color.Warning
                 : Color.Accent;
        }

        public static Color GetColor(bool akkoord)
        {
            return akkoord ? Color.Accent : Color.Warning;
        }

    }
}
