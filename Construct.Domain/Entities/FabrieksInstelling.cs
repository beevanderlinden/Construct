using CommonLibrary.Models;
using Eurocode.BetonConstructies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Construct.Domain.Entities
{
    public class FabrieksInstelling
    {
        private string _fabrieksNaam = "fabriek X";
        public string FabrieksNaam { get { return _fabrieksNaam; } set { _fabrieksNaam = value; } }

        private double _diamBasis = 8;
        /// <summary>
        /// Begin met zoeken naar wapening (basis) vanaf deze diameter.
        /// </summary>
        public double DiamBasis { get { return _diamBasis; } set { _diamBasis = value; } }

        private double _diamDetailWapening = 6;

        /// <summary>
        /// Begin met zoeken naar wapening (details) vanaf deze diameter. 
        /// </summary>
        public double DiamDetailWapening { get { return _diamDetailWapening; } set { _diamDetailWapening = value; } }

        private BaseMateriaal _materiaal = new BetonContext() { Betonsterkteklasse = BetonsterkteklasseEnum.C45_55 };
        public BaseMateriaal Materiaal { get { return _materiaal; } set { _materiaal = value; } }


        /// <summary>
        /// Bovengrens van de hart-op-hart afstand. 
        /// </summary>
        private int _hohBovengrens = 150;
        public int HohBovengrens { get { return _hohBovengrens; } set { _hohBovengrens = value; } }

        private int _hohOndergrensBasis = 100;
        /// <summary>
        /// Ondergrens van de hart-op-hart afstand voor de basiswapening.
        /// Als er een kleinere hoh afstand noodzakelijk is, dan wordt de diameter verhoogd. 
        /// </summary>
        public int HohOndergrensBasis { get { return _hohOndergrensBasis; } set { _hohOndergrensBasis = value; } }

        private int _hohZoekstap = 10;
        /// <summary>
        /// Zoekstap voor hoh-afstanden. 
        /// Bijvoorbeeld: 10 betekent dat er gezocht wordt naar 150, 140, 130, etc. totdat er een geschikte wapening gevonden is.
        /// </summary>
        public int HohZoekstap { get { return _hohZoekstap; } set { _hohZoekstap = value; } }



    }
}
