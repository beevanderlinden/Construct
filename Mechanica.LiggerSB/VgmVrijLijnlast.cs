using Microsoft.AspNetCore.Localization;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mechanica.LiggerSB
{
    public class VmnResult
    {
        public VmnResult(double x, VergeetMijNietje vmn)
        {
            X = x;
            VMN = vmn;
            Calculate();
        }

        double X { get; set; }
        VergeetMijNietje VMN { get; set; }

        public double V { get; set; }
        public double M { get; set; } 
        public double Theta { get; set; }
        public double W { get; set; }

        public void Calculate()
        {
            V = VMN.GetV(X);
            M = VMN.GetM(X);
            Theta = VMN.GetTheta(X);
            W = VMN.GetW(X);
        }

    }

    public abstract class VergeetMijNietje : IVergeetMijNietje
    {
        public double L { get; set; }
        public double E { get; set; }
        public double I { get; set; }
        public double RA { get; set; }
        public double RB { get; set; }

        // vaste punten
        public VmnResult PuntA => new (0, this);
        public VmnResult PuntB => new (L, this);
        public VmnResult PuntC => new (L / 2, this);

        // berekeningen (abstract, per vergeet-mij-nietje verschillend)
        public abstract double GetM(double x);
        public abstract double GetTheta(double x);
        public abstract double GetV(double x);
        public abstract double GetW(double x);

        // controle voor x (voor alle vergeet-mij-nietjes gelijk)
        public void CheckPosX(double x)
        {
            if (x < 0 || x > L)
                throw new ArgumentOutOfRangeException(nameof(x), "x moet tussen 0 en L liggen.");
        }

        public abstract double GetRA();
        public abstract double GetRB();
        public abstract void Init();
    }

    public interface IVergeetMijNietje
    {
        double GetRA();
        double GetRB();
        double GetV(double x);
        double GetM(double x);
        double GetTheta(double x);  
        double GetW(double x);
        void CheckPosX(double x);
        public void Init();
    }


    public class VgmInklemmingLijnlast : VergeetMijNietje
    {
        public VgmInklemmingLijnlast(double q, double l, double e, double i)
        {
            Q = q;
            L = l;
            E = e;
            I = i;
            Init();
        }
        // inputs (specifiek voor dit vergeet-mij-nietje)
        public double Q { get; set; }
        // methoden voor berekeningen (specifiek voor dit vergeet-mij-nietje)
        public override double GetV(double x)
        {
            CheckPosX(x);
            return Q * (L - x);
        }
        public override double GetM(double x)
        {
            CheckPosX(x);
            return -Q * Math.Pow(L - x, 2) / 2.0;
        }
        public override double GetTheta(double x)
        {
            CheckPosX(x);
            return Q / (6.0 * E * I) * (Math.Pow(L - x, 3) - Math.Pow(L, 3));
        }
        public override double GetW(double x)
        {
            CheckPosX(x);
            return Q / (24.0 * E * I) * (4 * L * Math.Pow(x, 3) - 6 * Math.Pow(L, 2) * Math.Pow(x, 2) - Math.Pow(x, 4));
        }
        public override double GetRA()
        {
            return Q * L;
        }
        public override double GetRB()
        {
            return 0;
        }
        public override void Init()
        {
            RA = GetRA();
            RB = GetRB();
        }
    }



    public class VgmVrijPuntlast : VergeetMijNietje
    {
        public VgmVrijPuntlast(double p, double a, double l, double e, double i)
        {
            P = p;
            A = a;
            L = l;
            E = e;
            I = i;
            Init();
        }
        // inputs (specifiek voor dit vergeet-mij-nietje)
        public double P { get; set; }
        public double A { get; set; }

        // integratieconstante: afgeleid uit randvoorwaarde w(L) = 0
        private double C1 => -RA * Math.Pow(L, 2) / 6.0 + P * Math.Pow(L - A, 3) / (6.0 * L);

        // methoden voor berekeningen (specifiek voor dit vergeet-mij-nietje)
        public override double GetV(double x)
        {
            CheckPosX(x);
            if (x < A)
                return RA;
            else
                return RA - P;
        }
        public override double GetM(double x)
        {
            CheckPosX(x);
            if (x < A)
                return RA * x;
            else
                return RA * x - P * (x - A);
        }
        public override double GetTheta(double x)
        {
            CheckPosX(x);
            if (x < A)
                return (RA * Math.Pow(x, 2) / 2.0 + C1) / (E * I);
            else
                return (RA * Math.Pow(x, 2) / 2.0 - P * Math.Pow(x - A, 2) / 2.0 + C1) / (E * I);
        }
        public override double GetW(double x)
        {
            CheckPosX(x);
            if (x < A)
                return (RA * Math.Pow(x, 3) / 6.0 + C1 * x) / (E * I);
            else
                return (RA * Math.Pow(x, 3) / 6.0 - P * Math.Pow(x - A, 3) / 6.0 + C1 * x) / (E * I);
        }
        public override double GetRA()
        {
            return P * (L - A) / L;
        }
        public override double GetRB()
        {
            return P * A / L;
        }
        public override void Init()
        {
            RA = GetRA();
            RB = GetRB();
        }

    }

    /// <summary>
    /// Voor standaard controleberekeningen aan de hand van de berekende doorsnede-eigenschappen en krachten.
    /// </summary>
    public class VgmVrijLijnlast : VergeetMijNietje
    {
        public VgmVrijLijnlast(double q, double l, double e, double i)
        {
            Q = q;
            L = l;
            E = e;
            I = i;

            Init();
        }


        // inputs (specifiek voor dit vergeet-mij-nietje)
        public double Q { get; set; }


        // methoden voor berekeningen (specifiek voor dit vergeet-mij-nietje)
        public override double GetV(double x)
        {
            CheckPosX(x);
            return RA - Q * x;
        }
                    

        public override double GetM(double x)
        {
            CheckPosX(x);
            return RA * x - Q * Math.Pow(x, 2) / 2.0;
        }

        public override double GetTheta(double x)
        {
            CheckPosX(x);
            return Q / (24 * E * I) * (6 * L * Math.Pow(x, 2) - 4 * Math.Pow(x, 3) - Math.Pow(L, 3));
        }

        public override double GetW(double x)
        {
            CheckPosX(x);
            return Q / (24 * E * I) * (2 * L * Math.Pow(x, 3) - Math.Pow(x, 4) - Math.Pow(L, 3) * x);
        }

        public override double GetRA()
        {
            return Q * L / 2.0;
        }

        public override double GetRB()
        {
            return Q * L / 2.0;
        }

        public override void Init()
        {
            RA = GetRA();
            RB = GetRB();
        }
    }

    /// <summary>
    /// Vrij opgelegde ligger met een combinatie van lijnlasten en/of puntlasten.
    /// Gebruikt het superpositieprincipe: V, M, θ en w zijn de som van de
    /// afzonderlijke vergeet-mij-nietjes. L, E en I zijn gedeeld.
    /// </summary>
    /// <example>
    /// var vmn = new VgmVrij(L, E, I)
    ///               .VoegLijnlastToe(10)
    ///               .VoegPuntlastToe(20, 4.0);
    /// </example>
    public class VgmVrij : VergeetMijNietje
    {
        private readonly List<VergeetMijNietje> _lasten = [];

        public VgmVrij(double l, double e, double i)
        {
            L = l;
            E = e;
            I = i;
            Init();
        }

        /// <summary>Voegt een verdeelde belasting q (kN/m) toe. Retourneert this voor fluent chaining.</summary>
        public VgmVrij VoegLijnlastToe(double q)
        {
            _lasten.Add(new VgmVrijLijnlast(q, L, E, I));
            Refresh();
            return this;
        }

        /// <summary>Voegt een puntlast p (kN) op positie a (m) toe. Retourneert this voor fluent chaining.</summary>
        public VgmVrij VoegPuntlastToe(double p, double a)
        {
            _lasten.Add(new VgmVrijPuntlast(p, a, L, E, I));
            Refresh();
            return this;
        }

        private void Refresh()
        {
            RA = GetRA();
            RB = GetRB();
        }

        public override double GetV(double x)
        {
            CheckPosX(x);
            return _lasten.Sum(vmn => vmn.GetV(x));
        }

        public override double GetM(double x)
        {
            CheckPosX(x);
            return _lasten.Sum(vmn => vmn.GetM(x));
        }

        public override double GetTheta(double x)
        {
            CheckPosX(x);
            return _lasten.Sum(vmn => vmn.GetTheta(x));
        }

        public override double GetW(double x)
        {
            CheckPosX(x);
            return _lasten.Sum(vmn => vmn.GetW(x));
        }

        public override double GetRA() => _lasten.Sum(vmn => vmn.GetRA());
        public override double GetRB() => _lasten.Sum(vmn => vmn.GetRB());

        public override void Init()
        {
            RA = GetRA();
            RB = GetRB();
        }
    }
}
