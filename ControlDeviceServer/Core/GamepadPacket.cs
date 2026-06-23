using ControlDeviceServer.Models;
using System;
using System.Windows.Controls;



namespace ControlDeviceServer.Core
{
    public sealed class GamepadPacket
    {
        public const int Size = 22;
        public static void Write(byte[] buf, GamepadState gamepadState)
        {
            PutU32(buf, 0, gamepadState.seq);
            PutU32(buf, 4, gamepadState.ms);
            PutI16(buf, 8, gamepadState.lx); PutI16(buf, 10, gamepadState.ly);
            PutI16(buf, 12, gamepadState.rx); PutI16(buf, 14, gamepadState.ry);
            PutU16(buf, 16, gamepadState.lt); PutU16(buf, 18, gamepadState.rt);
            PutU16(buf, 20, gamepadState.buttons);
        }

        static void PutU16(byte[] b, int o, ushort v) { b[o] = (byte)v; b[o + 1] = (byte)(v >> 8); }
        static void PutI16(byte[] b, int o, short v) { b[o] = (byte)v; b[o + 1] = (byte)(v >> 8); }
        static void PutU32(byte[] b, int o, uint v) { b[o] = (byte)v; b[o + 1] = (byte)(v >> 8); b[o + 2] = (byte)(v >> 16); b[o + 3] = (byte)(v >> 24); }
    }
}
