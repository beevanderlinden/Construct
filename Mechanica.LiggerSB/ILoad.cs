namespace Mechanica.LiggerSB
{
    // basis ILoad, uitgebreid met partial integralen
    public interface ILoad
    {
        double MomentAt(double x); // (optioneel, kan 0)
        double TotalForce { get; } // signed: neerwaarts NEGATIEF
        double ForceArmFromStart { get; } // zwaartepunt van de load (global x)
        double PartialForceUpTo(double x);    // resultante van deze load over [startOfLoad, x) (signed)
        double PartialMomentUpTo(double x);   // integral ∫ q(s)*(x - s) ds over contribution to moment at x from this load (signed)
    }


}

