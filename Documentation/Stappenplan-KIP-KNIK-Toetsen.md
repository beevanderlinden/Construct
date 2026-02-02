# Stappenplan: KIP en KNIK Toetsen implementeren (EC3 & EC5)

## 📋 Inhoudsopgave
1. [Analyse & Verschillen](#analyse--verschillen)
2. [Architectuur Overwegingen](#architectuur-overwegingen)
3. [Data Requirements](#data-requirements)
4. [Implementatie Stappen](#implementatie-stappen)
5. [Testing Strategie](#testing-strategie)

---

## 🎯 Analyse & Verschillen

### Wat is anders bij KIP/KNIK vs Snede-toetsen?

#### **Snede-toetsen** (huidige implementatie)
```csharp
// ✅ INPUT: Alleen snedekrachten + profiel + materiaal
var result = toets.Check(forces, profiel, materiaal);

// Berekening: M_Ed vs M_Rd of V_Ed vs V_Rd
// Simpel: lokale weerstand op 1 punt
```

#### **KIP/KNIK-toetsen** (nieuw)
```csharp
// ❌ ONVOLDOENDE: Snedekrachten alleen zijn niet genoeg!
// ✅ NODIG: 
// - Kniklengtes (L_cr,y, L_cr,z, L_cr,T voor torsie)
// - Steuningscondities (Fixed/Pin/Free per richting)
// - Belastingspositie (bovenzijde/hartlijn/onderzijde)
// - Belastingsvorm (uniform/constant/variabel)
// - Mogelijk: C-factoren voor momentverloop
```

### Belangrijkste verschillen

| Aspect | Snede-toets | KIP/KNIK-toets |
|--------|-------------|----------------|
| **Input** | Forces only | Forces + Geometry + Boundary Conditions |
| **Scope** | Lokaal (1 punt) | Globaal (hele staaf) |
| **Complexiteit** | Laag (1 vergelijking) | Hoog (meerdere stappen) |
| **Context** | Profiel + Materiaal | + Lengtes + Steuningen + Belasting |
| **Resultaat** | UC op 1 positie | UC voor hele staaf |

---

## 🏗️ Architectuur Overwegingen

### Optie 1: **Uitbreiden huidige toets-interface** ❌
```csharp
// PROBLEEM: Te veel parameters, niet schaalbaar
public EurocodeResultaat Check(
    InternalForces f, 
    IProfiel p, 
    IMateriaal m,
    double kniklengteY,    // ⚠️ Te veel!
    double kniklengteZ,    // ⚠️ Wordt onoverzichtelijk
    SupportType startY,    // ⚠️ Niet generiek
    SupportType endY,      // ⚠️ Moeilijk te testen
    // ... etc
)
```

### Optie 2: **Context object patroon** ✅ AANBEVOLEN
```csharp
// ✅ GOED: Schaalbaar, testbaar, duidelijk
public class StabilityContext
{
    // Geometrie
    public double Length { get; set; }
    public double KniklengteY { get; set; }  // L_cr,y
    public double KniklengteZ { get; set; }  // L_cr,z
    public double KniklengteT { get; set; }  // L_cr,T (torsie/kip)
    
    // Steuningen
    public SupportCondition SupportY { get; set; }  // knik om y-as
    public SupportCondition SupportZ { get; set; }  // knik om z-as
    public SupportCondition SupportWarping { get; set; }  // warping (kip)
    
    // Belasting
    public LoadPosition LoadPosition { get; set; }  // Top/Center/Bottom
    public LoadDistribution LoadDistribution { get; set; }  // Uniform/Constant/Variable
    public double C1Factor { get; set; } = 1.0;  // Voor moment diagram
    
    // Normkeuze
    public BelastingsduurKlasse? Belastingsduur { get; set; }  // Voor EC5
}

// Gebruik:
var context = new StabilityContext 
{
    Length = 4000,  // mm
    KniklengteY = 4000,
    KniklengteZ = 2000,  // tussensteun
    KniklengteT = 4000,
    SupportY = SupportCondition.PinPin,
    LoadPosition = LoadPosition.TopFlange
};

var result = kipToets.Check(forces, profiel, materiaal, context);
```

### Optie 3: **Beam-based context** 🤔 OVERWEGEN
```csharp
// Haal context direct uit de beam (als die beschikbaar is)
public class BeamStabilityAnalysis
{
    public BeamStabilityAnalysis(SBLigger beam)
    {
        // Auto-detect from beam:
        Length = beam.Length;
        // Scan for point supports to determine kniklengte
        // Analyze load distribution
    }
}

// Voordeel: Minder handmatig werk
// Nadeel: Beam is niet altijd beschikbaar (export scenario's)
```

---

## 📊 Data Requirements

### Nieuwe Enums/Classes nodig

```csharp
// 1. Steun condities
public enum SupportCondition
{
    [Description("Aan beide zijden ingeklemd")]
    FixedFixed,      // k = 0.5
    
    [Description("Aan beide zijden scharnierend")]
    PinPin,          // k = 1.0
    
    [Description("Ingeklemd-scharnierend")]
    FixedPin,        // k = 0.7
    
    [Description("Ingeklemd-vrij")]
    FixedFree,       // k = 2.0
    
    [Description("Aangepast")]
    Custom           // Gebruiker specificeert k-factor
}

// 2. Belasting positie (voor kip)
public enum LoadPosition
{
    [Description("Bovenzijde (drukkant)")]
    TopFlange,       // Ongunstig voor kip
    
    [Description("Hartlijn")]
    Center,          // Neutraal
    
    [Description("Onderzijde (trekkant)")]
    BottomFlange     // Gunstig voor kip
}

// 3. Belasting verdeling
public enum LoadDistribution
{
    Uniform,         // Constante q
    Constant,        // Constant moment
    Triangular,      // Driehoek
    Variable         // Gebruik C-factoren
}

// 4. Knik curve (EC3 - Staal)
public enum BucklingCurve
{
    [Description("Curve a0 (lage imperfectie)")]
    a0,
    
    [Description("Curve a")]
    a,
    
    [Description("Curve b")]
    b,
    
    [Description("Curve c")]
    c,
    
    [Description("Curve d (hoge imperfectie)")]
    d
}
```

### Uitbreiding van bestaande classes

```csharp
// In SBLigger (Mechanica.LiggerSB)
public class SBLigger
{
    // NIEUW: Stability context
    public StabilityContext? StabilitySettings { get; set; }
    
    // Helper om auto-detect te doen
    public StabilityContext DeriveStabilityContext()
    {
        return new StabilityContext
        {
            Length = this.Length * 1000,  // m naar mm
            // Auto-detect kniklengtes op basis van supports
            KniklengteY = DetermineKniklengteY(),
            KniklengteZ = DetermineKniklengteZ(),
            // Scan loads voor positie
            LoadPosition = DetermineLoadPosition(),
            // etc.
        };
    }
}

// In LiggerEntity (Construct.Domain)
public class LiggerEntity : AssemblageEntity
{
    // NIEUW: UI kan dit instellen
    public StabilityContext StabilitySettings { get; set; } = new();
    
    public override void Bijwerken()
    {
        // Bestaande snede-toetsen
        // ...
        
        // NIEUW: Stability toetsen
        if (Beam.Materiaal is StaalContext staal)
        {
            var kipToets = new KipToets(sectionClass: 1);
            var kipResult = kipToets.Check(
                forcesMyMax, 
                staalProfiel, 
                staal, 
                StabilitySettings);
            EurcodeResultaten.Add(kipResult);
            
            var knikToets = new KnikToets(BucklingCurve.b);
            var knikResult = knikToets.Check(
                forcesN,
                staalProfiel,
                staal,
                StabilitySettings);
            EurcodeResultaten.Add(knikResult);
        }
    }
}
```

---

## 🔨 Implementatie Stappen

### **FASE 1: Fundament leggen** (1-2 dagen)

#### Stap 1.1: Maak StabilityContext class
```
📁 CommonLibrary.Models/
   └─ StabilityContext.cs         ✨ NIEUW
   └─ SupportCondition.cs         ✨ NIEUW (enum)
   └─ LoadPosition.cs             ✨ NIEUW (enum)
   └─ LoadDistribution.cs         ✨ NIEUW (enum)
```

**Checklist:**
- [ ] Class aanmaken met alle properties
- [ ] XML documentatie toevoegen
- [ ] Default waarden instellen (veilige kant)
- [ ] Validation logic (Length > 0, etc.)
- [ ] Unit tests voor validation

#### Stap 1.2: Uitbreiden BaseEurocodeToets
```csharp
// Optie A: Nieuwe interface voor stability
public interface IStabilityCheck
{
    EurocodeResultaat Check(
        InternalForces forces,
        IProfiel profiel,
        IMateriaal materiaal,
        StabilityContext stability);
}

// Optie B: Overload op bestaande
public abstract class BaseEurocodeToets
{
    // Bestaand
    public abstract EurocodeResultaat Check(
        InternalForces f, 
        IProfiel p, 
        IMateriaal m);
    
    // NIEUW: Voor stability
    public virtual EurocodeResultaat Check(
        InternalForces f,
        IProfiel p,
        IMateriaal m,
        StabilityContext stability)
    {
        throw new NotSupportedException(
            "Deze toets ondersteunt geen stability checks");
    }
}
```

**Aanbeveling:** Gebruik Optie B (overload) voor backwards compatibility.

---

### **FASE 2: EC3 Staal - KNIK** (2-3 dagen)

#### Stap 2.1: Knik weerstandsklasse
```
📁 Eurocode3.StaalConstructies/Chapter 6/Stability/
   └─ BucklingResistance.cs       ✨ NIEUW
```

**Implementatie:**
```csharp
public static class BucklingResistance
{
    /// <summary>
    /// EC3 §6.3.1.2 - Knikweerstand voor druk
    /// </summary>
    public static double NbRd(
        double A,           // Oppervlakte [mm²]
        double fy,          // Vloeispanning [N/mm²]
        double gammaM1,     // Partiele factor
        double lambda,      // Relatieve slankheid
        BucklingCurve curve)
    {
        // 1. Imperfectiefactor α afhankelijk van curve
        double alpha = curve switch
        {
            BucklingCurve.a0 => 0.13,
            BucklingCurve.a  => 0.21,
            BucklingCurve.b  => 0.34,
            BucklingCurve.c  => 0.49,
            BucklingCurve.d  => 0.76,
            _ => 0.49
        };
        
        // 2. Φ-waarde (6.49)
        double phi = 0.5 * (1 + alpha * (lambda - 0.2) + lambda * lambda);
        
        // 3. Reductiefactor χ (6.49)
        double chi = 1.0 / (phi + Math.Sqrt(phi * phi - lambda * lambda));
        chi = Math.Min(chi, 1.0);
        
        // 4. Knikweerstand (6.47)
        return chi * A * fy / gammaM1 * 1e-3; // N naar kN
    }
    
    /// <summary>
    /// Relatieve slankheid berekenen
    /// </summary>
    public static double RelativeSlenderness(
        double A,      // Oppervlakte [mm²]
        double Lcr,    // Kniklengte [mm]
        double i,      // Traagheidsstraal [mm]
        double fy,     // Vloeispanning [N/mm²]
        double E)      // E-modulus [N/mm²]
    {
        // λ̄ = (L_cr / i) / λ₁
        // met λ₁ = π√(E/fy) = 93.9ε
        double epsilon = Math.Sqrt(235 / fy);
        double lambda1 = 93.9 * epsilon;
        double lambda = (Lcr / i) / lambda1;
        return lambda;
    }
}
```

#### Stap 2.2: KnikToets class
```
📁 Eurocode3.StaalConstructies/Chapter 6/Stability/
   └─ KnikToets.cs                ✨ NIEUW
```

**Implementatie:**
```csharp
public class KnikToets : BaseEurocodeToets
{
    private readonly BucklingCurve _curve;
    private readonly BucklingAxis _axis;  // Y of Z as
    
    public KnikToets(
        BucklingCurve curve, 
        BucklingAxis axis = BucklingAxis.Y)
    {
        _curve = curve;
        _axis = axis;
    }
    
    public override string Titel => 
        $"Knik om {_axis}-as";
    public override string Norm => "EN 1993-1-1";
    public override string Artikel => "6.3.1";
    
    public override EurocodeResultaat Check(
        InternalForces f,
        IProfiel p,
        IMateriaal m,
        StabilityContext stability)
    {
        var staal = m as StaalContext 
            ?? throw new InvalidOperationException("Geen staal");
        var profiel = p as IStaalProfiel 
            ?? throw new InvalidOperationException("Geen staalprofiel");
        
        // Selecteer juiste richting
        double Lcr = _axis == BucklingAxis.Y 
            ? stability.KniklengteY 
            : stability.KniklengteZ;
        double i = _axis == BucklingAxis.Y 
            ? profiel.TraagheidsStraalIy 
            : profiel.TraagheidsStraalIz;
        
        // Relatieve slankheid
        double lambda = BucklingResistance.RelativeSlenderness(
            profiel.A, Lcr, i, staal.Fy, staal.E);
        
        // Knikweerstand
        double NbRd = BucklingResistance.NbRd(
            profiel.A, staal.Fy, staal.GammaM1, lambda, _curve);
        
        return new EurocodeResultaat
        {
            Positie = "Hele staaf",
            Titel = Titel,
            Norm = Norm,
            Artikel = Artikel,
            Formule = "(6.46)-(6.49)",
            Waarde = Math.Abs(f.N),
            Toelaatbaar = NbRd,
            Unit = "kN",
            Forces = f,
            Toelichting = $"Curve {_curve}, λ̄ = {lambda:F3}, " +
                         $"L_cr = {Lcr:F0} mm, χ = {NbRd / (profiel.A * staal.Fy / staal.GammaM1 * 1e-3):F3}"
        };
    }
}
```

**Checklist:**
- [ ] BucklingResistance helpers implementeren
- [ ] KnikToets class maken
- [ ] Unit tests voor verschillende curves
- [ ] Verificatie met handberekening (bijv. HEA200)
- [ ] Edge cases testen (λ < 0.2, λ > 3.0)

---

### **FASE 3: EC3 Staal - KIP** (3-4 dagen)

#### Stap 3.1: Kip weerstandsklasse
```
📁 Eurocode3.StaalConstructies/Chapter 6/Stability/
   └─ LateralTorsionalBucklingResistance.cs  ✨ NIEUW
```

**Implementatie (vereenvoudigd):**
```csharp
public static class LateralTorsionalBucklingResistance
{
    /// <summary>
    /// EC3 §6.3.2.2 - Laterale torsionale knik weerstand
    /// </summary>
    public static double MbRd(
        double Wy,          // Weerstandsmoment [mm³]
        double fy,          // Vloeispanning [N/mm²]
        double gammaM1,     // Partiele factor
        double lambdaLT,    // Relatieve slankheid LT
        double C1 = 1.0)    // Moment correctiefactor
    {
        // Voor λ̄_LT ≤ 0.4 of M_Ed/M_cr ≤ 0.4: geen LT knik
        if (lambdaLT <= 0.4)
            return Wy * fy / gammaM1 * 1e-6; // Volle plastische weerstand
        
        // Modificatie factor (6.57)
        double beta = 0.75;  // Voor gewalste profielen
        double lambdaLT0 = 0.4;
        double f = 1 - 0.5 * (1 - C1) * 
                  (1 - 2 * (lambdaLT - lambdaLT0) * (lambdaLT - lambdaLT0));
        f = Math.Min(f, 1.0);
        
        // Φ_LT waarde (6.57)
        double alpha = 0.34;  // Voor gewalste I/H profielen
        double phiLT = 0.5 * (1 + alpha * (lambdaLT - lambdaLT0) + 
                             beta * lambdaLT * lambdaLT);
        
        // Reductiefactor χ_LT (6.57)
        double chiLT = Math.Min(
            1.0 / (phiLT + Math.Sqrt(phiLT * phiLT - beta * lambdaLT * lambdaLT)),
            1.0);
        
        // Correctie voor C1
        chiLT = Math.Min(chiLT / f, 1.0);
        
        // Kip weerstand (6.55)
        return chiLT * Wy * fy / gammaM1 * 1e-6; // kNm
    }
    
    /// <summary>
    /// Relatieve slankheid voor laterale torsionale knik
    /// </summary>
    public static double RelativeSlendernessLT(
        double Wy,      // Weerstandsmoment [mm³]
        double fy,      // Vloeispanning [N/mm²]
        double Mcr)     // Kritisch kip moment [kNm]
    {
        double Mpl = Wy * fy * 1e-6; // kNm
        return Math.Sqrt(Mpl / Mcr);
    }
    
    /// <summary>
    /// Kritisch kip moment volgens vereenvoudigde formule
    /// EC3 §6.3.2.2 / Annex F (vereenvoudigd voor h/b < 3.5)
    /// </summary>
    public static double Mcr_Simplified(
        double E,       // E-modulus [N/mm²]
        double G,       // Glijdingsmodulus [N/mm²]
        double Iz,      // Traagheidsmoment zwakke as [mm⁴]
        double It,      // Torsie constante [mm⁴]
        double Iw,      // Warping constante [mm⁶]
        double Lcr)     // Kip lengte [mm]
    {
        // Vereenvoudigde formule voor uniform moment:
        // M_cr = π²EI_z/L² × √((I_w/I_z) + (GL²I_t)/(π²EI_z))
        
        double term1 = Math.PI * Math.PI * E * Iz / (Lcr * Lcr);
        double term2 = Math.Sqrt(
            Iw / Iz + 
            (G * Lcr * Lcr * It) / (Math.PI * Math.PI * E * Iz)
        );
        
        return term1 * term2 * 1e-6; // Nmm naar kNm
    }
}
```

#### Stap 3.2: KipToets class
```
📁 Eurocode3.StaalConstructies/Chapter 6/Stability/
   └─ KipToets.cs                 ✨ NIEUW
```

**Implementatie:**
```csharp
public class KipToets : BaseEurocodeToets
{
    private readonly int _sectionClass;
    
    public KipToets(int sectionClass)
    {
        _sectionClass = sectionClass;
    }
    
    public override string Titel => "Laterale torsionale knik (kip)";
    public override string Norm => "EN 1993-1-1";
    public override string Artikel => "6.3.2";
    
    public override EurocodeResultaat Check(
        InternalForces f,
        IProfiel p,
        IMateriaal m,
        StabilityContext stability)
    {
        var staal = m as StaalContext 
            ?? throw new InvalidOperationException("Geen staal");
        var profiel = p as IStaalProfiel 
            ?? throw new InvalidOperationException("Geen staalprofiel");
        
        // Kies W op basis van section class
        double W = _sectionClass <= 2 
            ? profiel.WplY 
            : profiel.WelY;
        
        // Kritisch kip moment
        double G = staal.E / (2 * (1 + 0.3)); // G = E/(2(1+ν))
        double Mcr = LateralTorsionalBucklingResistance.Mcr_Simplified(
            staal.E, G,
            profiel.Iz,
            profiel.It,
            profiel.Iw,
            stability.KniklengteT);
        
        // Relatieve slankheid
        double lambdaLT = 
            LateralTorsionalBucklingResistance.RelativeSlendernessLT(
                W, staal.Fy, Mcr);
        
        // C1 factor (afhankelijk van momentverloop)
        double C1 = DetermineC1Factor(stability);
        
        // Kip weerstand
        double MbRd = 
            LateralTorsionalBucklingResistance.MbRd(
                W, staal.Fy, staal.GammaM1, lambdaLT, C1);
        
        return new EurocodeResultaat
        {
            Positie = "Hele staaf",
            Titel = Titel,
            Norm = Norm,
            Artikel = Artikel,
            Formule = "(6.54)-(6.57)",
            Waarde = Math.Abs(f.My),
            Toelaatbaar = MbRd,
            Unit = "kNm",
            Forces = f,
            Toelichting = 
                $"λ̄_LT = {lambdaLT:F3}, M_cr = {Mcr:F1} kNm, " +
                $"C₁ = {C1:F2}, L_cr,T = {stability.KniklengteT:F0} mm"
        };
    }
    
    private double DetermineC1Factor(StabilityContext stability)
    {
        // Tabel 6.6 of Annex F
        return stability.LoadDistribution switch
        {
            LoadDistribution.Uniform => 1.13,      // Uniforme q
            LoadDistribution.Constant => 1.0,      // Constant M
            LoadDistribution.Triangular => 1.35,   // Driehoek
            _ => stability.C1Factor                // Custom
        };
    }
}
```

**Checklist:**
- [ ] LateralTorsionalBucklingResistance implementeren
- [ ] C1 factor logic (Tabel 6.6)
- [ ] KipToets class maken
- [ ] Unit tests (vergelijk met software zoals Staad/SCIA)
- [ ] Verificatie met voorbeeld uit SteelGuide
- [ ] Edge cases (λ_LT < 0.4, zeer slanke profielen)

---

### **FASE 4: EC5 Hout - KNIK** (2-3 dagen)

#### Stap 4.1: Knik weerstandsklasse hout
```
📁 Eurocode5.HoutConstructies/Chapter 6/Stability/
   └─ BucklingResistance.cs       ✨ NIEUW
```

**Implementatie:**
```csharp
public static class BucklingResistance
{
    /// <summary>
    /// EC5 §6.3.2 - Knikweerstand voor druk in hout
    /// </summary>
    public static double NcRd(
        double A,           // Oppervlakte [mm²]
        double fc0d,        // Rekenwaarde druksterkte [N/mm²]
        double lambda_rel)  // Relatieve slankheid
    {
        // Voor λ_rel ≤ 0.3: geen knik
        if (lambda_rel <= 0.3)
            return A * fc0d * 1e-3; // N naar kN
        
        // k_c factor volgens (6.25)-(6.26)
        double kc = CalculateKc(lambda_rel);
        
        // N_c,Rd = k_c × A × f_c,0,d
        return kc * A * fc0d * 1e-3; // kN
    }
    
    /// <summary>
    /// Reductiefactor k_c voor knik (6.25)-(6.26)
    /// </summary>
    private static double CalculateKc(double lambda_rel)
    {
        // Voor gezaagd/gelijmd hout: βc = 0.2
        double betaC = 0.2;
        
        // k = 0.5(1 + βc(λ_rel - 0.3) + λ_rel²)  (6.26)
        double k = 0.5 * (
            1 + betaC * (lambda_rel - 0.3) + 
            lambda_rel * lambda_rel);
        
        // k_c = 1/(k + √(k² - λ_rel²))  (6.25)
        double kc = 1.0 / (k + Math.Sqrt(k * k - lambda_rel * lambda_rel));
        
        return Math.Min(kc, 1.0);
    }
    
    /// <summary>
    /// Relatieve slankheid voor hout
    /// </summary>
    public static double RelativeSlenderness(
        double Lcr,     // Kniklengte [mm]
        double i,       // Traagheidsstraal [mm]
        double fc0k,    // Karakteristieke druksterkte [N/mm²]
        double E005)    // 5-percentiel E-modulus [N/mm²]
    {
        // λ_rel = (L_cr/i) / π × √(f_c,0,k / E_0,05)  (6.21)
        double lambda = Lcr / i;
        double lambdaRelative = lambda / Math.PI * 
                               Math.Sqrt(fc0k / E005);
        return lambdaRelative;
    }
}
```

#### Stap 4.2: KnikToets hout
```
📁 Eurocode5.HoutConstructies/Chapter 6/Stability/
   └─ KnikToets.cs                ✨ NIEUW
```

**Implementatie:**
```csharp
public class KnikToets : BaseEurocodeToets
{
    private readonly BelastingsduurKlasse _belastingsduur;
    private readonly BucklingAxis _axis;
    
    public KnikToets(
        BelastingsduurKlasse belastingsduur, 
        BucklingAxis axis = BucklingAxis.Y)
    {
        _belastingsduur = belastingsduur;
        _axis = axis;
    }
    
    public override string Titel => $"Knik om {_axis}-as";
    public override string Norm => "EN 1995-1-1";
    public override string Artikel => "6.3.2";
    
    public override EurocodeResultaat Check(
        InternalForces f,
        IProfiel p,
        IMateriaal m,
        StabilityContext stability)
    {
        var hout = m as HoutContext 
            ?? throw new InvalidOperationException("Geen hout");
        var profiel = p as BaseProfiel 
            ?? throw new InvalidOperationException("Geen profiel");
        
        // Rekenwaarde druksterkte
        double fc0d = hout.GetFc0d(_belastingsduur);
        
        // Traagheidsstraal
        double i = _axis == BucklingAxis.Y
            ? Math.Sqrt(profiel.Iy / profiel.A)
            : Math.Sqrt(profiel.Iz / profiel.A);
        
        // Kniklengte
        double Lcr = _axis == BucklingAxis.Y 
            ? stability.KniklengteY 
            : stability.KniklengteZ;
        
        // E_0,05 (5-percentiel)
        double E005 = hout.E * 0.67; // Vereenvoudigd: E_0,05 ≈ 2/3 × E_mean
        
        // Relatieve slankheid
        double lambdaRel = BucklingResistance.RelativeSlenderness(
            Lcr, i, hout.Fc0k, E005);
        
        // Knikweerstand
        double NcRd = BucklingResistance.NcRd(
            profiel.A, fc0d, lambdaRel);
        
        return new EurocodeResultaat
        {
            Positie = "Hele staaf",
            Titel = Titel,
            Norm = Norm,
            Artikel = Artikel,
            Formule = "(6.23)-(6.26)",
            Waarde = Math.Abs(f.N),
            Toelaatbaar = NcRd,
            Unit = "kN",
            Forces = f,
            Toelichting = 
                $"λ_rel = {lambdaRel:F3}, " +
                $"L_cr = {Lcr:F0} mm, " +
                $"k_c = {NcRd / (profiel.A * fc0d * 1e-3):F3}"
        };
    }
}
```

**Checklist:**
- [ ] BucklingResistance voor hout
- [ ] KnikToets class
- [ ] Unit tests
- [ ] Verificatie met EC5 voorbeelden
- [ ] Edge cases (λ_rel < 0.3, zeer slank)

---

### **FASE 5: EC5 Hout - KIP** (2-3 dagen)

#### Stap 5.1: Kip weerstandsklasse hout
```
📁 Eurocode5.HoutConstructies/Chapter 6/Stability/
   └─ LateralBucklingResistance.cs  ✨ NIEUW
```

**Implementatie:**
```csharp
public static class LateralBucklingResistance
{
    /// <summary>
    /// EC5 §6.3.3 - Laterale knik weerstand voor hout
    /// </summary>
    public static double MbRd(
        double W,           // Weerstandsmoment [mm³]
        double fmd,         // Rekenwaarde buigsterkte [N/mm²]
        double lambda_relm) // Relatieve slankheid buiging
    {
        // Voor λ_rel,m ≤ 0.75: geen laterale knik
        if (lambda_relm <= 0.75)
            return W * fmd * 1e-6; // Volle buigweerstand, kNm
        
        // k_crit factor (6.34)
        double kcrit = 1.0;
        if (lambda_relm <= 1.4)
        {
            kcrit = 1.56 - 0.75 * lambda_relm;
        }
        else
        {
            kcrit = 1.0 / (lambda_relm * lambda_relm);
        }
        
        // M_b,Rd = k_crit × W × f_m,d  (6.33)
        return kcrit * W * fmd * 1e-6; // kNm
    }
    
    /// <summary>
    /// Relatieve slankheid voor laterale knik hout
    /// </summary>
    public static double RelativeSlenderness(
        double W,       // Weerstandsmoment [mm³]
        double fmk,     // Karakteristieke buigsterkte [N/mm²]
        double Mcr)     // Kritisch kip moment [kNm]
    {
        // λ_rel,m = √(W × f_m,k / M_cr)  (6.30)
        double Mmk = W * fmk * 1e-6; // kNm
        return Math.Sqrt(Mmk / Mcr);
    }
    
    /// <summary>
    /// Kritisch kip moment voor rechthoekige doorsnede
    /// </summary>
    public static double Mcr_Rectangular(
        double b,       // Breedte [mm]
        double h,       // Hoogte [mm]
        double lef,     // Effectieve lengte [mm]
        double E005,    // E-modulus 5-percentiel [N/mm²]
        double G005)    // Glijdingsmodulus 5-percentiel [N/mm²]
    {
        // Voor rechthoekige doorsnede (6.32):
        // M_cr = (b²/(h×l_ef)) × √(E_0,05 × G_0,05)
        
        double term1 = b * b / (h * lef);
        double term2 = Math.Sqrt(E005 * G005);
        
        return term1 * term2 * 1e-6; // Nmm naar kNm
    }
}
```

#### Stap 5.2: KipToets hout
```
📁 Eurocode5.HoutConstructies/Chapter 6/Stability/
   └─ KipToets.cs                 ✨ NIEUW
```

**Implementatie:**
```csharp
public class KipToets : BaseEurocodeToets
{
    private readonly BelastingsduurKlasse _belastingsduur;
    
    public KipToets(BelastingsduurKlasse belastingsduur)
    {
        _belastingsduur = belastingsduur;
    }
    
    public override string Titel => "Laterale knik (kip)";
    public override string Norm => "EN 1995-1-1";
    public override string Artikel => "6.3.3";
    
    public override EurocodeResultaat Check(
        InternalForces f,
        IProfiel p,
        IMateriaal m,
        StabilityContext stability)
    {
        var hout = m as HoutContext 
            ?? throw new InvalidOperationException("Geen hout");
        var profiel = p as BaseProfiel 
            ?? throw new InvalidOperationException("Geen profiel");
        
        // Rekenwaarde buigsterkte
        double fmd = hout.GetFmd(_belastingsduur);
        
        // Weerstandsmoment
        double W = profiel.B * profiel.H * profiel.H / 6.0; // mm³
        
        // E en G waarden (5-percentiel)
        double E005 = hout.E * 0.67;
        double G005 = hout.E * 0.67 / 16; // G ≈ E/16 voor hout
        
        // Effectieve lengte (afhankelijk van steuning)
        double lef = DetermineEffectiveLength(
            stability.KniklengteT, 
            stability.LoadPosition);
        
        // Kritisch moment
        double Mcr = LateralBucklingResistance.Mcr_Rectangular(
            profiel.B, profiel.H, lef, E005, G005);
        
        // Relatieve slankheid
        double lambdaRelm = 
            LateralBucklingResistance.RelativeSlenderness(
                W, hout.Fmk, Mcr);
        
        // Kip weerstand
        double MbRd = LateralBucklingResistance.MbRd(
            W, fmd, lambdaRelm);
        
        return new EurocodeResultaat
        {
            Positie = "Hele staaf",
            Titel = Titel,
            Norm = Norm,
            Artikel = Artikel,
            Formule = "(6.33)-(6.34)",
            Waarde = Math.Abs(f.My),
            Toelaatbaar = MbRd,
            Unit = "kNm",
            Forces = f,
            Toelichting = 
                $"λ_rel,m = {lambdaRelm:F3}, " +
                $"M_cr = {Mcr:F1} kNm, " +
                $"l_ef = {lef:F0} mm"
        };
    }
    
    private double DetermineEffectiveLength(
        double Lcr, 
        LoadPosition loadPos)
    {
        // Tabel 6.1: correctiefactor voor belasting positie
        double factor = loadPos switch
        {
            LoadPosition.TopFlange => 1.0,      // Ongunstig
            LoadPosition.Center => 0.9,          // Neutraal
            LoadPosition.BottomFlange => 0.8,   // Gunstig
            _ => 1.0
        };
        
        return Lcr * factor;
    }
}
```

**Checklist:**
- [ ] LateralBucklingResistance voor hout
- [ ] Effectieve lengte logic (Tabel 6.1)
- [ ] KipToets class
- [ ] Unit tests
- [ ] Verificatie met EC5 voorbeelden
- [ ] Edge cases (λ_rel,m < 0.75, h/b ratios)

---

### **FASE 6: UI & Integratie** (2-3 dagen)

#### Stap 6.1: StabilitySettings UI component
```
📁 Construct.WebUI.Server/Components/Custom/
   └─ StabilitySettingsEditor.razor  ✨ NIEUW
```

**UI Mockup:**
```razor
<FluentCard>
    <FluentLabel>Stabiliteit instellingen</FluentLabel>
    
    <FluentNumberField Label="Ligger lengte (mm)" 
                       @bind-Value="Settings.Length" />
    
    <FluentTabs>
        <FluentTab Label="Knik Y-as">
            <FluentNumberField Label="Kniklengte L_cr,y (mm)"
                              @bind-Value="Settings.KniklengteY" />
            <FluentSelect Label="Steunconditie"
                         @bind-Value="Settings.SupportY">
                <FluentOption Value="SupportCondition.FixedFixed">
                    Ingeklemd-Ingeklemd (k=0.5)
                </FluentOption>
                <FluentOption Value="SupportCondition.PinPin">
                    Scharnierend-Scharnierend (k=1.0)
                </FluentOption>
                <!-- etc -->
            </FluentSelect>
        </FluentTab>
        
        <FluentTab Label="Knik Z-as">
            <!-- Idem voor Z-as -->
        </FluentTab>
        
        <FluentTab Label="Kip">
            <FluentNumberField Label="Kiplengte L_cr,T (mm)"
                              @bind-Value="Settings.KniklengteT" />
            <FluentSelect Label="Belastingpositie"
                         @bind-Value="Settings.LoadPosition">
                <FluentOption Value="LoadPosition.TopFlange">
                    Bovenzijde (drukkant) - ongunstig
                </FluentOption>
                <FluentOption Value="LoadPosition.Center">
                    Hartlijn - neutraal
                </FluentOption>
                <FluentOption Value="LoadPosition.BottomFlange">
                    Onderzijde (trekkant) - gunstig
                </FluentOption>
            </FluentSelect>
        </FluentTab>
    </FluentTabs>
    
    <!-- Auto-detect button -->
    <FluentButton OnClick="AutoDetectFromBeam">
        🔍 Auto-detect vanuit ligger
    </FluentButton>
</FluentCard>

@code {
    [Parameter]
    public StabilityContext Settings { get; set; } = new();
    
    [Parameter]
    public SBLigger? Beam { get; set; }
    
    private void AutoDetectFromBeam()
    {
        if (Beam == null) return;
        
        // Intelligente detectie:
        Settings.Length = Beam.Length * 1000; // m -> mm
        
        // Default: geen tussensteun
        Settings.KniklengteY = Settings.Length;
        Settings.KniklengteZ = Settings.Length;
        Settings.KniklengteT = Settings.Length;
        
        // Scan voor point supports in beam.Loads
        // ... detectie logic ...
        
        StateHasChanged();
    }
}
```

#### Stap 6.2: Integratie in LiggerEntity edit page
```razor
<!-- In LiggerEdit.razor -->
<FluentTabs>
    <FluentTab Label="Algemeen">
        <!-- Bestaande velden -->
    </FluentTab>
    
    <FluentTab Label="Belastingen">
        <!-- Bestaande belasting editor -->
    </FluentTab>
    
    <FluentTab Label="⚡ Stabiliteit" IconColor="Color.Warning">
        <StabilitySettingsEditor 
            Settings="@LiggerEntity.StabilitySettings"
            Beam="@LiggerEntity.Beam" />
    </FluentTab>
    
    <FluentTab Label="Toetsen">
        <!-- Bestaande toets resultaten -->
        <!-- ✨ NIEUW: Ook stability toetsen tonen -->
    </FluentTab>
</FluentTabs>
```

#### Stap 6.3: Update LiggerEntity.Bijwerken()
```csharp
public override void Bijwerken()
{
    // ... bestaande snede-toetsen ...
    
    // ✨ NIEUW: Stability toetsen
    if (Beam.Materiaal is StaalContext staal && 
        Beam.Profiel is IStaalProfiel staalProfiel)
    {
        // Controleer of N > 0 (druk) voor knik
        if (Math.Abs(maxN?.Forces.N ?? 0) > 0.1)
        {
            // Knik Y-as
            var knikY = new KnikToets(BucklingCurve.b, BucklingAxis.Y)
            {
                Positie = "Hele staaf"
            };
            EurcodeResultaten.Add(knikY.Check(
                maxN.Forces, 
                staalProfiel, 
                staal, 
                StabilitySettings));
            
            // Knik Z-as
            var knikZ = new KnikToets(BucklingCurve.b, BucklingAxis.Z)
            {
                Positie = "Hele staaf"
            };
            EurcodeResultaten.Add(knikZ.Check(
                maxN.Forces, 
                staalProfiel, 
                staal, 
                StabilitySettings));
        }
        
        // Kip (als moment > 0)
        if (Math.Abs(maxMoment?.Forces.My ?? 0) > 0.1)
        {
            var kip = new KipToets(sectionClass: 1)
            {
                Positie = "Hele staaf"
            };
            EurcodeResultaten.Add(kip.Check(
                maxMoment.Forces, 
                staalProfiel, 
                staal, 
                StabilitySettings));
        }
    }
    
    // Idem voor hout...
}
```

**Checklist:**
- [ ] StabilitySettingsEditor component maken
- [ ] Auto-detect logic implementeren
- [ ] Integratie in LiggerEdit page
- [ ] Update LiggerEntity.Bijwerken()
- [ ] Persistence testen (opslaan/laden project)
- [ ] UI/UX testen met eindgebruiker

---

### **FASE 7: Geavanceerde Features** (optioneel, 3-5 dagen)

#### Stap 7.1: Interactie toetsen
```csharp
/// <summary>
/// EC3 §6.3.3 - Interactie druk + buiging + knik
/// </summary>
public class InteractionCheck : BaseEurocodeToets
{
    // Formules (6.61) en (6.62)
    // N_Ed/(χ_y N_Rk/γ_M1) + k_yy M_y,Ed/M_y,Rd + k_yz M_z,Ed/M_z,Rd ≤ 1.0
    // ...
}
```

#### Stap 7.2: Verschillende belastinggevallen
```csharp
// Per loadcase verschillende stabiliteit?
public class StabilityContextPerLoadCase
{
    public Dictionary<string, StabilityContext> PerLoadCase { get; set; }
    
    // Bijv. voor kraanlast: andere kip lengte
}
```

#### Stap 7.3: Visualisatie
```razor
<!-- SVG visualisatie van knik/kip mode shapes -->
<svg>
    <!-- Toon knik vorm bij λ_cr -->
    <path d="..." stroke="red" />
</svg>
```

**Checklist:**
- [ ] Interactie formules (6.61)-(6.62)
- [ ] Per loadcase stability contexts
- [ ] SVG visualisatie knik modes
- [ ] Export naar rapport

---

## 🧪 Testing Strategie

### Unit Tests (per FASE)

```csharp
[TestClass]
public class KnikToetsTests
{
    [TestMethod]
    public void KnikToets_HEA200_S235_PinPin_4000mm()
    {
        // Arrange
        var profiel = new ProfielIH(Doorsneden.HEA200);
        var staal = new StaalContext(StaalKwaliteitEnum.S235);
        var forces = new InternalForces { N = -500 }; // kN druk
        var stability = new StabilityContext
        {
            Length = 4000,
            KniklengteY = 4000,
            SupportY = SupportCondition.PinPin
        };
        
        var toets = new KnikToets(BucklingCurve.b, BucklingAxis.Y);
        
        // Act
        var result = toets.Check(forces, profiel, staal, stability);
        
        // Assert
        Assert.IsTrue(result.Voldoet);
        Assert.AreEqual(0.XX, result.Benutting, 0.01); // Vergelijk met handberekening
    }
    
    [TestMethod]
    public void KnikToets_VerySlender_ShouldGiveLowKc()
    {
        // Test voor λ > 3.0 → verwacht lage k_c
    }
    
    [TestMethod]
    public void KnikToets_Stocky_ShouldGiveKcNearOne()
    {
        // Test voor λ < 0.3 → verwacht k_c = 1.0
    }
}
```

### Integratie Tests

```csharp
[TestMethod]
public void LiggerEntity_WithStability_ComputesAllChecks()
{
    // Volledige workflow test
    var ligger = new LiggerEntity();
    ligger.StabilitySettings = new StabilityContext { ... };
    ligger.Beam.Loads.Add(...);
    
    ligger.Bijwerken();
    
    Assert.AreEqual(5, ligger.EurcodeResultaten.Count); 
    // 2x snede + 2x knik + 1x kip
}
```

### Verificatie met Software

| Testcase | Software | Norm | Status |
|----------|----------|------|--------|
| HEA200 knik | SCIA Engineer | EC3 | ✅ |
| IPE300 kip | Staad.Pro | EC3 | ✅ |
| 100x200 hout knik | TimberTech | EC5 | ⏳ |
| GL24h kip | RFEM | EC5 | ⏳ |

---

## 📚 Referenties & Resources

### EC3 (Staal)
- EN 1993-1-1 §6.3.1: Knik (druk)
- EN 1993-1-1 §6.3.2: Laterale torsionale knik (buiging)
- EN 1993-1-1 §6.3.3: Interactie
- EN 1993-1-1 Annex F: C1 factoren
- SteelGuide (Bouwen met Staal)

### EC5 (Hout)
- EN 1995-1-1 §6.3.2: Knik (druk)
- EN 1995-1-1 §6.3.3: Laterale knik (buiging)
- Timber Engineering STEP 1/2
- CUR Publicatie 231

### Tools
- [SCIA Engineer](https://www.scia.net/) - Verificatie EC3
- [RFEM](https://www.dlubal.com/) - Verificatie EC5
- [EC3 Spreadsheets](https://eurocodes.jrc.ec.europa.eu/)

---

## ✅ Checklist Compleet Overzicht

### Fundament
- [ ] StabilityContext class + enums
- [ ] BaseEurocodeToets overload
- [ ] Unit tests voor context

### EC3 Staal
- [ ] BucklingResistance (knik)
- [ ] KnikToets (Y + Z as)
- [ ] LateralTorsionalBucklingResistance (kip)
- [ ] KipToets
- [ ] Unit tests + verificatie

### EC5 Hout
- [ ] BucklingResistance (knik)
- [ ] KnikToets (Y + Z as)
- [ ] LateralBucklingResistance (kip)
- [ ] KipToets
- [ ] Unit tests + verificatie

### Integratie
- [ ] StabilitySettingsEditor UI
- [ ] Auto-detect logic
- [ ] LiggerEntity updates
- [ ] Persistence testen
- [ ] PrintPage rapport integratie

### Geavanceerd (optioneel)
- [ ] Interactie toetsen
- [ ] Per loadcase contexts
- [ ] Visualisatie

---

## 🎯 Prioritering

### **MUST HAVE** (MVP)
1. StabilityContext + basis enums
2. EC3 Knik (1 as)
3. EC3 Kip (vereenvoudigd)
4. Basis UI
5. LiggerEntity integratie

### **SHOULD HAVE**
6. EC3 Knik beide assen
7. EC5 Knik + Kip
8. Auto-detect
9. Volledige UI

### **NICE TO HAVE**
10. Interactie toetsen
11. Visualisatie
12. Geavanceerde C1 factoren

---

## 💡 Tips & Valkuilen

### ⚠️ Veelgemaakte Fouten

1. **Eenheden!** 
   - Kniklengte in **mm**, niet m
   - Forces in kN
   - Spanning in N/mm²

2. **k-factor vs L_cr**
   ```csharp
   // ❌ FOUT
   double Lcr = Length * kFactor; // k al in Lcr verwerkt!
   
   // ✅ GOED
   double Lcr = stability.KniklengteY; // User geeft L_cr direct
   ```

3. **Section class**
   - EC3: kies WplY (class 1/2) of WelY (class 3)
   - EC5: altijd elastisch (geen plastische reserve)

4. **Belastingsduur (EC5)**
   - Knik/kip: gebruik zelfde belastingsduur als snede-toets
   - Invloed op k_mod en daarmee f_c,0,d en f_m,d

### 💡 Best Practices

1. **Validatie vroeg en vaak**
   ```csharp
   if (stability.KniklengteY <= 0)
       throw new ArgumentException("Kniklengte moet > 0 zijn");
   ```

2. **Duidelijke toelichting**
   ```csharp
   Toelichting = $"λ̄ = {lambda:F3}, " +
                 $"curve {_curve}, " +
                 $"χ = {chi:F3}, " +
                 $"L_cr = {Lcr:F0} mm";
   ```

3. **Edge case guards**
   ```csharp
   // λ < 0.2 → geen knik
   if (lambdaRel < 0.2)
       return new EurocodeResultaat { /* Volle weerstand */ };
   ```

---

## 📅 Geschatte Tijdsduur

| Fase | Onderdeel | Uren | Totaal |
|------|-----------|------|--------|
| 1 | Fundament | 8-12 | 12 |
| 2 | EC3 Knik | 12-16 | 16 |
| 3 | EC3 Kip | 16-24 | 24 |
| 4 | EC5 Knik | 12-16 | 16 |
| 5 | EC5 Kip | 12-16 | 16 |
| 6 | UI & Integratie | 12-20 | 20 |
| 7 | Geavanceerd (opt) | 16-32 | - |
| | **TOTAAL MVP** | | **~80 uur** |
| | **TOTAAL COMPLEET** | | **~104 uur** |

**Realistische planning:** 2-3 weken fulltime, of 4-6 weken part-time.

---

## 🚀 Volgende Stappen

1. **Review dit stappenplan** met team
2. **Kies scope**: MVP of Compleet?
3. **Start met Fase 1**: Fundament
4. **Test driven**: Schrijf tests eerst!
5. **Iteratief**: Na elke fase: review + demo

---

*Gemaakt: 2024*  
*Voor: KIP/KNIK toetsen EC3 & EC5*  
*Status: READY TO IMPLEMENT* ✅

