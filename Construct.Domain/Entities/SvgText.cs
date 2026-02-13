using Construct.Domain.Extensions;

namespace Construct.Domain.Entities
{
    using System.Text;

    public class SvgText : BaseSvg
    {
        protected override string TagName => "text";

        /// <summary>
        /// Hoofdtekst
        /// </summary>
        public string Text { get; set; } = "?";

        // Positie
        public double X { get; set; }
        public double Y { get; set; }
        public double Angle { get; set; }
        
        /// <summary>
        /// Verticale Offset van de text van 0 tot 1 x de Textgrootte
        /// </summary>
        public double? DY { get; set; } 

        /// <summary>
        /// Horizontale offset van de text van 0 tot 1 keer de textgrootte
        /// </summary>
        public double? DX { get; set; } 


        // Opties
        public double Scale { get; set; } = 1;
        public double Pts { get; set; } = 12;
        public string? FontFamily { get; set; } = null;
        public double FontSize => Pts / Scale;
        public double LineHeight { get; set; } = 1.2;
        public string Anchor { get; set; } = "middle"; // start | middle | end
        public string DominantBaseLine { get; set; } = "middle";
        public string Fill { get; set; } = "var(--neutral-foreground-rest, black)";



        // Evenetueel tekstbox
        public double X1 { get; set; }
        public double X2 { get; set; }
        public double Y1 { get; set; }
        public double Y2 { get; set; }


        public SvgText() { }

        /// <summary>
        /// Voor <text /></text> elementen 
        /// </summary>
        /// <param name="text">De tekst</param>
        /// <param name="x">x-positie</param>
        /// <param name="y">y-positie</param>
        /// <param name="x2">optioneel voor richting</param>
        /// <param name="y2">optioneel voor richting</param>
        /// <param name="angle">hoek in graden CW</param>
        /// <param name="scale">schaal van de tekening</param>
        /// <param name="anchor">horizontale uitlijning  (start middle end)</param>
        /// <param name="dx">verplaatsing x</param>
        /// <param name="dy">verplaatsing y</param>
        /// <param name="dominantBaseLine">vertikale uitlijning (base middle hanging)</param>
        /// <param name="pts">tekstgrootte in pts. Standaard is 12pts. Groot is 16pts en klein is 8pts</param>
        public SvgText(
            string text = "?",
            double x = 0, double y = 0,
            double x2 = double.NaN, double y2 = double.NaN,
            double angle = 0,
            double scale = 1,
            string anchor = "middle",
            double? dx = null,
            double? dy= null,
            string dominantBaseLine = "middle",
            double pts = 12)
        {
            Text = text;
            Scale = scale;
            Anchor = anchor;
            DominantBaseLine = dominantBaseLine;
            DX = dx;
            DY = dy;
            Pts = pts;

            if (!double.IsNaN(x2) && !double.IsNaN(y2))
            {
                X1 = x; X2 = x2;
                Y1 = y; Y2 = y2;
                X = (x + x2) / 2.0;
                Y = (y + y2) / 2.0;
                Angle = Math.Atan2(y2 - y, x2 - x) * 180.0 / Math.PI;
            }
            else
            {
                X = x;
                Y = y;
                Angle = angle;
            }
        }



        //public SvgText(
        //    double scale)
        //{
        //    Scale = scale;
        //}

        //public SvgText(string text, double x, double y, double angle = 0, double scale = 100) 
        //    : this (scale)
        //{
        //    Text = text;
        //    X = x;
        //    Y = y;
        //    Angle = angle;
        //}

        //public SvgText(string text, double x1, double y1, double x2, double y2, double scale = 100)
        //    : this(scale)
        //{
        //    Text = text;
        //    X1 = x1;
        //    X2 = x2;
        //    Y1 = y1;
        //    Y2 = y2;
        //    X = (x1 + x2) / 2.0;
        //    Y = (y1 + y2) / 2.0;
        //    Angle = Math.Atan2(y2 - y1, x2 - x1) * 180.0 / Math.PI;
        //}

