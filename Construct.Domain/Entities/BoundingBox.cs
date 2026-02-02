using Construct.Domain.Extensions;
using static Construct.Domain.Entities.SvgHelper;

namespace Construct.Domain.Entities
{
    //using static Construct.WebUI.SvgHelper;

    public abstract class SvgBoxBase 
    {
        public double MinX { get; set; }
        public double MinY { get; set; }

        public abstract double Width { get;  }
        public abstract double Height { get;  }

        public double MaxX => MinX + Width;
        public double MaxY => MinY + Height;
    }


    public class SvgViewBox : SvgBoxBase
    {
        public double ViewWidth { get; set; }
        public double ViewHeight { get; set; }
        public override double Width => ViewWidth;

        public override double Height => ViewHeight;

        


    }


    public class BoundingBox : SvgBoxBase
    {
        //public double MinX { get; set; } = -0.001;
        //public double MinY { get; set; } = -0.001;
        public double MaxXValue { get; set; } = 0.001;
        public double MaxYValue { get; set; } = 0.001;

        public void Add(double x, double y)
        {
            if (x < MinX) MinX = x;
            if (y < MinY) MinY = y;
            if (x > MaxX) MaxXValue = x;
            if (y > MaxY) MaxYValue = y;
        }

        public void Add(BoundingBox other)
        {
            Add(other.MinX, other.MinY);
            Add(other.MaxX, other.MaxY);
        }

        public static BoundingBox Empty => new BoundingBox
        {
            MinX = 0,
            MinY = 0,
            MaxXValue = 1,
            MaxYValue = 1
        };

        public override double Width => MaxXValue - MinX;
        public override double Height => MaxYValue - MinY;


        public BoundingBox Clone()
        {
            return new BoundingBox
            {
                MinX = this.MinX,
                MinY = this.MinY,
                MaxXValue = this.MaxXValue,
                MaxYValue = this.MaxYValue
            };
        }


        public void Reset()
        {
            MinX = 0;
            MinY = 0; 
            MaxXValue = 1;
            MaxYValue = 1;
        }

        //public SvgViewBox ToSvgViewBox() => new(MinX, MinY, Width, Height);
       

        public string Render(string stroke = "red", double strokeWidth = 0.5, string dash = "3 3")
        {
           

            return
                $"<rect x=\"{MinX.ToSvg()}\" y=\"{MinY.ToSvg()}\" width=\"{Width.ToSvg()}\" height=\"{Height.ToSvg()}\" " +
                $"fill=\"none\" stroke=\"{stroke}\" vector-effect=\"non-scaling-stroke\" stroke-width=\"{strokeWidth.ToSvg()}\" stroke-dasharray=\"{dash}\" />";
        }

        

    }

}
