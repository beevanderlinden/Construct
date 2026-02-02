//namespace Construct.DELETE
//{
//    using static Construct.WebUI.Server.Components.Custom.SvgHelper;

//    public class BoundingBox
//    {
//        public double MinX { get; private set; } = double.MaxValue;
//        public double MinY { get; private set; } = double.MaxValue;
//        public double MaxX { get; private set; } = double.MinValue;
//        public double MaxY { get; private set; } = double.MinValue;

//        public void Add(double x, double y)
//        {
//            if (x < MinX) MinX = x;
//            if (y < MinY) MinY = y;
//            if (x > MaxX) MaxX = x;
//            if (y > MaxY) MaxY = y;
//        }

//        public void Add(BoundingBox other)
//        {
//            Add(other.MinX, other.MinY);
//            Add(other.MaxX, other.MaxY);
//        }

//        public double Width => MaxX - MinX;
//        public double Height => MaxY - MinY;


//        public void Reset()
//        {
//            MinX = double.MaxValue;
//            MinY = double.MaxValue;
//            MaxX = double.MinValue;
//            MaxY = double.MinValue;
//        }

//        public SvgViewBox ToSvgViewBox() => new(MinX, MinY, Width, Height);
//    }

//}
