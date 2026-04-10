namespace Mechanica.LiggerSB
{
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
}
