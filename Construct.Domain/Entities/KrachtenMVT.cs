//using Kaskon.Toolbox.PrefabModels;
namespace Construct.Domain.Entities
{
    public class KrachtenMVT
    {
        public KrachtenMVT()
        {

        }

        public KrachtenMVT(double m = 0, double v = 0, double t = 0)
        {
            M = m;
            V = v;
            T = t;
        }

        public double M { get; set; }
        public double V { get; set; }
        public double T { get; set; }
    }



}
