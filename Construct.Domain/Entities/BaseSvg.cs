namespace Construct.Domain.Entities
{
    using System.Globalization;
    using System.Text;

    public abstract class BaseSvg
    {
        /// <summary>
        /// Map met alle SVG attributen die in de tag komen.
        /// Afgeleide classes vullen deze in via Set()-methodes.
        /// </summary>
        protected Dictionary<string, string> Attributes { get; } = [];
        public Dictionary<string, string> InteractiveAttributes { get; } = [];
        public string? Id { get; set; }
        public string? Class { get; set; }
        public string? Cursor { get; set; }
        public Dictionary<string, string> Data { get; } = [];

        /// <summary>
        /// Voor inner content.
        /// </summary>
        public SvgContent Content { get; } = new();

        /// <summary>
        /// De daadwerkelijk te renderen SVG-tag, zoals &lt;path&gt;, &lt;text&gt;, &lt;line&gt;, enz.
        /// </summary>
        protected abstract string TagName { get; }
        public string SvgTag => TagName; // publieke toegang voor bijvoorbeeld razor components


        public abstract BoundingBox GetBoundingBox();


        /// <summary>
        /// Moet worden overschreven door afgeleide classes om attributen te vullen.
        /// </summary>
        protected virtual void ApplyAttributes()
        {
            if (!string.IsNullOrWhiteSpace(Id))
                Attributes["id"] = Id;

            if (!string.IsNullOrWhiteSpace(Class))
                Attributes["class"] = Class;

            if (!string.IsNullOrWhiteSpace(Cursor))
                Attributes["style"] = $"cursor:{Cursor};";

            foreach (var (key, value) in Data)
                Attributes[$"data-{key}"] = value;
        }

        // --------------------------------------------------------------------
        //  SET HELPERS (mag je overal hergebruiken)
        // --------------------------------------------------------------------

        protected void Set<T>(string name, T? value)
        {
            if (value is null)
                return;
            string str = value switch
            {
                double d => d.ToString("0.####", CultureInfo.InvariantCulture),
                float f => f.ToString("0.##", CultureInfo.InvariantCulture),
                decimal m => m.ToString("0.######", CultureInfo.InvariantCulture),
                _ => Convert.ToString(value, CultureInfo.InvariantCulture)!,
            };
            if (string.IsNullOrWhiteSpace(str))
                return;

            Attributes[name] = str;
        }

        protected void Set(string name, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            Attributes[name] = value;
        }


        protected void Set(string name, string format, params object?[] args)
        {
            // null-args → niets doen
            if (args.Any(a => a is null))
                return;

            // veilig om te casten want nulls zijn eruit
            object[] castArgs = Array.ConvertAll(args, a => (object)a!);

            string value = string.Format(CultureInfo.InvariantCulture, format, castArgs);

            Set(name, value);
        }

        // --------------------------------------------------------------------
        //  GET HELPERS (voor externe rendering, zoals razor components)
        // --------------------------------------------------------------------
        
        public IReadOnlyDictionary<string, string> GetRenderAttributes()
        {
            // Bouw altijd fresh op voor render
            Attributes.Clear();
            ApplyAttributes();

            return Attributes;
        }



        // --------------------------------------------------------------------
        //  RENDER
        // --------------------------------------------------------------------

        /// <summary>
        /// Rendeert één SVG element als string.
        /// </summary>
        public virtual string Render()
        {
            Attributes.Clear();     // opnieuw opbouwen
            ApplyAttributes();      // afgeleide vult attributen

            var sb = new StringBuilder();
            sb.Append('<').Append(TagName);

            foreach (var kv in Attributes)
            {
                sb.Append(' ')
                  .Append(kv.Key)
                  .Append("=\"")
                  .Append(kv.Value)
                  .Append('"');
            }

            // Geen inner content → self closing />
            if (!Content.HasContent)
            {
                sb.Append(" />");
                return sb.ToString();
            }

            // Wel inner content → open tag + content + sluit tag dus   ><inner>....</inner> </tag>
            sb.Append('>')
              .Append(Content.Render())
              .Append("</")
              .Append(TagName)
              .Append('>');

            return sb.ToString();
        }
    }


    public static class SvgPathBoundingBoxCalculator
    {

        public static BoundingBox GetCombinedBoundingBox(IEnumerable<BaseSvg> items)
        {
            var combined = new BoundingBox();

            foreach (var svg in items)
            {
                var bb = svg.GetBoundingBox();  // jouw eigen
                combined.Add(bb);               // jouw Add(BoundingBox)
            }

            return combined;
        }


        public static BoundingBox GetBoundingBox(string d)
        {

            if (string.IsNullOrWhiteSpace(d))
                return BoundingBox.Empty;

            if (d.Contains("NaN"))
                return BoundingBox.Empty;

            var bbox = new BoundingBox();
            var tokenizer = new PathTokenizer(d);

            double x = 0, y = 0; // current point
            double subStartX = 0, subStartY = 0; // Startpunt voor 'Z'

            while (tokenizer.MoveNext())
            {
                char cmd = tokenizer.CurrentCommand;
                bool relative = char.IsLower(cmd);
                List<double> nums = tokenizer.CurrentNumbers;

                switch (char.ToUpper(cmd))
                {
                    case 'M':
                        for (int i = 0; i < nums.Count; i += 2)
                        {
                            double nx = nums[i];
                            double ny = nums[i + 1];
                            if (relative) { nx += x; ny += y; }
                            x = nx; y = ny;
                            bbox.Add(x, y);
                            
                            // M start nieuw subpath
                            subStartX = x;
                            subStartY = y;
                        }
                        break;

                    case 'L':
                        for (int i = 0; i < nums.Count; i += 2)
                        {
                            double nx = nums[i];
                            double ny = nums[i + 1];
                            if (relative) { nx += x; ny += y; }
                            x = nx; y = ny;
                            bbox.Add(x, y);
                        }
                        break;

                    case 'H':
                        foreach (var v in nums)
                        {
                            double nx = v;
                            if (relative) nx += x;
                            x = nx;
                            bbox.Add(x, y);
                        }
                        break;

                    case 'V':
                        foreach (var v in nums)
                        {
                            double ny = v;
                            if (relative) ny += y;
                            y = ny;
                            bbox.Add(x, y);
                        }
                        break;

                    case 'C':
                        for (int i = 0; i < nums.Count; i += 6)
                        {
                            // control1 = nums[i], nums[i+1]
                            // control2 = nums[i+2], nums[i+3]
                            double nx = nums[i + 4];
                            double ny = nums[i + 5];
                            if (relative) { nx += x; ny += y; }

                            // alleen eindpunt → veilige bounding, niet perfect
                            x = nx; y = ny;
                            bbox.Add(x, y);
                        }
                        break;

                    case 'Z':
                        // Sluit pad: ga terug naar begin van dit subpad
                        x = subStartX;
                        y = subStartY;
                        bbox.Add(x, y);
                        break;

                    default:
                        throw new Exception($"Unsupported command {cmd}");
                }
            }



            return bbox;
        }
    }


    public class PathTokenizer
    {
        private readonly string _data;
        private int _index;

        public char CurrentCommand { get; private set; }
        public List<double> CurrentNumbers { get; private set; } = [];

        public PathTokenizer(string data)
        {
            _data = data;
        }

        public bool MoveNext()
        {
            SkipWhitespace();

            if (_index >= _data.Length)
                return false;

            char c = _data[_index];

            // Nieuwe command letter
            if (IsCommandChar(c))
            {
                CurrentCommand = c;
                _index++;
            }
            // Herhalen van de vorige command
            else if (char.IsDigit(c) || c == '-' || c == '+' || c == '.')
            {
                // Command blijft hetzelfde
            }
            else
            {
                throw new Exception($"Unexpected character '{c}' in path.");
            }

            CurrentNumbers = ParseNumbers();
            return true;
        }

        private List<double> ParseNumbers()
        {
            List<double> numbers = [];

            while (true)
            {
                SkipWhitespace();
                if (_index >= _data.Length)
                    break;

                char c = _data[_index];

                if (IsCommandChar(c))
                    break; // volgende command

                if (char.IsDigit(c) || c == '-' || c == '+' || c == '.')
                {
                    numbers.Add(ParseDouble());
                }
                else if (c == ',')
                {
                    _index++;
                }
                else
                {
                    break;
                }
            }

            return numbers;
        }

        private double ParseDouble()
        {
            int start = _index;

            while (_index < _data.Length)
            {
                char c = _data[_index];
                if (!(char.IsDigit(c) || c == '-' || c == '+' || c == '.' || c == 'e' || c == 'E'))
                    break;
                _index++;
            }

            string numStr = _data[start.._index];
            return double.Parse(numStr, System.Globalization.CultureInfo.InvariantCulture);
        }

        private void SkipWhitespace()
        {
            while (_index < _data.Length && char.IsWhiteSpace(_data[_index]))
                _index++;
        }

        private bool IsCommandChar(char c)
            => "MLHVCZmlhvcz".Contains(c);
    }


}
