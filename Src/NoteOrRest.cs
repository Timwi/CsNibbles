namespace Nibbles.Bas
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
