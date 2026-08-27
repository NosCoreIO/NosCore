//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System;
using System.Runtime.InteropServices;

namespace NosCore.Core
{
    public static class ConsoleTitle
    {
        public static void Set(string title)
        {
            if (OperatingSystem.IsWindows() && HasConsole)
            {
                Console.Title = title;
            }
        }

        public static void Append(string suffix)
        {
            if (OperatingSystem.IsWindows() && HasConsole)
            {
                Console.Title += suffix;
            }
        }

        private static bool HasConsole => !Console.IsOutputRedirected && GetConsoleWindow() != IntPtr.Zero;

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
    }
}
