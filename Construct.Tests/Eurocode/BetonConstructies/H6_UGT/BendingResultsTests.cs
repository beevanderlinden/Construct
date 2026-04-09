using Eurocode.BetonConstructies;
using Eurocode.Belastingen;
using FluentAssertions;
using Profielen.Beton;
using Xunit;

namespace Construct.Tests.Eurocode.BetonConstructies.H6_UGT;

/// <summary>
/// Unit tests voor <see cref="BendingResults"/> — momentwapening.
/// Ref: NEN-EN 1992-1-1, §6.1.
/// </summary>
public class BendingResultsTests
{
    private static BendingResults CreateBr(string wap, double dekking = 25, double my = 20, double h = 150)
    {
        var beton = new BetonContext(BetonsterkteklasseEnum.C30_37);
        return new BendingResults(
            beton,
            new BetonProfiel(1000, h),
            new WapeningContext(wap, dekking),
            new SectionForces { My = my });
    }

    // ── Nuttige hoogte ────────────────────────────────────────────────────────

    [Fact]
    public void NuttigeHoogte_D_BerekendCorrect()
    {
        // d = h - c - ø/2 = 150 - 25 - 4 = 121 mm
        var br = CreateBr("8-150");
        br.D.Should().BeApproximately(121, 0.5);
    }

    // ── Benodigde wapening ────────────────────────────────────────────────────

    [Fact]
    public void AsRequired_PositiefBijMoment()
    {
        var br = CreateBr("8-150", my: 20);
        br.AsRequired.Should().BeGreaterThan(0);
    }

    [Fact]
    public void AsRequired_NeeemtToeMetGroterMoment()
    {
        var br20 = CreateBr("10-100", my: 20);
        var br40 = CreateBr("10-100", my: 40);
        br40.AsRequired.Should().BeGreaterThan(br20.AsRequired);
    }

    [Fact]
    public void AsRequired_NulBijGeenMoment()
    {
        // MEd = 0 → AsBerekend = 0, AsMin2 = 1.25 × 0 = 0 → AsRequired = 0
        var br = CreateBr("8-150", my: 0);
        br.AsRequired.Should().Be(0);
    }

    [Fact]
    public void Minimumwapening_WordtToegepast_BijLichtBelastePlaat()
    {
        // Licht belast: AsMin1 (uit scheurmoment) is maatgevend boven AsBerekend
        var br = CreateBr("8-150", my: 2);
        br.MinimaleWapeningToegepast.Should().BeTrue();
    }

    // ── Validatie ─────────────────────────────────────────────────────────────

    [Fact]
    public void IsValidated_True_BijVoldoendeWapening()
    {
        // 10-100 geeft As ≈ 785 mm² > AsRequired ≈ 496 mm²
        var br = CreateBr("10-100", my: 20);
        br.IsValidated.Should().BeTrue();
    }

    [Fact]
    public void IsValidated_False_BijOnvoldoendeWapening()
    {
        // 6-200 geeft As ≈ 141 mm² < AsRequired ≈ 496 mm²
        var br = CreateBr("6-200", my: 20);
        br.IsValidated.Should().BeFalse();
    }
}
