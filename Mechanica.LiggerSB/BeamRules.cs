using CommonLibrary.Interfaces;
using CommonLibrary.Models;
using Eurocode.Belastingen;
using Eurocode.BetonConstructies;
using Microsoft.AspNetCore.Localization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;



namespace Mechanica.SimpleBeam
{

    /// <summary>
    /// Represents an envelope point with full internal forces
    /// </summary>
    public readonly record struct BeamEnvelopePoint(
        double Position,
        CommonLibrary.Models.InternalForces Forces,
        BeamResult Source)
    {
        public string SourceName => Source.CombinationName;
        
        // Convenience properties voor directe toegang tot krachten
        public double N => Forces.N;
        public double Vy => Forces.Vy;
        public double Vz => Forces.Vz;
        public double My => Forces.My;
        public double Mz => Forces.Mz;
        public double T => Forces.T;
        
        // Legacy compatibility (backwards compatible met oude PrimaryValue/AssociatedValue)
        [Obsolete("Gebruik Forces.My of My property")]
        public double Moment => Forces.My;
        
        [Obsolete("Gebruik Forces.Vz of Vz property")]
        public double Shear => Forces.Vz;
    }

    public static class LoadExtensions
    {
        public static IEnumerable<ILoad> GetSimilarShape(this IEnumerable<ILoad> loads, double tolerance = 1e-6)
        {
            return loads.Distinct(new LoadShapeComparer(tolerance));
        }

        

      
    }




    public class LoadShapeComparer : IEqualityComparer<ILoad>
    {
        private readonly double _tolerance;

        public LoadShapeComparer(double tolerance = 1e-6)
        {
            _tolerance = tolerance;
        }

        public bool Equals(ILoad? x, ILoad? y)
        {
            if (x == null || y == null) return false;

            // Name gelijk
            if (x.Name != y.Name) return false;

            // Range gelijk binnen tolerantie
            if (Math.Abs(x.Range.Start - y.Range.Start) > _tolerance ||
                Math.Abs(x.Range.End - y.Range.End) > _tolerance)
                return false;

            // Controleer vorm
            bool xFlat = Math.Abs(x.Range.Start - x.Range.End) < _tolerance;
            bool yFlat = Math.Abs(y.Range.Start - y.Range.End) < _tolerance;
            if (xFlat != yFlat) return false;

            bool xRising = x.Range.Start < x.Range.End;
            bool yRising = y.Range.Start < y.Range.End;
            if (!xFlat && xRising != yRising) return false;

            return true;
        }

        public int GetHashCode(ILoad obj)
        {
            if (obj == null)
                return 0;

            // Name hash safe
            int nameHash = obj.Name?.GetHashCode() ?? 0;

            // Range null-safe
            
            double start = obj.Range.Start;
            double end = obj.Range.End;

            // Normaliseren zodat (5→10) dezelfde hash heeft als (10→5)
            double min = Math.Min(start, end);
            double max = Math.Max(start, end);

            // Ronde af om hash-explosie door double ruis te voorkomen
            double minRound = Math.Round(min, 6);
            double maxRound = Math.Round(max, 6);

            return HashCode.Combine(nameHash, minRound, maxRound);
        }

    }




    public enum SchemaType { VrijOpgelegd, Uitkraging}
    public enum SupportType { Pin, Fixed, None }

    public interface ILoad : INotifyPropertyChanged
    {
        string Id { get; }
        double TotalForce { get; }
        double ForceArmFromStart { get; }
        double PartialMomentUpTo(double x);
        string Name { get; set; }
        double OffsetY { get; set; }


        double StartMagnitude { get; set; }
        double EndMagnitude { get; set; }

        // Magnitude is readonly en wordt automatisch berekend in concrete class
        double Magnitude { get; }
        double Factor { get; set; }
        string Unit { get; }
       

        // De geometrische projectie op de balk-as
        (double Start, double End) Range { get; }

        // Convenience-properties
        double StartX => Range.Start;
        double EndX => Range.End;
        double Length => Range.End - Range.Start;
        string UserFriendlyValue
        {
            get
            {
                if (StartMagnitude == EndMagnitude)
                {
                    return StartMagnitude.ToString("0.0", CultureInfo.InvariantCulture);
                }
                else
                {
                    return ($"{StartMagnitude:0.0} - {EndMagnitude:0.0}");
                }
            }
        }

        string UserFriendlyStartValue
        {
            get
            {
                return StartMagnitude.ToString("0.0", CultureInfo.InvariantCulture);
            }
        }

        string UserFriendlyEndValue
        {
            get
            {
                if (this is PointLoad)
                {
                    return "—";
                }
                return EndMagnitude.ToString("0.0", CultureInfo.InvariantCulture);
            }
        }

        string UserFriendlyStartPos
        {
            get
            {
                return StartX.ToString("0.000", CultureInfo.InvariantCulture);
            }
        }

        string UserFriendlyFromTo
        {
            get
            {
                return UserFriendlyStartPos + "-" + UserFriendlyEndPos;
            }
        }

        string UserFriendlyFromToValue
        {
            get
            {
                return UserFriendlyValue;
            }
        }

        string UserFriendlyEndPos
        {
            get
            {
                if (this is PointLoad) { 
                    return "—";
                }
                return EndX.ToString("0.000", CultureInfo.InvariantCulture);
            }
        }


        BelastingGeval? LoadCase { get; } // link naar Load.Case
        string? Description { get; }
        ILoad CloneWeighted();

    }

    


    public class MovingPointLoad : PointLoad
    {
        public MovingPointLoad(double position, double magnitude, string name = "F" ) 
            : base(position, magnitude, name)
        {
        }


        public double StartPos { get; set; }
        public double EndPos { get; set; }
        public override (double Start, double End) Range => (StartPos, EndPos);
        public override double PartialMomentUpTo(double x) => x <= Range.Start ? 0 : Magnitude * x;


    }



