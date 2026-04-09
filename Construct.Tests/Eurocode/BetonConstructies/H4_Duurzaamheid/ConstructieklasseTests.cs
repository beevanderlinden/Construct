using Eurocode.BetonConstructies;
using Eurocode.Grondslagen;
using FluentAssertions;
using Xunit;

namespace Construct.Tests.Eurocode.BetonConstructies.H4_Duurzaamheid;

/// <summary>
/// Unit tests voor <see cref="Constructieklasse"/> — bepaling constructieklasse.
/// Ref: NEN-EN 1992-1-1, §4.4.1.2 (5).
/// </summary>
public class ConstructieklasseTests
{
    [Fact]
    public void DefaultKlasse_IsS4()
    {
        var ctx = new BetonDekkingContext();

        ctx.Constructieklasse.Klasse.Should().Be(4);
    }

    [Fact]
    public void LangereOntwerpLevensduur_VerhoogtKlasse()
    {
        var grondslagen = new GrondslagenContext { OntwerpLevensduur = OntwerpLevensduurEnum.Honderd };
        var ctx = new BetonDekkingContext(grondslagen, new BetonContext());

        // Basisklasse S4 + CorrectieLevensduur +2 = S6
        ctx.Constructieklasse.Klasse.Should().Be(6);
    }

    [Fact]
    public void PlaatGeometrie_VerlaagdKlasse()
    {
        var ctx = new BetonDekkingContext();
        ctx.IsPlaatGeometrie = true;

        // Basisklasse S4 + CorrectiePlaatGeometrie -1 = S3
        ctx.Constructieklasse.Klasse.Should().Be(3);
    }

    [Fact]
    public void KwaliteitsBeheersing_VerlaagdKlasse()
    {
        var ctx = new BetonDekkingContext();
        ctx.IsKwaliteitsBeheersing = true;

        // Basisklasse S4 + CorrectieKwaliteitsBeheersing -1 = S3
        ctx.Constructieklasse.Klasse.Should().Be(3);
    }
}
