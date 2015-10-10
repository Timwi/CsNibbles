using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Nibbles.Bas
{
    static class WinAPI
    {
        const int TMPF_TRUETYPE = 0x4;
        const int LF_FACESIZE = 32;
        const int STD_INPUT_HANDLE = -10;
        const int STD_OUTPUT_HANDLE = -11;
        const int STD_ERROR_HANDLE = -12;

        readonly static IntPtr InvalidHandleValue = new IntPtr(-1);

        [DllImport("Kernel32.dll", SetLastError = true)]
        extern static IntPtr GetStdHandle(int handle);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        extern static bool GetCurrentConsoleFontEx(IntPtr hConsoleOutput, bool bMaximumWindow, [In, Out] ref CONSOLE_FONT_INFOEX lpConsoleCurrentFont);

        [DllImport("kernel32.dll")]
        public extern static bool Beep(int freq, int dur);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct CONSOLE_FONT_INFOEX
        {
            public int cbSize;
            public int Index;
            public short Width;
            public short Height;
            public int Family;
            public int Weight;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = LF_FACESIZE)]
            public string FaceName;
        }

        public static IntPtr GetOutputHandle()
        {
            IntPtr handle = GetStdHandle(STD_OUTPUT_HANDLE);
            if (handle == InvalidHandleValue)
                throw new Win32Exception();
            return handle;
        }

        public static bool IsOutputConsoleFontTrueType()
        {
            CONSOLE_FONT_INFOEX cfi = new CONSOLE_FONT_INFOEX();
            cfi.cbSize = Marshal.SizeOf(typeof(CONSOLE_FONT_INFOEX));
            if (GetCurrentConsoleFontEx(GetOutputHandle(), false, ref cfi))
                return (cfi.Family & TMPF_TRUETYPE) == TMPF_TRUETYPE;
            return false;
        }


        public static bool Is64BitProcess
        {
            get { return IntPtr.Size == 8; }
        }

        public static bool Is64BitOperatingSystem
        {
            get
            {
                // Clearly if this is a 64-bit process we must be on a 64-bit OS.  
                if (Is64BitProcess)
                    return true;

                // Ok, so we are a 32-bit process, but is the OS 64-bit?  
                // If we are running under Wow64 than the OS is 64-bit.  
                bool isWow64;
                return ModuleContainsFunction("kernel32.dll", "IsWow64Process") && IsWow64Process(GetCurrentProcess(), out isWow64) && isWow64;
            }
        }

        public static bool ModuleContainsFunction(string moduleName, string methodName)
        {
            return ModuleContainsFunction(moduleName, methodName, false, false);
        }

        public static bool ModuleContainsFunction(string moduleName, string methodName, bool loadIfNecessary, bool freeIfNotFound)
        {
            bool weLoadedIt = false;
            IntPtr hModule = GetModuleHandle(moduleName);
            if (hModule == IntPtr.Zero && loadIfNecessary)
                weLoadedIt = (hModule = LoadLibrary(moduleName)) != IntPtr.Zero;
            if (hModule != IntPtr.Zero)
            {
                if (GetProcAddress(hModule, methodName) != IntPtr.Zero)
                    return true;
                if (weLoadedIt && freeIfNotFound)
                    FreeLibrary(hModule);
            }
            return false;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        extern static bool IsWow64Process(IntPtr hProcess, [MarshalAs(UnmanagedType.Bool)] out bool isWow64);

        [DllImport("kernel32.dll", SetLastError = true)]
        extern static IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        extern static IntPtr GetModuleHandle(string moduleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        extern static IntPtr LoadLibrary(string moduleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        extern static bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        extern static IntPtr GetProcAddress(IntPtr hModule, string methodName);
    }
}
