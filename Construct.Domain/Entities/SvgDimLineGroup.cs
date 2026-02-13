using System.Collections.Generic;

namespace Construct.Domain.Entities
{
    /// <summary>
    /// Groep van opeenvolgende maatlijnen die dezelfde eigenschappen delen.
    /// Bijvoorbeeld: betonrand → buitenrand beugel → buitenrand beugel → betonrand
    /// </summary>
    public class SvgDimLineGroup : BaseSvg
    {
        /// <summary>
        /// Lijst van punten waartussen de maatlijnen worden getekend.
        /// Minimaal 2 punten nodig, elk paar opeenvolgende punten wordt een maatlijn.
        /// </summary>
        public List<(double X, double Y)> Points { get; set; } = new();
        
        public double Scale { get; set; } = 1.0;
        public string? MarkerStart { get; set; } = "chevStart";
        public string? MarkerEnd { get; set; } = "chevEnd";
        
        /// <summary>
        /// Offset in viewBox-units voor alle maatlijnen
        /// </summary>
        public double Offset { get; set; } = 0.0;
        
        /// <summary>
        /// Extra offset in aantal tekstregels voor alle maatlijnen
        /// </summary>
        public int OffsetLines { get; set; } = 0;
        
        /// <summary>
        /// Mode (Aligned, Horizontal, Vertical) voor alle maatlijnen
        /// </summary>
        public DimLineMode Mode { get; set; } = DimLineMode.Aligned;
        
        /// <summary>
        /// Optionele custom teksten per segment. Als null/leeg, wordt automatisch afstand berekend.
        /// </summary>
        public List<string>? Texts { get; set; }
        
        public string? Stroke { get; set; }
        public double StrokeWidth { get; set; } = 1;
        public bool ShowExtensionLines { get; set; } = true;
        public string StringFormat { get; set; } = "0";
        
        protected override string TagName => "g";
        
        /// <summary>
        /// Genereert individuele SvgDimLine objecten voor elk segment
        /// </summary>
        public List<SvgDimLine> GenerateDimLines()
        {
            var dimLines = new List<SvgDimLine>();
            
            if (Points.Count < 2)
                return dimLines;
            
            for (int i = 0; i < Points.Count - 1; i++)
            {
                var p1 = Points[i];
                var p2 = Points[i + 1];
                
                var dimLine = new SvgDimLine
                {
                    X1 = p1.X,
                    Y1 = p1.Y,
                    X2 = p2.X,
                    Y2 = p2.Y,
                    Scale = Scale,
                    MarkerStart = MarkerStart,
                    MarkerEnd = MarkerEnd,
                    Offset = Offset,
                    OffsetLines = OffsetLines,
                    Mode = Mode,
                    StrokeWidth = StrokeWidth,
                    ShowExtensionLines = ShowExtensionLines,
                    StringFormat = StringFormat
                };
                
                // Custom tekst indien beschikbaar
                if (Texts != null && i < Texts.Count && !string.IsNullOrEmpty(Texts[i]))
                {
                    dimLine.Text = Texts[i];
                }
                
                // Custom stroke color indien opgegeven
                if (!string.IsNullOrEmpty(Stroke))
                {
                    dimLine.StrokeColor = Stroke;
                }
                
                dimLines.Add(dimLine);
            }
            
            return dimLines;
        }

        public override BoundingBox GetBoundingBox()
        {
            return new BoundingBox();
            throw new NotImplementedException();
        }

      
    }
}
