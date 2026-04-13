
namespace Eurocode.BetonConstructies;

/// <summary>
/// Context voor beugel(en) in een betonnen doorsnede
/// </summary>
public class BeugelWapeningContext
{
    /// <summary>
    /// Aantal sneden (1 of 2)
    /// </summary>
    public int AantalSneden { get; set; } = 2;
    
    /// <summary>
    /// Diameter van de beugel in mm
    /// </summary>
    public double Diameter { get; set; } = 8;
    
    /// <summary>
    /// Dekking links in mm
    /// </summary>
    public double DekkingLinks { get; set; } = 30;
    
    /// <summary>
    /// Dekking rechts in mm
    /// </summary>
    public double DekkingRechts { get; set; } = 30;
    
    /// <summary>
    /// Dekking boven in mm
    /// </summary>
    public double DekkingBoven { get; set; } = 30;
    
    /// <summary>
    /// Dekking onder in mm
    /// </summary>
    public double DekkingOnder { get; set; } = 30;
    
    /// <summary>
    /// Buigstraal in mm (standaard 2.5 × diameter voor hart)
    /// </summary>
    public double Buigstraal { get; set; } = 20; // 2.5 × 8mm
    
    /// <summary>
    /// Hoek van de haak (90 of 135 graden)
    /// </summary>
    public int HaakHoek { get; set; } = 135;
    
    /// <summary>
    /// Rechte lengte na de buiging in mm
    /// </summary>
    public double HaakLengte { get; set; } = 40;
    
    /// <summary>
    /// Draairichting van de beugel (true = linksom, false = rechtsom)
    /// </summary>
    public bool Linksom { get; set; } = true;
    
    /// <summary>
    /// Geeft string representatie van de beugel (bijv. "r8-150")
    /// </summary>
    public override string ToString()
    {
        string sneden = AantalSneden == 2 ? "2-snedig" : "1-snedig";
        return $"r{Diameter} ({sneden})";
    }
}