        /// <summary>
        /// Voegt de tekst toe aan een bounding box (ruwe schatting).
        /// </summary>
        public void AddToBoundingBox(BoundingBox bb, double scale)
        {
            double textSize = FontSize / scale;
            string[] lines = Text.Split('\n');

            double totalHeight = textSize * LineHeight * lines.Length;
            double maxWidth = lines.Max(l => l.Length) * (textSize * 0.6);

            double halfW = maxWidth / 2.0;
            double halfH = totalHeight / 2.0;

            // Text zit gecentreerd rond (X,Y)
            bb.Add(X - halfW, Y - halfH);
            bb.Add(X + halfW, Y + halfH);
        }


        public string ToSvgBox()
        {
            //double boxHeight = FontSize / scale;
            
            
            double w = X2 - X1;
            double h = Y2 - Y1;


            // teken een rechthoek van P1 naar P2
            return $"<rect x=\"{X1}\" y=\"{Y1}\" width=\"{w}\" height=\"{h}\" />";
        }


        /// <summary>
        /// Geeft SVG XML terug ZONDER multiline support! dit buiten deze method regelen of later
        /// </summary>
        public string ToSvg(double scale)
        {
            double textSize = FontSize / scale;
            //string[] lines = Text.Split('\n');

            var sb = new StringBuilder();
            sb.AppendLine($@"<text");
            sb.AppendLine($@" x=""{X.ToSvg()}"" y=""{Y.ToSvg()}""");
            sb.AppendLine(@$" text-anchor=""{Anchor}""");
            sb.AppendLine(@$" dominant-baseline=""{DominantBaseLine}""");

            sb.AppendLine(@$" font-size=""{textSize.ToSvg()}""");
            
            if (FontFamily != null)
                sb.AppendLine(@$" font-family=""{FontFamily}""");
            
            sb.AppendLine(@$" fill=""{Fill}""");

            if (DX.HasValue)
            {
                sb.AppendLine($@" dx=""{DX:0.###}""");
            }
            if (DY.HasValue)
            {
                sb.AppendLine($@" dy=""{DY:0.###}""");
            }


            if (Angle != 0)
            {
                sb.AppendLine(@$" transform=""rotate({Angle.ToSvg()},{X.ToSvg()},{Y.ToSvg()})""");
            }

            sb.AppendLine($@">");

           

            //sb.AppendLine(
            //    $@"<text x=""{X.ToSvg()}"" y=""{Y.ToSvg()}"" text-anchor=""{Anchor}"" font-size=""{textSize.ToSvg()}"" 

            //font-family=""{FontFamily}""
            //fill=""{Fill}""
            //transform=""rotate({Angle.ToSvg()},{X.ToSvg()},{Y.ToSvg()})"">");

            //for (int i = 0; i < lines.Length; i++)
            //{
            //    double dy = i == 0 ? 0 : textSize * LineHeight;
            //    sb.AppendLine($@"  <tspan x=""{X.ToSvg()}"" dy=""{dy.ToSvg()}"">{lines[i]}</tspan>");
            //}
            sb.AppendLine(Text);
            sb.AppendLine("</text>");
            return sb.ToString();
        }

        protected override void ApplyAttributes()
        {
            Set("x", X);
            Set("y", Y);
            Set("dx", DX);
            Set("dy", DY);
            Set("text-anchor", Anchor);
            Set("dominant-baseline", DominantBaseLine);
            Set("font-size", FontSize);
            Set("font-family", FontFamily);
            Set("fill", Fill);

            if (Angle != 0)
                Set("transform", "rotate({0},{1},{2})", Angle, X, Y);

            // Value of inner text opslaan als een speciaal attribuut
            // BaseSvg kan dit renderen in Render()
            Attributes["__InnerText"] = Text;
        }

        /// <summary>
        /// Renderen inclusief inner text
        /// </summary>
        public string Render(double scale)
        {
            // Gebruik ToSvg(scale) voor correcte schaling
            return ToSvg(scale);
        }

        /// <summary>
        /// Rendert tekst zonder schaling (tekst schaalt mee met SVG).
        /// Dit is de standaard Render() van BaseSvg.
        /// </summary>
        public override string Render()
        {
            // Gebruik interne Scale property als deze is ingesteld
            //if (Scale > 0 && Scale != 1.0)
            //{
            //    return ToSvg(Scale);
            //}

            // Anders gewone render zonder schaling (scale = 1)
            //return base.Render();
            return ToSvg(1.0);
        }

