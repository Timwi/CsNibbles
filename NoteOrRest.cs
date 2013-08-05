using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
