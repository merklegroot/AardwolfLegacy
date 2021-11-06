using System;

namespace math_lib
{
    public static class MathUtil
    {
        public static bool IsBetween(decimal comparisonValue, decimal boundaryA, decimal boundaryB)
        {
            return (comparisonValue >= boundaryA && comparisonValue <= boundaryB)
                || (comparisonValue >= boundaryB && comparisonValue <= boundaryA);
        }

        public static bool IsWithinPercentDiff(decimal a, decimal b, decimal percent)
        {
            var diffAbs = Math.Abs(b - a);
            var ratio = percent / 100.0m;

            return (diffAbs / Math.Abs(a) < ratio) && (diffAbs / Math.Abs(b) < ratio);
        }

        public static decimal Truncate(decimal value, int places)
        {
            for (var i = 0; i < places; i++)
            {
                value *= 10.0m;
            }

            value = (long)value;

            for (var i = 0; i < places; i++)
            {
                value /= 10.0m;
            }

            return value;
        }

        public static decimal RoundUp(decimal value, int places)
        {
            var truncatedValue = Truncate(value, places);
            if (truncatedValue < value)
            {
                var tick = (decimal)(1.0 / Math.Pow(10.0d, places));
                truncatedValue += tick;
            }

            return truncatedValue;
        }

        public static decimal ConstrainToMultipleOf(decimal value, decimal lotSize)
        {
            var remainder = value % lotSize;
            return value - remainder;
        }
    }
}
