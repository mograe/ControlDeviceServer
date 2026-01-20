using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlDeviceServer.Models
{
    public sealed class ConfigSerialized
    {
        public string TargetIP { get; set; } = "127.0.0.1";
        public int TargetPort { get; set; } = 7777;
        public double SendHz { get; set; } = 200;
        public bool LowLatencyTos { get; set; } = false;

        public InputMode InputMode { get; set; } = InputMode.Gamepad;

        public double Deadzone { get; set; } = 0.08;
        public bool InvertLeftY { get; set; } = false;
        public bool InvertRightY { get; set; } = false;

        public bool LogState {  get; set; } = false;
    }
}
