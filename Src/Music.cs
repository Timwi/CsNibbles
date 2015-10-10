using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace Nibbles.Bas
{
    static class Music
    {
        private static object _lock = new object();
        private static int _tempo = 120;
        private static int _octave = 0;
        private static int _noteLength = 1;
        private static string[] _notes1 = new[] { "c", "c#", "d", "d#", "e", "f", "f#", "g", "g#", "a", "a#", "b" };
        private static string[] _notes2 = new[] { "c", "d-", "d", "e-", "e", "f", "g-", "g", "a-", "a", "b-", "b" };

        private static Beeper Beeper = new Beeper();

        public static void Play(string music, bool background)
        {
            var thread = new Thread(() =>
            {
                var input = music;
                Match m;
                var actions = new List<NoteOrRest>();
                while (input.Length > 0)
                {
                    if ((m = Regex.Match(input, @"^\s*o\s*(\d+)\s*", RegexOptions.Singleline | RegexOptions.IgnoreCase)).Success)
                    {
                        _octave = Convert.ToInt32(m.Groups[1].Value);
                        if (_octave < 0 || _octave > 6)
                            throw new ArgumentException("Octave must be from 0 to 6.");
                    }
                    else if ((m = Regex.Match(input, @"^\s*([<>])\s*", RegexOptions.Singleline | RegexOptions.IgnoreCase)).Success)
                    {
                        if (m.Groups[1].Value == ">")
                            _octave = Math.Min(6, _octave + 1);
                        else
                            _octave = Math.Max(0, _octave - 1);
                    }
                    else if ((m = Regex.Match(input, @"^\s*([abcdefg])\s*([#\+\-])?\s*(\.)?\s*", RegexOptions.Singleline | RegexOptions.IgnoreCase)).Success)
                    {
                        var noteStr = m.Groups[1].Value.ToLowerInvariant() + (m.Groups[2].Value == "+" ? "#" : m.Groups[2].Value);
                        var noteInOctave = Array.IndexOf(_notes1, noteStr);
                        if (noteInOctave == -1)
                            noteInOctave = Array.IndexOf(_notes2, noteStr);
                        if (noteInOctave == -1)
                            throw new ArgumentException("Unrecognised note: " + noteStr);
                        double times = m.Groups[3].Value == "." ? 1.5f : 1f;

                        // MusicTempo = number of quarters per minute
                        // ∴ 1/MusicTempo = number of minutes per quarter
                        // ∴ 60/MusicTempo = number of seconds per quarter
                        // ∴ 240/MusicTempo = number of seconds per whole note
                        actions.Add(new NoteOrRest((int) (440 * Math.Pow(2, _octave + (double) (noteInOctave + 3) / 12)), (int) (240000 * times / _tempo / _noteLength)));
                    }
                    else if ((m = Regex.Match(input, @"^\s*l\s*(\d+)\s*", RegexOptions.Singleline | RegexOptions.IgnoreCase)).Success)
                    {
                        var len = Convert.ToInt32(m.Groups[1].Value);
                        if (len < 1)
                            throw new ArgumentException(string.Format("L: Length cannot be zero or negative.", len));
                        _noteLength = len;
                    }
                    else if ((m = Regex.Match(input, @"^\s*p\s*(\d+)\s*", RegexOptions.Singleline | RegexOptions.IgnoreCase)).Success)
                    {
                        var len = Convert.ToInt32(m.Groups[1].Value);
                        if (len < 1)
                            throw new ArgumentException(string.Format("P: Length cannot be zero or negative.", len));
                        actions.Add(new NoteOrRest(null, (int) (240000 / _tempo / len)));
                    }
                    else if ((m = Regex.Match(input, @"^\s*t\s*(\d+)\s*", RegexOptions.Singleline | RegexOptions.IgnoreCase)).Success)
                    {
                        var tempo = Convert.ToInt32(m.Groups[1].Value);
                        if (tempo < 32 || tempo > 255)
                            throw new ArgumentException(string.Format("T: Unsupported tempo {0}. Tempo must be 32 to 255.", tempo));
                        _tempo = tempo;
                    }
                    else
                        throw new ArgumentException("Unrecognised command: " + input);
                    input = input.Substring(m.Length);
                }
                lock (_lock)
                {
                    foreach (var act in actions)
                        if (act.Frequency == null)
                            Thread.Sleep(act.DurationMs);
                        else
                            Beeper.Beep(act.Frequency.Value, act.DurationMs);
                }
            });
            thread.Start();
            if (!background)
                thread.Join();
        }

        public static void Dispose()
        {
            if (Beeper != null)
                Beeper.Dispose();
        }
    }
}
