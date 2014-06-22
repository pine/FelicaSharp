using System;
using System.Runtime.InteropServices;

namespace FelicaSharp
{
    /// <summary>
    /// 動的リンクを実現するための P/Invoke 用のスタティッククラスです。
    /// </summary>
    internal static class DynamicLink
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "LoadLibraryW")]
        internal extern static IntPtr LoadLibrary(string fileName);

        [DllImport("kernel32.dll")]
        internal extern static void FreeLibrary(IntPtr handle);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, EntryPoint = "GetProcAddress")]
        internal extern static IntPtr GetProcAddress(
            IntPtr handle,
            string procName
            );
    }
}
