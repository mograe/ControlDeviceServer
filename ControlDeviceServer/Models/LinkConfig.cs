using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ControlDeviceServer.Models
{
    public sealed class LinkConfig
    {
        public IPAddress TargetIP;
        public int Port;
        public int SendHz;
        public bool LowLatencyTos;
        public bool LogGamepad;
        public double Deadzone;
        public bool InvertLeftY;
        public bool InvertRightY;
        public InputMode InputMode;
    }
}
