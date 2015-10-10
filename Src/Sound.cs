using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Nibbles.Bas
{
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
    public class Beeper : IDisposable
    {
        // Target frequency precision: less than 0.5% error
        //   37  Hz @ 44.1kHz sampling -> half-period of ~596 samples
        // 32.8 kHz @ 44.1kHz sampling -> half-period of ~0.67 samples

        // These probably should not be changed
        const int MinFrequency = 37;
        const int MaxFrequency = 32767;
        const int BitsPerSamplePerChannel = 16;
        const int NumberOfChannels = 1;
        const int SamplingRate = 44100;
        const int MaxAmplitude = (1 << (BitsPerSamplePerChannel - 1)) - 1;

        IDirectSound directSoundDevice = null;
        DSBUFFERDESC soundBufferDescription;

        const string DirectSoundModuleName = "dsound.dll";

        public Beeper()
        {
            // Silently fail DirectSound creation

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
            waveFormat.cbSize = (short) Marshal.SizeOf(typeof(WAVEFORMATEX));
            waveFormat.wFormatTag = WAVE_FORMAT_PCM;
            waveFormat.nChannels = NumberOfChannels;
            waveFormat.wBitsPerSample = BitsPerSamplePerChannel;
            waveFormat.nSamplesPerSec = SamplingRate;
            waveFormat.nBlockAlign = (short) (waveFormat.nChannels * waveFormat.wBitsPerSample / 8);
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

        ~Beeper()
        {
            // see comment in Dispose() for further info
            Dispose();
        }

        public void Dispose()
        {
            // This doesn't follow the Microsoft Dispose pattern because that one sucks (http://nitoprograms.blogspot.com/2009/08/how-to-implement-idisposable-and.html)
            // This also doesn't follow the Cleary's pattern because it's too much work.
            // If an IDisposable field were added to this class, this code would need further work because of that, but until then this is acceptable.
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

        public bool Beep(int freq, int duration)
        {
            if (directSoundDevice == null)
                return WinAPI.Beep(freq, duration);

            // Clamp the frequency to the acceptable range
            if (freq < MinFrequency)
                freq = MinFrequency;
            if (freq > MaxFrequency)
                freq = MaxFrequency;

            // 1/dwFreq = period of wave, in seconds
            // SAMPLING_RATE / dwFreq = samples per wave-period
            int period = SamplingRate * NumberOfChannels / freq;
            // The above line introduces roundoff error, which at higher
            // frequencies is significant (>30% at the 32kHz,
            // easily above 1% in general, not good). We will fix this below.

            // If frequency too high, make sure it's not just a constant DC level
            if (period < 1)
                period = 1;

            int bufferSamples = duration * SamplingRate / 1000;
            int bufferLength = bufferSamples * NumberOfChannels * BitsPerSamplePerChannel / 8;

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
                        // When we set the period above, we rounded down, so if
                        // we play the buffer as is, it will sound higher frequency
                        // than it ought to be.
                        // To compensate, we should play the buffer at a slower speed.
                        // The slowdown factor is precisely the rounded-down period
                        // divided by the true period:
                        //   period / [ SAMPLING_RATE * NUM_CHANNELS / dwFreq ]
                        // = dwFreq * period / (SAMPLING_RATE * NUM_CHANNELS)
                        //
                        // The adjusted frequency needs to be multiplied by this factor:
                        //   play_freq *= dwFreq * period / (SAMPLING_RATE * NUM_CHANNELS)

                        playFrequency = (int) ((long) playFrequency * ((long) freq * (long) period) / (long) (SamplingRate * NumberOfChannels));
                        if (soundBuffer.SetFrequency(playFrequency) >= 0)
                        {
                            //
                            // Write in the faded sinewave

                            IntPtr realBuffer1, realBuffer2;
                            int realBuffer1Length, realBuffer2Length;
                            if (soundBuffer.Lock(0, bufferLength, out realBuffer1, out realBuffer1Length, out realBuffer2, out realBuffer2Length, DSBLOCK_ENTIREBUFFER) >= 0)
                            {
                                bool unlocked = false;
                                int fadeInSamples = 18 * SamplingRate / 1000;
                                int fadeOutSamples = 47 * SamplingRate / 1000;
                                try
                                {
                                    int bufferIndex = 0;
                                    for (int sample = 0; sample < bufferSamples; sample++)
                                    {
                                        var amplitude =
                                            sample < fadeInSamples ? MaxAmplitude * sample / fadeInSamples :
                                            sample >= bufferSamples - fadeOutSamples ? MaxAmplitude * (bufferSamples - sample - 1) / fadeOutSamples :
                                            MaxAmplitude;
                                        for (int channel = 0; channel < NumberOfChannels; channel++)
                                            Marshal.WriteInt16(realBuffer1, bufferIndex++ * 2, (short) (amplitude * Math.Pow(Math.Sin(sample * 2 * Math.PI / period), 2.3)));
                                    }
                                }
                                finally
                                {
                                    unlocked = soundBuffer.Unlock(realBuffer1, realBuffer1Length, realBuffer2, realBuffer2Length) >= 0;
                                }

                                if (unlocked && soundBuffer.SetCurrentPosition(0) >= 0 && soundBuffer.Play(0, 0, 0) >= 0)
                                {
                                    played = true;
                                    int status;
                                    do
                                    {
                                        soundBuffer.GetStatus(out status);
                                        Thread.Sleep(1);
                                    }
                                    while ((status & DSBSTATUS_PLAYING) != 0);
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
        const int DSBSTATUS_PLAYING = 0x00000001;

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
}
