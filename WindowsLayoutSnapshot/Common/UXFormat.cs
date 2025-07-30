using System;
using System.Globalization;

namespace WindowsLayoutSnapshot
{
    internal static class UXFormat
    {
        public static string FormatShortPositiveDuration(TimeSpan ts)
        {
            if (ts < TimeSpan.Zero) ts = -ts;

            double t = ts.TotalMilliseconds;
            if (t == 0)
            {
                return "0ms";
            }

            if (t < 99)
            {
                int digitsAfterPoint = 1;
                double shifted = t;
                for (int i = 0; i < 2 && shifted < 10; i++)
                {
                    ++digitsAfterPoint;
                    shifted *= 10;
                }

                return t.ToString("F" + (char)(digitsAfterPoint + '0'), NumberFormatInfo.InvariantInfo) + "ms";
            }

            double sec = t / 1000;
            if (sec < 1)
            {
                return IsCloseToWholeNumber(t)
                    ? t.ToString("F0", NumberFormatInfo.InvariantInfo) + "ms"
                    : t.ToString("F", NumberFormatInfo.InvariantInfo) + "ms";
            }

            if (sec < 60)
            {
                ts = TimeSpan.FromSeconds(Math.Floor(ts.TotalSeconds));
                return ts.ToString(@"s'sec'", CultureInfo.InvariantCulture);
            }
            if (sec < 60 * 60)
            {
                ts = TimeSpan.FromSeconds(Math.Floor(ts.TotalSeconds));
                return ts.ToString(@"mm\:ss'min'", CultureInfo.InvariantCulture);
            }
            if (sec < 24 * 60 * 60)
            {
                ts = TimeSpan.FromMinutes(Math.Floor(ts.TotalMinutes));
                return ts.ToString(@"hh\:mm'h'", CultureInfo.InvariantCulture);
            }

            ts = TimeSpan.FromMinutes(Math.Floor(ts.TotalMinutes));
            return ts.ToString(@"d'days 'hh\:mm'h'", CultureInfo.InvariantCulture);
        }

        public static bool IsCloseToWholeNumber(double d)
        {
            const double EPS_ZERO = 0.0001;
            d = Math.Abs(d);
            return Math.Abs(Math.Round(d) - d) < EPS_ZERO;
        }
    }
}
