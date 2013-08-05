﻿/*
 * 
 *      The authors waive all rights to this source file.
 *      It is public domain where permitted by law.
 * 
 */

namespace Nibbles
{
    struct NoteOrRest
    {
        public int? Frequency { get; private set; }
        public int DurationMs { get; private set; }

        public NoteOrRest(int? freq, int ms)
            : this()
        {
            Frequency = freq;
            DurationMs = ms;
        }
    }
}
