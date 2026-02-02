using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Construct.Application.Interfaces.Beam
{
    public interface ILoad
    {
        double MomentAt(double x);
        double TotalForce { get; }
        double ForceArmFromStart { get; }
    }
}
