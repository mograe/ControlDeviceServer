using UnityEngine;

namespace Digitech.GamepadNetwork
{
    public class GamepadPacket
    {
        public const int Size = 22;

        public static void Read(byte[] data, out GamepadState state)
        {
            state = default;

            state.seq = RU32(data, 0);
            state.ms = RU32(data, 4);
            state.lx = RI16(data, 8);
            state.ly = RI16(data, 10);
            state.rx = RI16(data, 12);
            state.ry = RI16(data, 14);
            state.lt = RU16(data, 16);
            state.rt = RU16(data, 18);
            state.buttons = RU16(data, 20);
        }

        static uint RU32(byte[] d, int o) => (uint)(d[o] | d[o+1]<<8 | d[o+2]<<16 | d[o+3]<<24);
        static ushort RU16(byte[] d, int o) => (ushort)(d[o] | d[o + 1] << 8);
        static short RI16(byte[] d, int o) => (short)(d[o] | d[o + 1] << 8);
    }
}