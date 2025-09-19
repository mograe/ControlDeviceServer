using UnityEngine;

namespace Digitech.GamepadNetwork
{
    public struct GamepadState
    {
        public uint seq;
        public uint ms;
        public short lx, ly, rx, ry;
        public ushort lt, rt, buttons;

        public Vector2 LeftStick => new Vector2(lx / 32767f, ly / 32767f);
        public Vector2 RightStick => new Vector2(rx / 32767f, ry / 32767f);
        public float LeftTrigger => lt / 1023f;
        public float RightTrigger => rt / 1023f;

        public bool IsDown(int bit) => (buttons & (1 << bit)) != 0;

        public override string ToString() =>
            $"seq={seq} ms={ms} L=({lx},{ly}) R=({rx},{ry}) LT={lt} RT={rt} BTN=0x{buttons:X4}";
    }

}
