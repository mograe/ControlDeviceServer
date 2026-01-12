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
        static extern short GetAsyncKeyState(int vKey);

        public static bool TryGetState(byte[] state256)
        {
            if (state256 == null || state256.Length != 256)
                throw new ArgumentException("state256 must be length 256");

            for (int vk = 0; vk < 256; vk++)
            {
                bool down = (GetAsyncKeyState(vk) & 0x8000) != 0;
                state256[vk] = down ? (byte)0x80 : (byte)0x00;
            }
            return true;
        }
    }
}
