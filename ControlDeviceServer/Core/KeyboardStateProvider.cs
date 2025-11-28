using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ControlDeviceServer.Core
{
    internal class KeyboardStateProvider
    {
        [DllImport("user32.dll")]
        private static extern int GetKeyboardState(byte[] lpKeyState);

        public static bool TryGetState(byte[] state256)
        {
            if (state256 == null || state256.Length != 256)
                throw new ArgumentException("state256 must be length 256");

            return GetKeyboardState(state256) != 0;
        }
    }
}
