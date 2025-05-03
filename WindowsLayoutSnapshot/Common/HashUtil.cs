using System.Collections.Generic;

namespace WindowsLayoutSnapshot
{
    internal static class HashUtil
    {
        private const uint Prime1 = 2654435761U;
        private const uint Prime2 = 2246822519U;
        private const uint Prime3 = 3266489917U;
        private const uint Prime4 = 668265263U;
        private const uint Prime5 = 374761393U;
        static uint[] primes = {
            11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97,
            101, 103, 107, 109, 113, 127, 131, 137, 139, 149, 151, 157, 163, 167, 173, 179, 181,
            191, 193, 197, 199, 211, 223, 227, 229
        };

        // returns pair (hash of position, full hash)

        public static KeyValuePair<long,long> HashValue(Snapshot.WINDOWPLACEMENT a)
        {
            /*
            public int length;
            public int flags;
            public int showCmd;
            public Point ptMinPosition;
            public Point ptMaxPosition;
            public RECT rcNormalPosition;
            */
            int i = 0;
            long h = 3;
            h = h * primes[i++] + a.rcNormalPosition.Right;
            h = h * primes[i++] + a.rcNormalPosition.Top;
            h = h * primes[i++] + a.rcNormalPosition.Left;
            h = h * primes[i++] + a.rcNormalPosition.Bottom;
            long h1 = h;
            h = h * primes[i++] + a.ptMinPosition.X;
            h = h * primes[i++] + a.ptMinPosition.Y;
            h = h * primes[i++] + a.ptMaxPosition.X;
            h = h * primes[i++] + a.ptMaxPosition.Y;
            h = h * primes[i++] + a.flags;
            h = h * primes[i++] + a.showCmd;
            long h2 = h;

            return new KeyValuePair<long, long>(h1,h2);
        }

        public static long Hash(Snapshot.WINDOWPLACEMENT a, bool positionOnly)
        {
            return positionOnly ? HashOfPosition(a) : HashFull(a);
        }

        public static long HashOfPosition(Snapshot.WINDOWPLACEMENT a)
        {
            int i = 0;
            long h = 3;
            h = h * primes[i++] + a.rcNormalPosition.Right;
            h = h * primes[i++] + a.rcNormalPosition.Top;
            h = h * primes[i++] + a.rcNormalPosition.Left;
            h = h * primes[i++] + a.rcNormalPosition.Bottom;
            return h;
        }

        public static long HashFull(Snapshot.WINDOWPLACEMENT a)
        {
            int i = 0;
            long h = 3;
            h = h * primes[i++] + a.rcNormalPosition.Right;
            h = h * primes[i++] + a.rcNormalPosition.Top;
            h = h * primes[i++] + a.rcNormalPosition.Left;
            h = h * primes[i++] + a.rcNormalPosition.Bottom;

            h = h * primes[i++] + a.ptMinPosition.X;
            h = h * primes[i++] + a.ptMinPosition.Y;
            h = h * primes[i++] + a.ptMaxPosition.X;
            h = h * primes[i++] + a.ptMaxPosition.Y;
            h = h * primes[i++] + a.flags;
            h = h * primes[i++] + a.showCmd;
            return h;
        }
    }
}