    public class PointLoad : ILoad
    {
        public ILoad CloneWeighted()
        {
            return new PointLoad(this.Magnitude, this.LoadCase)
            {
                Name = this.Name,
                Magnitude = this.Magnitude * this.Factor,
                Factor = 1.0,
                Position = this.Position,
            };
        }



        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));


        private readonly string _id = Guid.NewGuid().ToString("N");
        public string Id => _id;
        public string Unit => "kN";

        public BelastingGeval? LoadCase { get; set; }

        private double _magnitude;
        public double Magnitude
        {
            get => _magnitude;
            set
            {
                if (_magnitude != value)
                {
                    _magnitude = value;
                    StartMagnitude = value;
                    EndMagnitude = value;
                    OnPropertyChanged();
                }
               
            }
        }
        public double Factor { get; set; } = 1.0;
       

        public PointLoad(double magnitude, BelastingGeval? loadCase)
        {
            Name = "?";
            _magnitude = magnitude;
            StartMagnitude = magnitude;
            EndMagnitude = magnitude;
            LoadCase = loadCase;
        }



        public string Name { get; set; }
        public string Description { get; set; }
        public double OffsetY { get; set; }
        public double StartMagnitude { get; set; }
        public double EndMagnitude { get; set; }
        

        private double _position;
        public double Position 
        { 
            get => _position;
            set
            {
                if (_position != value)
                {
                    _position = value;
                    OnPropertyChanged();
                }
            }
        }
        public virtual (double Start, double End) Range =>  (Position, Position);

        
        public PointLoad(double position, double magnitude, string name = "F") 
        { 
            Position = position; 
            Magnitude = magnitude; 
            Name = name;
        }
        
        
        public double TotalForce => Magnitude;
        public double ForceArmFromStart => Position;

        //string ILoad.Id { get => Id; init => throw new NotImplementedException(); }

        public virtual double PartialMomentUpTo(double x) => x <= Position ? 0 : Magnitude * (x - Position);
    }


    
    public class DistributedLoad : ILoad
    {
        public ILoad CloneWeighted()
        {
            return new DistributedLoad(this.StartPosition, this.EndPosition, this.StartMagnitude * Factor, this.EndMagnitude * Factor)
            {
                Name = this.Name,
                Factor = 1.0, // clone is klaar voor gebruik
                LoadCase = this.LoadCase,
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private readonly string _id = Guid.NewGuid().ToString("N");
        public string Id => _id;

        public string Unit => "kN/m¹";
        public BelastingGeval? LoadCase { get; set; }
        
        
        
        public string Name { get; set; }
        public string Description { get; set; }


        // is UI offset, kijken of we dit buiten deze class kunnen houden, voor nu werkt het prima
        public double OffsetY { get; set; }
        
        
        public (double Start, double End) Range => (StartPosition, EndPosition);

        private double _startPosition;
        public double StartPosition 
        {
            get => _startPosition;
            set
            {
                if (_startPosition != value)
                {
                    _startPosition = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _endPosition;
        public double EndPosition 
        { 
            get => _endPosition;
            set
            {
                if (_endPosition != value)
                {
                    _endPosition = value;
                    OnPropertyChanged();
                }
            } 
        }

        private bool _keepUniform = true;
        public bool KeepUniform
        {
            get => _keepUniform;
            set
            {
                if (_keepUniform != value)
                {
                    _keepUniform = value;
                    if (value)
                    {
                        EndMagnitude = StartMagnitude;
                    }
                }
            }
        }

        private bool _isUniform = true;
        public bool IsUniform
        {
            get => _isUniform;
            set
            {
                if (_isUniform != value)
                {
                    _isUniform = value;
                    if (value)
                    {
                        EndMagnitude = StartMagnitude;
                    }
                }
            }
        }

        private double _startMagnitude;
        public double StartMagnitude 
        { 
            get => _startMagnitude;
            set
            {
                if(_startMagnitude != value)
                {
                    _startMagnitude = value;
                    if (Math.Abs(EndMagnitude - value) < 1e-12)
                    {
                        _isUniform = true;
                    }
                    else
                    {
                        _isUniform = false;
                    }
                    OnPropertyChanged();
                }
            } 
        }

        private double _endMagnitude;
        public double EndMagnitude
        {
            get => _endMagnitude;
            set 
            { 
                if (_endMagnitude != value)
                {
                    _endMagnitude = value;
                    if (Math.Abs(StartMagnitude - value) < 1e-12)
                    {
                        _isUniform = true;
                    }
                    else
                    {
                        _isUniform = false;
                    }
                    OnPropertyChanged();
                }
            }
        }


        // ----------
        // -- HELPERS
        // ----------

        private static double MaxValueAbs => 1e9;
        private static double MinLength => 0.1;
        //public static double MinStartPosition => 0;
        public double MaxStartPosition => EndPosition - MinLength;
        public double MinEndPostion => StartPosition + MinLength;
        // NB. MaxS

        public double MinStartMagnitude
        {
            get
            {
                if (EndMagnitude <= 0) return -MaxValueAbs;
                else return 0;
            }
        }
        public double MaxStartMagnitude
        {
            get
            {
                if (EndMagnitude >= 0) return MaxValueAbs;
                else return 0;

            }
        }

        public double MinEndMagnitude
        {
            get
            {
                if (StartMagnitude <= 0) return -MaxValueAbs;
                else return 0;
            }
        }
        public double MaxEndMagnitude
        {
            get
            {
                if (StartMagnitude >= 0) return MaxValueAbs;
                else return 0;

            }
        }



        public double Factor { get; set; } = 1.0;

        // Magnitude = grootste absolute extremum
        public double Magnitude
        {
            get
            {
                // We willen de grootste absolute waarde, maar behouden het teken
                return Math.Abs(StartMagnitude) > Math.Abs(EndMagnitude)
                    ? StartMagnitude
                    : EndMagnitude;
            }
        }



        public (double X, string Anchor) LabelPosition
        {
            get
            {
                var x = 0.0;
                var xMargin = 0.05;
                string anchor = "middle";
                if (StartMagnitude == EndMagnitude) 
                { 
                    x = (EndPosition + StartPosition) / 2.0; 
                    anchor = "middle"; 
                }
                else
                {
                    var a = Math.Abs(StartMagnitude);
                    var b = Math.Abs(EndMagnitude);
                    if (a > b)
                    {
                        x = StartPosition + xMargin;
                        anchor = "start";
                    }
                    else 
                    {
                        x = EndPosition - xMargin;
                        anchor = "end";
                    }
                }
                return (x, anchor);
            }
        }

        public DistributedLoad(BelastingGeval? geval, string name, double start, double end, double magnitude)
        {
            LoadCase = geval;
            Name = name;
            StartPosition = start;
            EndPosition = end;
            StartMagnitude = magnitude;
            EndMagnitude = magnitude;
        }

        public DistributedLoad(double start, double end, double magnitude)
        {
            StartPosition = start;
            EndPosition = end;
            StartMagnitude = magnitude;
            EndMagnitude = magnitude;
        }

        public DistributedLoad(double start, double end, double startMag, double endMag, string name = "P")
        { 
            StartPosition = start; 
            EndPosition = end; 
            StartMagnitude = startMag; 
            EndMagnitude = endMag; 
            Name = name;
        }

        public double TotalForce => 0.5 * (StartMagnitude + EndMagnitude) * (EndPosition - StartPosition);

        public double ForceArmFromStart
        {
            get
            {
                double L = EndPosition - StartPosition;
                if (Math.Abs(StartMagnitude - EndMagnitude) < 1e-12)
                    return StartPosition + L / 2.0;
                return StartPosition + L * (2 * StartMagnitude + EndMagnitude) / (3 * (StartMagnitude + EndMagnitude));
            }
        }

        public double PartialMomentUpTo(double x)
        {
            if (x <= StartPosition) return 0;
            double xEnd = Math.Min(x, EndPosition);
            double Lx = xEnd - StartPosition;

            if (Math.Abs(StartMagnitude - EndMagnitude) < 1e-12)
                return StartMagnitude * Lx * (x - StartPosition - Lx / 2.0);
            else
            {
                double q1 = StartMagnitude;
                double q2 = StartMagnitude + (EndMagnitude - StartMagnitude) * (Lx / (EndPosition - StartPosition));
                double area = 0.5 * (q1 + q2) * Lx;
                double centroid = Lx * (2 * q1 + q2) / (3 * (q1 + q2));
                return area * (x - StartPosition - centroid);
            }
        }
    }


    public sealed class ResultCollection
    {
        private readonly List<BeamResult> _results = [];

        public IReadOnlyList<BeamResult> All => _results;

        public void Add(BeamResult result)
            => _results.Add(result);

        // 🔍 elementaire queries
        public IEnumerable<BeamResult> ForLoadCase(BelastingGeval geval)
            => _results.Where(r => r.LoadCase == geval);

        public IEnumerable<BeamResult> ForCombination(BelastingCombinatie combi)
            => _results.Where(r => r.Combination == combi);

        public IEnumerable<BeamResult> FundamenteleCombinatie()
        {
            return ForCombinationType(BelastingCombinatieTypeEnum.Fundamenteel_A | BelastingCombinatieTypeEnum.Fundamenteel_B);
        }

        public IEnumerable<BeamResult> ForCombinationType(BelastingCombinatieTypeEnum type)
        {
            return _results.Where(r =>
                r.Combination?.Type != null &&
                (r.Combination.Type & type) != 0
            );
        }


        // 🎯 ENVELOPE METHODEN - Maximum/Minimum met volledige details

        /// <summary>
        /// Vindt het maximum (meest negatieve) moment over alle resultaten of voor specifiek type
        /// </summary>
        public BeamEnvelopePoint? GetMaxMoment(BelastingCombinatieTypeEnum? type = null)
        {
            var results = type.HasValue ? ForCombinationType(type.Value) : _results;
            
            BeamEnvelopePoint? maxPoint = null;
            double maxAbsMoment = 0;




            foreach (var result in results)
            {

                var rMax = result.MomentDiagram.OrderByDescending(md => Math.Abs(md.M)).First();
                if (Math.Abs(rMax.M) > maxAbsMoment || maxPoint == null)
                {
                    maxAbsMoment = Math.Abs(rMax.M);

                    var shearFromDiagram = result.ShearDiagram.FirstOrDefault(sd => sd.x == rMax.x);
                    var forces = new CommonLibrary.Models.InternalForces
                    {
                        My = rMax.M,
                        Vz = shearFromDiagram.V,
                        // N, Vy, Mz, T blijven 0 voor 2D analyse
                    };

                    maxPoint = new BeamEnvelopePoint(rMax.x, forces, result);

                }

                //foreach (var (x, M) in result.MomentDiagram)
                //{
                //    if (Math.Abs(M) > maxAbsMoment || maxPoint == null)
                //    {
                //        maxAbsMoment = Math.Abs(M);

                //        var shearFromDiagram = result.ShearDiagram.FirstOrDefault(sd => sd.x == x);


                //        var (shear, _) = result.ShearAt(x);

                //        shear = shearFromDiagram.V;

                        

                //        var forces = new CommonLibrary.Models.InternalForces
                //        {
                //            My = M,
                //            Vz = shear,
                //            // N, Vy, Mz, T blijven 0 voor 2D analyse
                //        };
                        
                //        maxPoint = new BeamEnvelopePoint(x, forces, result);
                //    }
                //}
            }

            return maxPoint;
        }

        /// <summary>
        /// Vindt het minimum moment (kleinste absolute waarde)
        /// </summary>
        public BeamEnvelopePoint? GetMinMoment(BelastingCombinatieTypeEnum? type = null)
        {
            var results = type.HasValue ? ForCombinationType(type.Value) : _results;
            
            BeamEnvelopePoint? minPoint = null;
            double minAbsMoment = double.MaxValue;

            foreach (var result in results)
            {
                foreach (var (x, M) in result.MomentDiagram)
                {
                    if (Math.Abs(M) < minAbsMoment)
                    {
                        minAbsMoment = Math.Abs(M);
                        var (shear, _) = result.ShearAt(x);
                        
                        var forces = new CommonLibrary.Models.InternalForces
                        {
                            My = M,
                            Vz = shear,
                        };
                        
                        minPoint = new BeamEnvelopePoint(x, forces, result);
                    }
                }
            }

            return minPoint;
        }

        /// <summary>
        /// Vindt het maximum (meest negatieve) dwarskracht over alle resultaten of voor specifiek type
        /// </summary>
        public BeamEnvelopePoint? GetMaxShear(BelastingCombinatieTypeEnum? type = null)
        {
            var results = type.HasValue ? ForCombinationType(type.Value) : _results;
            
            BeamEnvelopePoint? maxPoint = null;
            double maxAbsShear = 0;

            foreach (var result in results)
            {
                foreach (var (x, V) in result.ShearDiagram)
                {
                    if (Math.Abs(V) > maxAbsShear || maxPoint == null)
                    {
                        maxAbsShear = Math.Abs(V);
                        double moment = result.MomentAt(x);
                        
                        var forces = new CommonLibrary.Models.InternalForces
                        {
                            Vz = V,
                            My = moment,
                        };
                        
                        maxPoint = new BeamEnvelopePoint(x, forces, result);
                    }
                }
            }

            return maxPoint;
        }

        /// <summary>
        /// Vindt het minimum dwarskracht (kleinste absolute waarde)
        /// </summary>
        public BeamEnvelopePoint? GetMinShear(BelastingCombinatieTypeEnum? type = null)
        {
            var results = type.HasValue ? ForCombinationType(type.Value) : _results;
            
            BeamEnvelopePoint? minPoint = null;
            double minAbsShear = double.MaxValue;

            foreach (var result in results)
            {
                foreach (var (x, V) in result.ShearDiagram)
                {
                    if (Math.Abs(V) < minAbsShear)
                    {
                        minAbsShear = Math.Abs(V);
                        double moment = result.MomentAt(x);
                        
                        var forces = new CommonLibrary.Models.InternalForces
                        {
                            Vz = V,
                            My = moment,
                        };
                        
                        minPoint = new BeamEnvelopePoint(x, forces, result);
                    }
                }
            }

            return minPoint;
        }

        /// <summary>
        /// Geeft volledige moment envelope (max en min op elke positie)
        /// </summary>
        public IReadOnlyList<(double x, double MaxM, double MinM, BeamResult MaxSource, BeamResult MinSource)> 
            GetMomentEnvelope(BelastingCombinatieTypeEnum type)
        {
            var results = ForCombinationType(type).ToList();
            if (!results.Any()) return Array.Empty<(double, double, double, BeamResult, BeamResult)>();

            // Verzamel alle unieke x-posities
            var positions = results
                .SelectMany(r => r.MomentDiagram.Select(m => m.x))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var envelope = new List<(double, double, double, BeamResult, BeamResult)>();

            foreach (var x in positions)
            {
                double maxM = double.MinValue;
                double minM = double.MaxValue;
                BeamResult? maxSource = null;
                BeamResult? minSource = null;

                foreach (var result in results)
                {
                    double M = result.MomentAt(x);
                    
                    if (M > maxM)
                    {
                        maxM = M;
                        maxSource = result;
                    }
                    if (M < minM)
                    {
                        minM = M;
                        minSource = result;
                    }
                }

                if (maxSource != null && minSource != null)
                    envelope.Add((x, maxM, minM, maxSource, minSource));
            }

            return envelope;
        }

        /// <summary>
        /// Geeft volledige dwarskracht envelope (max en min op elke positie)
        /// </summary>
        public IReadOnlyList<(double x, double MaxV, double MinV, BeamResult MaxSource, BeamResult MinSource)> 
            GetShearEnvelope(BelastingCombinatieTypeEnum type)
        {
            var results = ForCombinationType(type).ToList();
            if (!results.Any()) return Array.Empty<(double, double, double, BeamResult, BeamResult)>();

            // Verzamel alle unieke x-posities
            var positions = results
                .SelectMany(r => r.ShearDiagram.Select(s => s.x))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var envelope = new List<(double, double, double, BeamResult, BeamResult)>();

            foreach (var x in positions)
            {
                double maxV = double.MinValue;
                double minV = double.MaxValue;
                BeamResult? maxSource = null;
                BeamResult? minSource = null;

                foreach (var result in results)
                {
                    var (v1, v2) = result.ShearAt(x);
                    double V = Math.Max(Math.Abs(v1), Math.Abs(v2)) * Math.Sign(v1);
                    
                    if (V > maxV)
                    {
                        maxV = V;
                        maxSource = result;
                    }
                    if (V < minV)
                    {
                        minV = V;
                        minSource = result;
                    }
                }

                if (maxSource != null && minSource != null)
                    envelope.Add((x, maxV, minV, maxSource, minSource));
            }

            return envelope;
        }


       



    }




    public class BeamResultCollection : Dictionary<string, BeamResult>
    {
        public void AddResult(BeamResult result)
        {
            this[result.CombinationName] = result;
        }
    }


    public class BeamResult
    {
        public BelastingGeval? LoadCase { get; }
        public BelastingCombinatie? Combination { get; }
        public string CombinationName { get; }

        public double LeftReaction { get; }
        public double RightReaction { get; }

        /// <summary>
        /// Factor voor toevallige inklemming (0.0 - 1.0). Standaard 0.15 (15%).
        /// Dit representeert kleine inkastringsmomenten door onvolkomenheden in steunpunten.
        /// Het moment op positie x wordt gecorrigeerd als: M_corrected = M - factor * |M_min|
        /// </summary>
        public double AccidentalFixityFactor { get; set; } = 0.15;

        // Optioneel: voorgedefinieerde diagram-samples
        public IReadOnlyList<(double x, double V)> ShearDiagram { get; }
        
        /// <summary>
        /// Normaal momentendiagram (zonder toevallige inklemming)
        /// </summary>
        public IReadOnlyList<(double x, double M)> MomentDiagram { get; }
        
        /// <summary>
        /// Momentendiagram met toevallige inklemming-correctie
        /// Nuttig voor veiligere dimensionering
        /// </summary>
        public IReadOnlyList<(double x, double M)> MomentDiagramAccidentalFixity { get; }
        
        public IReadOnlyList<(double x, double W)> DeflectionDiagram { get; }

        private readonly SBLigger _beamClone; // bevat toestand voor deze combinatie


        public BeamResult(BelastingGeval loadCase, SBLigger beam)
        {
            LoadCase = loadCase;
            Combination = null;
            CombinationName = loadCase.Naam; // verwijderen?
            // Neem reactions over zoals berekend
            LeftReaction = beam.StartVerticalReaction;
            RightReaction = beam.EndVerticalReaction;
            _beamClone = beam.CloneForResult();
            ShearDiagram = SampleShear(beam);
            (MomentDiagram, MomentDiagramAccidentalFixity) = SampleMoment(beam, AccidentalFixityFactor);
        }

        public BeamResult(BelastingCombinatie combination, SBLigger beam, double accidentalFixityFactor = 0.15)
        {
            Combination = combination;
            CombinationName = combination.Naam; // verwijderen?
            AccidentalFixityFactor = accidentalFixityFactor;

            // Neem reactions over zoals berekend
            LeftReaction = beam.StartVerticalReaction;
            RightReaction = beam.EndVerticalReaction;

            // Bewaar een lichte kopie van de beam
            // (alleen nodig voor ShearAt/MomentAt)
            _beamClone = beam.CloneForResult(); // NIET MEER NODIG?

            // Wil je diagrammen vooraf opslaan?
            ShearDiagram = SampleShear(beam);
            
            // Toevallige inklemming alleen voor Fundamenteel combinaties
            bool isFundamental = (combination.Type & BelastingCombinatieTypeEnum.Fundamenteel_A) != 0 ||
                                 (combination.Type & BelastingCombinatieTypeEnum.Fundamenteel_B) != 0;
            
            (MomentDiagram, MomentDiagramAccidentalFixity) = SampleMoment(beam, isFundamental ? AccidentalFixityFactor : 0.0);
            DeflectionDiagram = SampleDeflection(beam);

            // Convienience properties
            var myMin = MomentDiagram.Min(i => i.M);
            var myMax = MomentDiagram.Max(i => i.M);

            

            beam.MyEd = (Math.Min(myMin, beam.MyEd.Item1), Math.Max(myMax, beam.MyEd.Item2));

            var vzMin = ShearDiagram.Min(i => i.V);
            var vzMax = ShearDiagram.Max(i => i.V);

            beam.VzEd = (Math.Min(vzMin, beam.VzEd.Item1), Math.Min(vzMax, beam.VzEd.Item2));
        }

        public (double, double) ShearAt(double x) => _beamClone.ShearAt(x);
        public double MomentAt(double x) => _beamClone.MomentAt(x);

        private static List<(double x, double V)> SampleShear(SBLigger beam)
        {
            var list = new List<(double x, double V)>();
            double L = beam.Length;

            var positions = beam.GetPositions();
            positions.Add(beam.GetShearZeroPosition());

            foreach (var pos in positions.OrderBy(p=>p))
            {
                double? recalculatedVertReaction = null;
                // als er een kraanlast zit dan eerste de correcte reactie uitrekenen.
                if (beam.Loads.OfType<MovingPointLoad>().Any())
                {
                    beam.Compute(); // CRASH! 
                }


                var v = beam.ShearAt(pos);
                list.Add((pos,v.Item1));

                // sprong
                if (v.Item2 != v.Item1)
                {
                    list.Add((pos, v.Item2));
                }

            }

            //for (int i = 0; i <= 10; i++)
            //{
            //    double x = L * i / 10.0;
            //    var shearX = beam.ShearAt(x);
            //    list.Add((x, shearX.Item1));
            //    if (shearX.Item1 != shearX.Item2)
            //    {
            //        list.Add((x, shearX.Item2));
            //    }
            //}
            return list;
        }


        private static List<(double x, double W)> SampleDeflection(SBLigger beam)
        {
            var list = new List<(double x, double M)>();
            var positions = beam.GetPositions(8);
            positions.Add(beam.GetShearZeroPosition());
            foreach (var pos in positions.OrderBy(p => p).Distinct())
            {
                list.Add((pos, beam.DeflectionAt(pos)));
            }
            return list;
        }


        /// <summary>
        /// Genereert twee momentendiagrammen: normaal en met toevallige inklemming
        /// </summary>
        /// <returns>Tuple van (MomentDiagram, MomentDiagramAccidentalFixity)</returns>
        private static (List<(double x, double M)>, List<(double x, double M)>) SampleMoment(SBLigger beam, double accidentalFixityFactor = 0.15)
        {
            var normalMoments = new List<(double x, double M)>();
            var accidentalMoments = new List<(double x, double M)>();

            var positions = beam.GetPositions(8);
            positions.Add(beam.GetShearZeroPosition());

            // Stap 1: Verzamel normale momenten om het minimum te vinden
            var normalValues = new List<double>();
            foreach (var pos in positions.Distinct())
            {
                normalValues.Add(beam.MomentAt(pos));
            }

            // Stap 2: Bepaal het kleinste absolute moment (voor toevallige inklemming)
            // Als accidentalFixityFactor = 0, slaan we de correctie over
            double minAbsMoment = (accidentalFixityFactor > 0 && normalValues.Count > 0)
                ? Math.Abs(normalValues.Min(m => m))
                : 0;

            // Stap 3: Sample beide diagrammen met continue waarden
            foreach (var pos in positions.OrderBy(p=>p).Distinct())
            {
                double M = beam.MomentAt(pos);
                
                // Toevallige inklemming: M_corrected = M - factor * |M_min|
                // Dit maakt het moment negatiever (ongunstiger)
                // Alleen toepassen als accidentalFixityFactor > 0
                double correction = accidentalFixityFactor * minAbsMoment;
                double MWithAccidentalFixity = M + correction;
                
                normalMoments.Add((pos, M));
                
                // Als accidentalFixityFactor = 0, voeg hetzelfde moment toe (geen correctie)
                if (accidentalFixityFactor > 0)
                {
                    accidentalMoments.Add((pos, MWithAccidentalFixity));
                }
                else
                {
                    accidentalMoments.Add((pos, M));
                }
            }
            
            return (normalMoments, accidentalMoments);
        }
    }



    public class SBLigger : BaseDirtyTracking
    {
        public SBLigger(BelastingenContext? loadContext = null)
        {
            LoadContext = loadContext;

            // Luister naar add/remove
            Loads.CollectionChanged += Loads_CollectionChanged;

            // BELANGRIJK: bestaande items koppelen
            foreach (var load in Loads)
                load.PropertyChanged += Load_PropertyChanged;
        }

        private void Loads_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Add / Remove / Reset → altijd dirty
            IsDirty = true;

            // Nieuwe loads → PropertyChanged koppelen
            if (e.NewItems != null)
            {
                foreach (ILoad load in e.NewItems)
                    load.PropertyChanged += Load_PropertyChanged;
            }

            // Verwijderde loads → PropertyChanged ontkoppelen
            if (e.OldItems != null)
            {
                foreach (ILoad load in e.OldItems)
                    load.PropertyChanged -= Load_PropertyChanged;
            }
        }

        private void Load_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Interne wijziging → dirty
            IsDirty = true;
        }


        
        [Obsolete("Gebruik ResultCollection in plaats van ResultCollectionLegacy")]
        [JsonIgnore]
        public BeamResultCollection ResultCollectionLegacy { get; set; } = [];

        [JsonIgnore]
        public ResultCollection ResultCollection { get; set; } = new();


        public SBLigger CloneGeometryOnly()
        {
            // BELANGRIJK ALLEEN GEOMETRIE
            return new SBLigger() { 
                Length = this.Length, 
                StartSupport = this.StartSupport, 
                EndSupport = this.EndSupport,
                EI = this.EI
            };
        }

        public SBLigger CloneForResult()
        {
            // BELANGRIJK:
            // Alleen geometrie + weighted loads moeten worden gekopieerd.
            // GEEN combinaties, GEEN factoren, alleen pure toestand.

            var copy = new SBLigger(LoadContext)
            {
                Length = this.Length, 
                StartSupport = this.StartSupport,
                EndSupport = this.EndSupport,
                EI = this.EI
            };

            foreach (var load in this.Loads)
                copy.Loads.Add(load.CloneWeighted()); // clone met al toegepaste factor

            //copy.LeftReaction = this.LeftReaction;
            //copy.RightReaction = this.RightReaction;

            return copy;
        }


        private SchemaType? _schematisering = SchemaType.VrijOpgelegd;
        public SchemaType? Schematisering
        {
            get => _schematisering;
            set
            {
                // SetProperty retourneert true als de waarde echt veranderd is
                if (SetProperty(ref _schematisering, value))
                {
                    // Pas hier je start/end supports aan
                    switch (value)
                    {
                        case SchemaType.VrijOpgelegd:
                            StartSupport = SupportType.Pin;
                            EndSupport = SupportType.Pin;
                            break;
                        case SchemaType.Uitkraging:
                            StartSupport = SupportType.Fixed;
                            EndSupport = SupportType.None;
                            // of andere logica voor ingeklemd
                            break;
                    }
                }
            }
        }

        private double _eI = 21000.0;
        public double EI
        {
            get => _eI;
            set => SetProperty(ref _eI, value);
        }

        private double _length = 1.0;
        public double Length
        {
            get => _length;
            set => SetProperty(ref _length, value);
        }

        private SupportType _startSupport = SupportType.Pin;
        public SupportType StartSupport
        {
            get => _startSupport;
            set => SetProperty(ref _startSupport, value);
        }


        private SupportType _endSupport = SupportType.Pin;
        public SupportType EndSupport
        {
            get => _endSupport;
            set => SetProperty(ref _endSupport, value);
        }

        private BelastingenContext? _loadContext = null;
        public BelastingenContext? LoadContext
        {
            get => _loadContext;
            set => SetProperty(ref _loadContext, value);
        }


        public ObservableCollection<ILoad> Loads { get; internal set; } = new();

        /// <summary>
        /// Convienience property voor MyEd (max. moment)
        /// </summary>
        public (double, double) MyEd { get; internal set; }
        public (double, double) VzEd { get; internal set; } 

        public double StartVerticalReaction { get; internal set; }
        public double EndVerticalReaction { get; internal set; }
        public double StartFixMoment { get; internal set; } = 0;  // Steunpuntsmoment aan de linkerzijde (positief = steunpunt)
        public double EndFixMoment { get; internal set; } = 0;    // Steunpuntsmoment aan de rechterzijde (positief = steunpunt)

        private BaseProfiel? _profiel;

        [JsonIgnore] // TODO introduceer ProfielState voor persistence
        public BaseProfiel? Profiel
        {
            get => _profiel;
            set => SetProperty(ref _profiel, value);
        }

        private PlaatWapening? _plaatWapening;
        public PlaatWapening? PlaatWapening
        {
            get => _plaatWapening;
            set => SetProperty(ref _plaatWapening, value);
        }


        private BaseMateriaal? _materiaal;
        [JsonIgnore] // TODO introduceer MateriaalState voor persistence
        public BaseMateriaal? Materiaal
        {
            get => _materiaal;
            set => SetProperty(ref _materiaal, value);
        }


        public List<double> GetPositions(int numberOfMeshes = 0)
        {
            if (numberOfMeshes <= 0)
            {
                List<double> positions = [.. Loads
                .SelectMany(l => new[] { l.Range.Start, l.Range.End })
                .Where(v => !double.IsNaN(v)), 0.0, Length];
                return positions.Distinct().OrderBy(x => x).ToList();
            }

            if (numberOfMeshes == 1)
            {
                return [.. Loads
            .SelectMany(l => new[] { l.StartX, l.EndX, l.ForceArmFromStart })
            .Where(v => !double.IsNaN(v))
            .Distinct()
            .OrderBy(x => x)];
            }

            // numberOfMeshes > 1 → tussenpunten toevoegen
            var points = new List<double>();

            foreach (var l in Loads)
            {
                double a = l.StartX;
                double b = l.EndX;

                if (double.IsNaN(a) || double.IsNaN(b))
                    continue;

                // Altijd begin en eindpunt
                points.Add(a);
                points.Add(b);

                // Tussenliggende nodes
                double dx = (b - a) / numberOfMeshes;

                for (int i = 1; i < numberOfMeshes; i++)
                {
                    points.Add(a + i * dx);
                }
            }

            return points
                .Where(v => !double.IsNaN(v))
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }


        public void Compute()
        {
            if (LoadContext == null)
            {
                // geen context → gewoon 1 berekening
                ComputeReactions();
                IsDirty = false;
                return;
            }

            // controleer of er bc's zijn. Anders crash... dit moet beter AI
            if (LoadContext.BelastingCombinaties.Count == 0)
            {
                LoadContext.GenereerBelastingCombinaties(LoadContext, LoadContext.BelastingGevallen, LoadContext.CombinatiesTypes);
                Console.WriteLine("Belastingcombinaties opnieuw aangemaakt!");

            }

            // Meerdere combinaties
            BeamResultCollection results = new();
            ResultCollection resultCollection = new();
            foreach (var comb in LoadContext.BelastingCombinaties)
            {
                var r = ComputeForCombination(comb);
                results.AddResult(r);
                resultCollection.Add(r);
            }
            foreach (var geval in LoadContext.BelastingGevallen)
            {
                var r = ComputeForLoadCase(geval);
                resultCollection.Add(r);
            }
           

            this.ResultCollectionLegacy = results; // Opslaan in beam
            this.ResultCollection = resultCollection;
            IsDirty = false;
        }


        public BeamResult ComputeForLoadCase(BelastingGeval loadCase)
        {
            // 1. Maak gewogen kopieën
            var weightedLoads = new ObservableCollection<ILoad>();

            foreach (var load in Loads.Where(l=>l.LoadCase == loadCase))
            {
               // factor instellen
                load.Factor = 1.0;

                // gewogen clone maken
                var w = load.CloneWeighted();
                weightedLoads.Add(w);
            }

            // 2. Nieuwe beam maken voor deze combinatie
            var beamClone = this.CloneGeometryOnly();
            beamClone.Loads = weightedLoads;

            // 3. Reken deze ene combinatie
            beamClone.ComputeReactions();

            // 4. Teruggeven
            return new BeamResult(loadCase, beamClone);
        }

        public BeamResult ComputeForCombination(BelastingCombinatie comb)
        {
            // 1. Maak gewogen kopieën
            var weightedLoads = new ObservableCollection<ILoad>();

            foreach (var load in Loads)
            {
                // bepaal factor voor deze load
                double factor = 0.0;

                var match = comb.Items.FirstOrDefault(i => i.Geval == load.LoadCase);
                if (match != null)
                    factor = match.FactorNetto;

                // factor instellen
                load.Factor = factor;

                // gewogen clone maken
                var w = load.CloneWeighted();
                weightedLoads.Add(w);
            }

            // 2. Nieuwe beam maken voor deze combinatie
            var beamClone = this.CloneGeometryOnly();
            beamClone.Loads = weightedLoads;

            // 3. Reken deze ene combinatie
            beamClone.ComputeReactions();

            // 4. Teruggeven
            return new BeamResult(comb, beamClone);
        }


        public void ComputeReactions()
        {
            // Reset reacties
            StartVerticalReaction = 0;
            EndVerticalReaction = 0;
            StartFixMoment = 0;
            EndFixMoment = 0;

            double L = Length;
            double totalForce = Loads.Sum(l => l.TotalForce);

            
           
            
            double totalMomentAboutLeft = Loads.Sum(l => l.TotalForce * l.ForceArmFromStart);

            switch (StartSupport, EndSupport)
            {
                case (SupportType.Pin, SupportType.Pin):
                    // ΣFy = 0, ΣM_links = 0
                    EndVerticalReaction = -totalMomentAboutLeft / L;
                    StartVerticalReaction = -totalForce - EndVerticalReaction;
                    break;

                case (SupportType.Fixed, SupportType.Fixed):
                    // 2 onbekenden: RA, RB, steunpuntsmoment M_left, M_right
                    // ΣFy = 0: RA + RB = totalForce
                    // ΣM_links = 0: M_left + RB*L - Σ(loadMomentsAboutLeft) = 0
                    // ΣM_rechts = 0: M_right + RA*L - Σ(loadMomentsAboutRight) = 0

                    EndVerticalReaction = -totalMomentAboutLeft / L;
                    StartVerticalReaction = -totalForce - EndVerticalReaction;

                    // Steunpuntsmomenten exact (veldmoment wordt via PartialMomentUpTo berekend)
                    StartFixMoment = Loads.Sum(l => -l.TotalForce * Math.Pow(L - l.ForceArmFromStart, 2) / L);
                    EndFixMoment = Loads.Sum(l => -l.TotalForce * Math.Pow(l.ForceArmFromStart, 2) / L);
                    break;

                case (SupportType.Pin, SupportType.Fixed):
                    StartVerticalReaction = 0;
                    EndVerticalReaction = -totalForce;
                    EndFixMoment = Loads.Sum(l => l.TotalForce * (L - l.ForceArmFromStart));
                    break;

                case (SupportType.Fixed, SupportType.Pin):
                    StartVerticalReaction = -totalForce;
                    EndVerticalReaction = 0;
                    StartFixMoment = Loads.Sum(l => l.TotalForce * l.ForceArmFromStart);
                    break;

                case (SupportType.Fixed, SupportType.None):
                    // Cantilever
                    StartVerticalReaction = -totalForce;
                    EndVerticalReaction = 0;
                    StartFixMoment = Loads.Sum(l => -l.TotalForce * l.ForceArmFromStart);
                    break;

                case (SupportType.None, SupportType.Fixed):
                    // Omgekeerde cantilever
                    StartVerticalReaction = 0;
                    EndVerticalReaction = -totalForce;
                    EndFixMoment = Loads.Sum(l => l.TotalForce * (L-l.ForceArmFromStart));
                    break;

                default:

                    throw new ArgumentException("Onvoldoende steunen om de ligger in evenwicht te brengen.");
            }
        }

        public (double, double) ShearAt(double x, double? startVerticalReaction = null)
        {
            double vMinus = 0.0;
            double vPlus  = 0.0;
            const double eps = 1e-9;

    // --- Startoplegging op x = 0
    if (x > 0)
    {
        vMinus += startVerticalReaction ?? StartVerticalReaction;
        vPlus  += startVerticalReaction ?? StartVerticalReaction;
    }
    else if (Math.Abs(x) < eps)
    {
        vPlus += startVerticalReaction ?? StartVerticalReaction;
    }

    foreach (var load in Loads)
    {
        switch (load)
        {
            case PointLoad pl:
                if (pl.Range.End < x - eps)
                {
                    vMinus += pl.TotalForce;
                    vPlus  += pl.TotalForce;
                }
                else if (Math.Abs(pl.Position - x) < eps)
                {
                    vPlus += pl.TotalForce;
                }
                break;

            case DistributedLoad dl:
                if (x > dl.StartPosition + eps)
                {
                    double effectiveEnd = Math.Min(x, dl.EndPosition);
                    double L = effectiveEnd - dl.StartPosition;

                    if (L > 0)
                    {
                        double q1 = dl.StartMagnitude;
                        double q2 = dl.StartMagnitude +
                            (dl.EndMagnitude - dl.StartMagnitude) *
                            (L / (dl.EndPosition - dl.StartPosition));

                        double total = 0.5 * (q1 + q2) * L;

                        vMinus += total;
                        vPlus  += total;
                    }
                }
                break;
        }
    }

    // --- Eindoplegging op x = L
    if (Math.Abs(x - Length) < eps)
    {
        vPlus += EndVerticalReaction;
    }

    return (vMinus, vPlus);
        }


        double IntegrateSimpson(Func<double, double> f, double a, double b, int n = 12)
        {
            if (n % 2 != 0)
                throw new ArgumentException("Simpson requires an even number of steps.");

            double h = (b - a) / n;
            double sum = f(a) + f(b);

            for (int i = 1; i < n; i++)
            {
                double x = a + i * h;
                sum += (i % 2 == 0 ? 2 : 4) * f(x);
            }

            return sum * h / 3.0;
        }

        double RotationAt(double x)
        {
            return IntegrateSimpson(MomentAt, 0.0, x) / EI;
        }

        public double DeflectionAt(double x)
        {
            double w = IntegrateSimpson(RotationAt, 0.0, x);

            // Randvoorwaarden afdwingen
            if (StartSupport == SupportType.Fixed)
            {
                // θ(0) = 0
                double theta0 = RotationAt(0.0);
                w -= theta0 * x;
            }

            if (EndSupport == SupportType.Pin)
            {
                // zorg w(L) = 0 (numerieke correctie)
                double wL = IntegrateSimpson(RotationAt, 0.0, Length);
                w -= wL * x / Length;
            }

            return -w;
        }


        public double GetMinMoment()
        {
            var shearZeroPos = GetShearZeroPosition();
            return MomentAt(shearZeroPos);
        }

        public double GetShearZeroPosition()
        {
            var positions = GetPositions(2);
            double? previousShear = null;
            double? previousPos = null;
            foreach (var position in positions)
            {
                
                var shear = ShearAt(position);
                if (Math.Max(Math.Abs(shear.Item1), Math.Abs(shear.Item2)) <= 0.001)
                    return position;

                var thisShear = shear.Item1;

                if (previousShear != null && previousPos != null)
                {
                    double y1 = previousShear.Value;
                    double y2 = thisShear;
                    double x1 = previousPos.Value;        // ← deze moet je inderdaad onthouden
                    double x2 = position;

                    // tekenwisseling?
                    if ((y1 < 0 && y2 > 0) || (y1 > 0 && y2 < 0))
                    {
                        // t-vraag: waar snijdt de lijn y=0
                        double t = -y1 / (y2 - y1);

                        // bijbehorende X-positie
                        double xZero = x1 + (x2 - x1) * t;

                        return xZero;
                    }
                }




                previousShear = shear.Item2;
                previousPos = position;

            }

            // niks gevonden
            return Length / 2.0;
           
        }

        public double MomentAt(double x)
        {
            double m = StartFixMoment - StartVerticalReaction * x;

            foreach (var l in Loads)
                m -= l.PartialMomentUpTo(x);

            if (Math.Abs(x - Length) < 1e-9)
                m += EndFixMoment;

            return m; // Negatief = veldmoment, positief = steunpuntmoment
        }
    }





    public class Examples
    {
        public static void Run(bool withP = true, bool withRect = true, bool withTri = true)
        {
            double L1 = 4.0, L2 = 4.0, L3 = 2.0;
            double P = -100.0;
            double qRect = -10.0, qTriMax = -10.0;

            // Pin-Pin
            var pin = new SBLigger { Length = L1, StartSupport = SupportType.Pin, EndSupport = SupportType.Pin };
            if (withP) pin.Loads.Add(new PointLoad(2.0, P));
            if (withRect) pin.Loads.Add(new DistributedLoad(0, L1, qRect, qRect)); // rechthoek
            if (withTri)
            {
                pin.Loads.Add(new DistributedLoad(0, 2.0, 0, qTriMax)); // driehoek links
                pin.Loads.Add(new DistributedLoad(2.0, 4.0, qTriMax, 0)); // driehoek rechts
            }
            pin.ComputeReactions();
            PrintBeam("Pin-Pin", pin);


            // Fixed-Fixed
            var fix = new SBLigger { Length = L2, StartSupport = SupportType.Fixed, EndSupport = SupportType.Fixed };
            if (withP) fix.Loads.Add(new PointLoad(2.0, P));
            if (withRect) fix.Loads.Add(new DistributedLoad(0, L2, qRect, qRect));
            if (withTri)
            {
                fix.Loads.Add(new DistributedLoad(0, 2.0, 0, qTriMax));
                fix.Loads.Add(new DistributedLoad(2.0, 4.0, qTriMax, 0));
            }
            fix.ComputeReactions();
            PrintBeam("Fixed-Fixed", fix);

            // Fixed-Pin
            var fixPin = new SBLigger { Length = L2, StartSupport = SupportType.Fixed, EndSupport = SupportType.Pin };
            if (withP) fixPin.Loads.Add(new PointLoad(2.0, P));
            if (withRect) fixPin.Loads.Add(new DistributedLoad(0, L2, qRect, qRect));
            if (withTri)
            {
                fixPin.Loads.Add(new DistributedLoad(0, 2.0, 0, qTriMax));
                fixPin.Loads.Add(new DistributedLoad(2.0, 4.0, qTriMax, 0));
            }
            fixPin.ComputeReactions();
            PrintBeam("Fixed-Pin", fix);


            // Cantilever
            var cant = new SBLigger { Length = L3, StartSupport = SupportType.Fixed, EndSupport = SupportType.None };
            if (withP) cant.Loads.Add(new PointLoad(2.0, P));
            if (withRect) cant.Loads.Add(new DistributedLoad(0, L3, qRect, qRect));
            if (withTri) cant.Loads.Add(new DistributedLoad(0, 2.0, qTriMax, 0));
            cant.ComputeReactions();
            PrintBeam("Cantilever", cant);
        }

        static void PrintBeam(string name, SBLigger b)
        {
            double mid = b.Length / 2.0;
            Console.WriteLine($"\n=== {name} ===");
            Console.WriteLine($"Length={b.Length:F2}");
            Console.WriteLine($"Supports: Left={b.StartSupport}, Right={b.EndSupport}");

            Console.WriteLine($"Loads:");
            foreach (var l in b.Loads)
            {
                if (l is PointLoad pl)
                    Console.WriteLine($"  Point Load at {pl.Position:F2} m: {pl.Magnitude:F2} kN");
                else if (l is DistributedLoad dl)
                    Console.WriteLine($"  Distributed Load from {dl.StartPosition:F2} m to {dl.EndPosition:F2} m: {dl.StartMagnitude:F2} kN/m to {dl.EndMagnitude:F2} kN/m");
            }


            List<double> punten = new() { 0, mid, b.Length };

            
            Console.WriteLine($"R(A)={b.StartVerticalReaction:F2} kN, R(B)={b.EndVerticalReaction:F2} kN");

            foreach (var p in punten)
            {
                var (v1, v2) = b.ShearAt(p);
                double m = b.MomentAt(p);
                if (v1 == v2)
                    Console.WriteLine($"V({p:F2})={v1:F2} kN, M={m:F2} kNm");
                else
                    Console.WriteLine($"V({p:F2})={v1:F2} {v2:F2} kN, M={m:F2} kNm");
            }


            Console.WriteLine($"V(A)={b.ShearAt(0).Item1:F2}, V(c)={b.ShearAt(mid):F2}, V(B)={b.ShearAt(b.Length):F2}");
            Console.WriteLine($"M(A)={b.MomentAt(0):F2}, M(c)={b.MomentAt(mid):F2}, M(B)={b.MomentAt(b.Length):F2}");
        }
    
    }

    public class LoadLayoutService
    {
        private void PlaceLoads(
    List<ILoad> loads,
    List<List<ILoad>> layers,
    Func<ILoad, ILoad, bool> overlapPredicate,
    double verticalStep)
        {
            foreach (var load in loads)
            {
                int layerIndex = 0;
                bool placed = false;

                while (!placed)
                {
                    if (layerIndex >= layers.Count)
                        layers.Add(new List<ILoad>());

                    var layer = layers[layerIndex];

                    bool overlap = layer.Any(existing =>
                        overlapPredicate(load, existing));

                    if (!overlap)
                    {
                        layer.Add(load);
                        load.OffsetY = layerIndex * verticalStep;
                        placed = true;
                    }
                    else
                    {
                        layerIndex++;
                    }
                }
            }
        }


        public void PlaceLoadsInLayers(
            List<ILoad> loads,
            double minGap = 0.001,
            double verticalStep = 1)
        {
            var layers = new List<List<ILoad>>();

            var distrubutedLoads = loads.Where(l => l is DistributedLoad).ToList();
            var pointLoads = loads.Where(l => l is PointLoad).ToList();

            PlaceLoads(
                distrubutedLoads, 
                layers,
                overlapPredicate: (l1, l2) =>
                {
                    var (s1, e1) = l1.Range;
                    var (s2, e2) = l2.Range;
                    return !(e1 - minGap < s2 || s1 > e2 - minGap);
                },
                verticalStep
            );

            PlaceLoads(
                pointLoads,
                layers,
                overlapPredicate: (l1, l2) =>
                {
                    var (s1, e1) = l1.Range;
                    var (s2, e2) = l2.Range;
                    // Puntlast overlapt met een andere puntlast als ze exact op dezelfde positie zitten
                    if (Math.Abs(s1 - s2) < 1e-3)
                        return true;
                    // Puntlast overlapt met een verdeelde last als de positie binnen het bereik van de verdeelde last valt
                    return s1 >= s2 - minGap && s1 <= e2 + minGap;
                },
                verticalStep
            );


            
        }
    }





}

