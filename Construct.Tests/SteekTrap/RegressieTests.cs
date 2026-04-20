using Construct.Tests.Factories;
using FluentAssertions;
using Xunit;

namespace Construct.Tests.SteekTrap;

public class RegressieTests
{
    [Fact]
    public void Trap1_HalfVerdieping_RegressieWaarden()
    {
        var (trap, _) = SteekTrapFactory.Create(8, 100, 187.5, 220, false);
        trap.Bijwerken();

        trap.LtProjZ.Should().BeApproximately(1794.0, 50);
        trap.HoogteTotaal.Should().BeApproximately(1500.0, 1);
        trap.Snedekrachten.My.Should().BeApproximately(-4.76, 0.5);
        trap.Snedekrachten.Vz.Should().BeApproximately(11.1, 0.5);
        trap.MomentSchil!.AsRequired.Should().BeApproximately(155, 5);
    }

    [Fact]
    public void Trap2_VolVerdieping_RegressieWaarden()
    {
        var (trap, _) = SteekTrapFactory.Create(16, 120, 185, 220, true);
        trap.Bijwerken();



        trap.LtProjZ.Should().BeApproximately(3503.0, 40);
        trap.Snedekrachten.My.Should().BeApproximately(-19.2, 1.0);
        trap.MomentSchil!.AsRequired.Should().BeApproximately(518, 5);
    }

    [Fact]
    public void Trap3_SteileSmalleTrap_RegressieWaarden()
    {
        var (trap, _) = SteekTrapFactory.Create(14, 130, 200, 210, true);
        trap.Bijwerken();

        trap.LtProjZ.Should().BeApproximately(2927.0, 50);
        trap.Snedekrachten.My.Should().BeApproximately(-14.5, 0.5);
    }

    [Fact]
    public void Trap4_LangeFlauweTrap_RegressieWaarden()
    {
        var (trap, _) = SteekTrapFactory.Create(18, 150, 170, 240, false);
        trap.Bijwerken();

        trap.LtProjZ.Should().BeApproximately(4349.0, 50);
        trap.Snedekrachten.My.Should().BeApproximately(-30.7, 0.5);
    }
}
