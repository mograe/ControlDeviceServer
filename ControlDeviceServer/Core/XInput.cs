using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ControlDeviceServer.Core
{
    public static class XInput
    {
        public const int XINPUT_GAMEPAD_DPAD_UP = 0x0001;
        public const int XINPUT_GAMEPAD_DPAD_DOWN = 0x0002;
        public const int XINPUT_GAMEPAD_DPAD_LEFT = 0x0004;
        public const int XINPUT_GAMEPAD_DPAD_RIGHT = 0x0008;
        public const int XINPUT_GAMEPAD_START = 0x0010;
        public const int XINPUT_GAMEPAD_BACK = 0x0020;
        public const int XINPUT_GAMEPAD_LEFT_THUMB = 0x0040;
        public const int XINPUT_GAMEPAD_RIGHT_THUMB = 0x0080;
        public const int XINPUT_GAMEPAD_LEFT_SHOULDER = 0x0100;
        public const int XINPUT_GAMEPAD_RIGHT_SHOULDER = 0x0200;
        public const int XINPUT_GAMEPAD_A = 0x1000;
        public const int XINPUT_GAMEPAD_B = 0x2000;
        public const int XINPUT_GAMEPAD_X = 0x4000;
        public const int XINPUT_GAMEPAD_Y = 0x8000;

        [StructLayout(LayoutKind.Sequential)]
        public struct XINPUT_GAMEPAD
        {
            public ushort wButtons;
            public byte bLeftTrigger;
            public byte bRightTrigger;
            public short sThumbLX;
            public short sThumbLY;
            public short sThumbRX;
            public short sThumbRY;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct XINPUT_STATE
        {
            public uint dwPacketNumber;
            public XINPUT_GAMEPAD gamepad;
        }

        [DllImport("XInput1_4.dll", EntryPoint = "XInputGetState", CallingConvention = CallingConvention.StdCall)]
        static extern int XInputGetState_1_4(uint dwUserIndex, out XINPUT_STATE pState);

        [DllImport("XInput9_1_0.dll", EntryPoint = "XInputGetState", CallingConvention = CallingConvention.StdCall)]
        static extern int XInputGetState_9_1_0(uint dwUserIndex, out XINPUT_STATE pState);

        public static bool TryGetState(out XINPUT_STATE state)
        {
            try
            {
                if (XInputGetState_1_4(0, out state) == 0) return true;
            }
            catch { }
            try
            {
                if (XInputGetState_9_1_0(0, out state) == 0) return true;
            }
            catch { }
            state = default;
            return false;
        }

    }
}
