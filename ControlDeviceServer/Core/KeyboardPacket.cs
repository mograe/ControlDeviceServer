using System;

namespace ControlDeviceServer.Core
{
    public sealed class KeyboardPacket
    {
        public const int Size = 8 + 256;

        public static void Write(byte[] buf, uint seq, uint ms, byte[] keys256)
        {
            if (buf == null) throw new ArgumentNullException(nameof(buf));
            if (keys256 == null) throw new ArgumentNullException(nameof(keys256));

            if (buf.Length < Size)
                throw new ArgumentException($"buf.Length < {Size}", nameof(buf));

            if (keys256.Length != 256)
                throw new ArgumentException("keys256 must be length 256", nameof(keys256));

            PutU32(buf, 0, seq);
            PutU32(buf, 4, ms);

            Buffer.BlockCopy(keys256, 0, buf, 8, 256);
        }

        static void PutU32(byte[] b, int o, uint v)
        {
            b[o] = (byte)v;
            b[o + 1] = (byte)(v >> 8);
            b[o + 2] = (byte)(v >> 16);
            b[o + 3] = (byte)(v >> 24);
        }
    }
}