        public override BoundingBox GetBoundingBox()
        {
            var bb = new BoundingBox();

            if (string.IsNullOrEmpty(Text))
                return bb;

            //
            // 1. Schat tekstbreedte en hoogte
            //
            double width = 0.6 * FontSize * Text.Length;
            double height = FontSize;

            //
            // 2. Bepaal ongetransformeerde bbox (voor transformaties)
            //
            double xmin = X;
            double ymin = Y - height; // baseline = onder, bovenkant = Y - height
            double xmax = X + width;
            double ymax = Y;

            //
            // 3. Text-anchor horizontale correctie
            //
            switch (Anchor)
            {
                case "middle":
                    xmin = X - width / 2.0;
                    xmax = X + width / 2.0;
                    break;

                case "end":
                    xmin = X - width;
                    xmax = X;
                    break;

                    // "start" is standaard: xmin = X, xmax = X + width
            }

            //
            // 4. Dominant-baseline correctie (alleen hanging)
            //
            if (DominantBaseLine == "hanging")
            {
                ymin = Y;          // bovenkant op baseline
                ymax = Y + height; // onderkant
            }

            //
            // 5. Hoekpunten van de ongetransformeerde bbox
            //
            var pts = new (double x, double y)[]
            {
        (xmin, ymin),
        (xmax, ymin),
        (xmax, ymax),
        (xmin, ymax)
            };

            //
            // 6. Bouw transformatie:
            //    1) dx / dy
            //    2) rotate(angle) rond het text-anchor punt (X+dx, Y+dy)
            //
            double cx = X + (DX ?? 0);   // rotatiepivot
            double cy = Y + (DY ?? 0);

            Matrix2D m =
                Matrix2D.Translate(DX ?? 0, DY ?? 0)
                .Multiply(
                    Matrix2D.Translate(cx, cy)
                    .Multiply(Matrix2D.Rotate(Angle))
                    .Multiply(Matrix2D.Translate(-cx, -cy))
                );

            //
            // 7. Transformeer alle hoekpunten
            //
            foreach (var p in pts)
            {
                var (tx, ty) = m.Transform(p.x, p.y);
                bb.Add(tx, ty);
            }

            return bb;
        }



        //public override BoundingBox GetBoundingBoxOLD()
        //{
        //    var bb = new BoundingBox();

        //    if (string.IsNullOrEmpty(Text))
        //        return bb;

        //    double width = 0.6 * FontSize * Text.Length;
        //    double height = FontSize;

        //    // SVG text anchor point is the *baseline* at (X, Y)
        //    double xmin = X;
        //    double ymin = Y - height; // bovenkant van tekst
        //    double xmax = X + width;
        //    double ymax = Y; // baseline

        //    if (DominantBaseLine == "hanging")
        //    {
        //        ymax = Y + height;
        //    }


        //    bb.Add(xmin, ymin);
        //    bb.Add(xmax, ymax);

        //    return bb;
        //}

        public struct Matrix2D
        {
            public double A, B, C, D, E, F;

            public Matrix2D(double a, double b, double c, double d, double e, double f)
            {
                A = a; B = b;
                C = c; D = d;
                E = e; F = f;
            }

            public static Matrix2D Identity => new Matrix2D(1, 0, 0, 1, 0, 0);

            public static Matrix2D Translate(double tx, double ty)
                => new Matrix2D(1, 0, 0, 1, tx, ty);

            public static Matrix2D Rotate(double angleDeg)
            {
                double a = angleDeg * Math.PI / 180.0;
                double cos = Math.Cos(a);
                double sin = Math.Sin(a);
                return new Matrix2D(cos, sin, -sin, cos, 0, 0);
            }

            public Matrix2D Multiply(Matrix2D m)
            {
                return new Matrix2D(
                    A * m.A + C * m.B,
                    B * m.A + D * m.B,
                    A * m.C + C * m.D,
                    B * m.C + D * m.B,
                    A * m.E + C * m.F + E,
                    B * m.E + D * m.F + F
                );
            }

            public (double x, double y) Transform(double x, double y)
            {
                return (
                    A * x + C * y + E,
                    B * x + D * y + F
                );
            }
        }


    }


}
