using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlDeviceServer.Models
{
    public struct GamepadState
    {
        public uint seq;
        public uint ms;
        public short lx;
        public short ly;
        public short rx;
        public short ry;
        public ushort lt;
        public ushort rt;
        public ushort buttons;

        public override string ToString()
        {
            float Clamp(float x, float lo, float hi) { if (x < lo) return lo; if (x > hi) return hi; return x; }

            float nlx = Clamp(lx / 32767f, -1f, 1f);
            float nly = Clamp(ly / 32767f, -1f, 1f);
            float nrx = Clamp(rx / 32767f, -1f, 1f);
            float nry = Clamp(ry / 32767f, -1f, 1f);
            float nlt = Clamp(lt / 1023f, 0f, 1f);
            float nrt = Clamp(rt / 1023f, 0f, 1f);

            return string.Format(
                "seq={0} ms={1} L=({2},{3})[{4:F2},{5:F2}] R=({6},{7})[{8:F2},{9:F2}] LT={10}({11:F2}) RT={12}({13:F2}) BTN=0x{14:X4}",
                seq, ms,
                lx, ly, nlx, nly,
                rx, ry, nrx, nry,
                lt, nlt,
                rt, nrt,
                buttons
            );
        }
    }
}
