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
                // note that TimeSpan can't represent less than 0.001ms
                // 10.1ms
                // 1.12ms
                // 0.123ms
                // 0.012ms
                // 0.001ms
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

            if (sec < 60) return ts.ToString(@"s\ \s\e\c", NumberFormatInfo.InvariantInfo);
            if (sec < 60 * 60) return ts.ToString(@"mm\:ss\ \m\i\n", NumberFormatInfo.InvariantInfo);
            if (sec < 24 * 60 * 60) return ts.ToString(@"hh\:mm\ \h", NumberFormatInfo.InvariantInfo);

            return ts.ToString(@"d\ \d\a\y\s\ hh\:mm\ \h", NumberFormatInfo.InvariantInfo);
        }

        public static bool IsCloseToWholeNumber(double d)
        {
            const double EPS_ZERO = 0.0001; // 0.01->1% 0.0001 ->0.01%
            d = Math.Abs(d);
            return Math.Abs(Math.Round(d) - d) < EPS_ZERO;
        }
    }
}
