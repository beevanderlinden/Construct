namespace Construct.Domain.Common
{
    public static class MathExtensions
    {
        public static double ToRad(this double degrees) => degrees * Math.PI / 180.0;

        public static double ToDegree(this double radians) => radians * 180.0 / Math.PI;
    }
}
