using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace ControlDeviceServer.Core
{
    public sealed class KeyboardPacket
    {
        public const int Size = 8 + 256;

        public static void Write(byte[] buf, uint seq, uint ms, byte[] keys256)
        {
            if (buf.Length < Size)
                throw new ArgumentException($"buf.Length < {Size}");
            if (keys256 == null || keys256.Length != Size)
                throw new ArgumentException("keys256 must be length 256");

          
        }
        
        static void PutU32(byte[] b, int o, uint v)
        {
            b[o] = (byte)v;
            b[o + 1] = (byte) (v >> 8);
            b[o + 2] = (byte) (v >> 16);
            b[o + 3] = (byte) (v >> 24);

        }
    }
}
