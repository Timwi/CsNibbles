
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Nibbles.Bas
{
    public static class SoundManager
    {
        public static Beeper CreateBeeper(CreateBeeperHint hint)
        {
            // No sound desired, return silent beeper
            if (hint == CreateBeeperHint.NoSound)
                return new SilentBeeper();

            // 64-bit Windows can't use speaker Beep, only option is DirectSound, whether it works or not
            if (WinAPI.Is64BitOperatingSystem)
                return new DirectSoundBeeper();

            // If we want DirectSound, return it if it will work
            if (hint == CreateBeeperHint.DirectSound)
            {
                DirectSoundBeeper beeper = new DirectSoundBeeper();
                if (beeper.CanPlay)
                    return beeper;

                // Dispose is unnecessary because if it !CanPlay, it has no external resources
                //  but it's here for show.
                beeper.Dispose();
            }

            // The only option left is the speaker
            return new SpeakerBeeper();
        }
    }

    public enum CreateBeeperHint
    {
        NoSound,
        DirectSound,
        SpeakerSound,
    }

    public abstract class Beeper : IDisposable
    {
        ~Beeper()
        {
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Plays a beep sound.
        /// </summary>
        /// <param name="frequency">Frequency of the beep in Hz.</param>
        /// <param name="duration">Duration of the beep in Ms.</param>
        /// <returns>True if a sound was played.</returns>
        public abstract bool Beep(int frequency, int duration);
    }

    public class SilentBeeper : Beeper
    {
        public override bool Beep(int frequency, int duration)
        {
            return false;
        }
    }

    public class SpeakerBeeper : Beeper
    {
        public override bool Beep(int frequency, int duration)
        {
            return WinAPI.Beep(frequency, duration);
        }
    }


    /*
     * Translated to C# October, 2010
     * Tergiver (tergiver@msn.com)
     * 
     * Original source code from:
     * DirectSound Beep Implementation (Victor Dyndns, aka CyLith)
     * http://www.codeproject.com/KB/winsdk/DirectSound_beep.aspx
     * Victor Dyndns
     * http://www.stanford.edu/~vkl/
     * 
     * Most of the comments are from the original.
     * 
     * It uses the original IDirectSound interface from DirectX 3 (1996).
     * It will compile to, and run on, both x86 and x64 so you can target AnyCPU.
     * 
     */

    public class DirectSoundBeeper : Beeper
    {
        // Target frequency precision: less than 0.5% error
        //   37  Hz @ 44.1kHz sampling -> half-period of ~596 samples
        // 32.8 kHz @ 44.1kHz sampling -> half-period of ~0.67 samples

        // These probably should not be changed
        const int MinFrequency = 37;
        const int MaxFrequency = 32767;
        const int BitsPerSample = 16;
        const int NumberOfChannels = 1;
        const int SamplingRate = 44100;
        const int WaveScale = (1 << (BitsPerSample - 1)) - 1;

        IDirectSound directSoundDevice = null;
        DSBUFFERDESC soundBufferDescription;

        const string DirectSoundModuleName = "dsound.dll";

        public DirectSoundBeeper()
        {
            // Silently fail DirectSound creation
            //  CanPlay can be used to check if this will play sounds or not

            if (!WinAPI.ModuleContainsFunction(DirectSoundModuleName, "DirectSoundCreate", true, true))
                return;

            if (DirectSoundCreate(IntPtr.Zero, out directSoundDevice, IntPtr.Zero) < 0)
                return;

            if (directSoundDevice.SetCooperativeLevel(GetDesktopWindow(), DSSCL_NORMAL) < 0)
            {
                Marshal.ReleaseComObject(directSoundDevice);
                directSoundDevice = null;
                return;
            }

            WAVEFORMATEX waveFormat;
            waveFormat.cbSize = (short)Marshal.SizeOf(typeof(WAVEFORMATEX));
            waveFormat.wFormatTag = WAVE_FORMAT_PCM;
            waveFormat.nChannels = NumberOfChannels;
            waveFormat.wBitsPerSample = BitsPerSample;
            waveFormat.nSamplesPerSec = SamplingRate;
            waveFormat.nBlockAlign = (short)(waveFormat.nChannels * waveFormat.wBitsPerSample / 8);
            waveFormat.nAvgBytesPerSec = waveFormat.nSamplesPerSec * waveFormat.nBlockAlign;

            // The wave format never changes so we marshal it to unmanaged memory and reuse it in calls
            //  to CreateSoundBuffer.
            IntPtr unmanagedWaveFormat = Marshal.AllocHGlobal(waveFormat.cbSize);
            Marshal.StructureToPtr(waveFormat, unmanagedWaveFormat, false);

            // These fields never change
            soundBufferDescription.dwSize = Marshal.SizeOf(typeof(DSBUFFERDESC));
            soundBufferDescription.dwReserved = 0;
            soundBufferDescription.dwFlags = DSBCAPS_CTRLPOSITIONNOTIFY | DSBCAPS_CTRLFREQUENCY | DSBCAPS_GLOBALFOCUS;
            soundBufferDescription.lpwfxFormat = unmanagedWaveFormat;
        }

        public bool CanPlay
        {
            get { return directSoundDevice != null; }
        }

        protected override void Dispose(bool disposing)
        {
            if (soundBufferDescription.lpwfxFormat != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(soundBufferDescription.lpwfxFormat);
                soundBufferDescription.lpwfxFormat = IntPtr.Zero;
            }
            if (directSoundDevice != null)
            {
                Marshal.ReleaseComObject(directSoundDevice);
                directSoundDevice = null;
            }
        }

        public override bool Beep(int freq, int duration)
        {
            if (directSoundDevice == null)
                return false;

            // Clamp the frequency to the acceptable range
            if (freq < MinFrequency)
                freq = MinFrequency;
            if (freq > MaxFrequency)
                freq = MaxFrequency;

            // 1/dwFreq = period of wave, in seconds
            // SAMPLING_RATE / dwFreq = samples per wave-period
            int halfPeriod = SamplingRate * NumberOfChannels / (2 * freq);
            // The above line introduces roundoff error, which at higher
            // frequencies is significant (>30% at the 32kHz,
            // easily above 1% in general, not good). We will fix this below.

            // If frequency too high, make sure it's not just a constant DC level
            if (halfPeriod < 1)
                halfPeriod = 1;

            int bufferLength = 2 * halfPeriod * BitsPerSample / 8;

            bool played = false;

            soundBufferDescription.dwBufferBytes = bufferLength;
            IDirectSoundBuffer soundBuffer;
            if (directSoundDevice.CreateSoundBuffer(ref soundBufferDescription, out soundBuffer, IntPtr.Zero) >= 0)
            {
                try
                {
                    // Frequency adjustment to correct for the roundoff error above.
                    int playFrequency;
                    if (soundBuffer.GetFrequency(out playFrequency) >= 0)
                    {
                        // When we set the half_period above, we rounded down, so if
                        // we play the buffer as is, it will sound higher frequency
                        // than it ought to be.
                        // To compensate, we should play the buffer at a slower speed.
                        // The slowdown factor is precisely the rounded-down period
                        // divided by the true period:
                        //   half_period / [ SAMPLING_RATE * NUM_CHANNELS / (2*dwFreq) ]
                        // = 2*dwFreq*half_period / (SAMPLING_RATE * NUM_CHANNELS)
                        //
                        // The adjusted frequency needs to be multiplied by this factor:
                        //   play_freq *= 2*dwFreq*half_period / (SAMPLING_RATE * NUM_CHANNELS)
                        // To do this computation (in a way that works on 32-bit machines),
                        // we cannot multiply the numerator directly, since that may
                        // cause rounding problems (44100 * 2*44100*1 ~ 3.9 billion which
                        // is uncomfortable close to the upper limit of 4.3 billion).
                        // Therefore, we use MulDiv to safely (and efficiently) avoid any
                        // problems.

                        // [Tergiver] I simply cast to 64-bit long which works the same as MulDiv

                        playFrequency = (int)((long)playFrequency * (long)(2 * freq * halfPeriod) / (long)(SamplingRate * NumberOfChannels));
                        if (soundBuffer.SetFrequency(playFrequency) >= 0)
                        {
                            //
                            // Write in the square wave

                            IntPtr realBuffer1, realBuffer2;
                            int realBuffer1Length, realBuffer2Length;
                            if (soundBuffer.Lock(0, bufferLength, out realBuffer1, out realBuffer1Length, out realBuffer2, out realBuffer2Length, DSBLOCK_ENTIREBUFFER) >= 0)
                            {
                                bool unlocked = false;
                                try
                                {
                                    int bufferIndex = 0;
                                    for (int n = 0; n < halfPeriod * NumberOfChannels; n++)
                                        Marshal.WriteInt16(realBuffer1, bufferIndex++ * 2, -WaveScale);
                                    for (int n = 0; n < halfPeriod * NumberOfChannels; n++)
                                        Marshal.WriteInt16(realBuffer1, bufferIndex++ * 2, +WaveScale);
                                }
                                finally
                                {
                                    unlocked = soundBuffer.Unlock(realBuffer1, realBuffer1Length, realBuffer2, realBuffer2Length) >= 0;
                                }

                                if (unlocked && soundBuffer.SetCurrentPosition(0) >= 0 && soundBuffer.Play(0, 0, DSBPLAY_LOOPING) >= 0)
                                {
                                    played = true;
                                    Thread.Sleep(duration);
                                    soundBuffer.Stop();
                                }
                            }
                        }
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(soundBuffer);
                }
            }

            return played;
        }

        #region DirectX & Win32 API

        [DllImport("User32.dll")]
        extern static IntPtr GetDesktopWindow();

        /*
         * The following DirectSound wrappers are incomplete.
         * They are only sufficiently correct as was needed to implement the Beeper.
        */

        const int WAVE_FORMAT_PCM = 1;

        const int DSSCL_NORMAL = 0x00000001;

        const int DSBCAPS_CTRLFREQUENCY = 0x00000020;
        const int DSBCAPS_CTRLPOSITIONNOTIFY = 0x00000100;
        const int DSBCAPS_GLOBALFOCUS = 0x00008000;

        const int DSBPLAY_LOOPING = 0x00000001;

        const int DSBLOCK_ENTIREBUFFER = 0x00000002;

        [DllImport(DirectSoundModuleName)]
        extern static int DirectSoundCreate(IntPtr pcGuidDevice, out IDirectSound ppDS, IntPtr pUnkOuter);

        struct WAVEFORMATEX
        {
            public short wFormatTag;        /* format type */
            public short nChannels;         /* number of channels (i.e. mono, stereo...) */
            public int nSamplesPerSec;    /* sample rate */
            public int nAvgBytesPerSec;   /* for buffer estimation */
            public short nBlockAlign;       /* block size of data */
            public short wBitsPerSample;    /* Number of bits per sample of mono data */
            public short cbSize;            /* The count in bytes of the size of*/
        }

        struct DSBUFFERDESC
        {
            public int dwSize;
            public int dwFlags;
            public int dwBufferBytes;
            public int dwReserved;
            // ugly use of IntPtr here, but to keep from using 'unsafe' code we will allocate unmanaged memory
            //  and marshal a copy of the WAVEFORMATEX to that memory, pointing this at that copy.
            public IntPtr lpwfxFormat;
        }

        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("279AFA85-4981-11CE-A521-0020AF0BE560")]
        interface IDirectSoundBuffer
        {
            int GetCaps(IntPtr pDSBufferCaps);
            int GetCurrentPosition(out int pdwCurrentPlayCursor, out int pdwCurrentWriteCursor);
            int GetFormat(out WAVEFORMATEX pwfxFormat, int dwSizeAllocated, out int pdwSizeWritten);
            int GetVolume(out int plVolume);
            int GetPan(out int plPan);
            int GetFrequency(out int pdwFrequency);
            int GetStatus(out int pdwStatus);
            int Initialize(IDirectSound pDirectSound, [In] ref DSBUFFERDESC pcDSBufferDesc);
            int Lock(int dwOffset, int dwBytes, out IntPtr ppvAudioPtr1, out int pdwAudioBytes1, out IntPtr ppvAudioPtr2, out int pdwAudioBytes2, int dwFlags);
            int Play(int dwReserved1, int dwPriority, int dwFlags);
            int SetCurrentPosition(int dwNewPosition);
            int SetFormat([In] ref WAVEFORMATEX pcfxFormat);
            int SetVolume(int lVolume);
            int SetPan(int lPan);
            int SetFrequency(int dwFrequency);
            int Stop();
            int Unlock(IntPtr pvAudioPtr1, int dwAudioBytes1, IntPtr pvAudioPtr2, int dwAudioBytes2);
            int Restore();
        }

        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("279AFA83-4981-11CE-A521-0020AF0BE560")]
        interface IDirectSound
        {
            int CreateSoundBuffer([In] ref DSBUFFERDESC pcDSBufferDesc, out IDirectSoundBuffer ppDSBuffer, IntPtr pUnkOuter);
            int GetCaps(IntPtr pDSCaps);
            int DuplicateSoundBuffer(IDirectSoundBuffer pDSBufferOriginal, out IDirectSoundBuffer ppDSBufferDuplicate);
            int SetCooperativeLevel(IntPtr hwnd, int dwLevel);
            int Compact();
            int GetSpeakerConfig(out int pdwSpeakerConfig);
            int SetSpeakerConfig(int dwSpeakerConfig);
            int Initialize([In] ref Guid pcGuidDevice);
        }

        #endregion
    }

    public static class Wow
    {
    }
}
