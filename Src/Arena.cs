using System;

namespace Nibbles.Bas
{
    /// <summary>
    ///     This type is used to represent the playing screen in memory. It is used to simulate graphics in text mode. Instead
    ///     of the normal 80x25 text graphics using "█", we will use "▄" and "▀" to mimic an 80x50 pixel screen.</summary>
    struct Arena
    {
        /// <summary>Maps the 80×50 point into the real 80×25.</summary>
        public int RealRow;

        /// <summary>Stores the current color of the point.</summary>
        public ConsoleColor Color;

        /// <summary>Each char has 2 points in it.  Sister is -1 if sister point is above, +1 if below</summary>
        public int Sister;
    }
}
