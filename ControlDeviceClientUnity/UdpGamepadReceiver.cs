using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Digitech.GamepadNetwork
{
    public class UdpGamepadReceiver : MonoBehaviour
    {
        [Header("Network")]
        [SerializeField] private int port = 29299;

        [Header("Metrics (read-only)")]
        [SerializeField] private uint lastSeq;
        [SerializeField] private int packetsPerSecond;
        [SerializeField] private int packetsTotal;

        private GamepadState Current;
        private GamepadState Previous;

        UdpClient _udp;
        CancellationTokenSource _cts;
        int _ppsCounter;
        float _ppsTimer;

        private void Start()
        {
            Application.runInBackground = true;
            Application.targetFrameRate = 120;
            StartReceiver();
        }

        private void Update()
        {
            _ppsTimer += Time.deltaTime;
            if (_ppsTimer > 1f)
            {
                packetsPerSecond = _ppsCounter;
                _ppsCounter = 0;
                _ppsTimer = 0f;
            }
        }

        private void OnDestroy() => StopReceiver();

        public void StopReceiver()
        {
            try { _cts?.Cancel(); } catch { }
            _cts = null;

            try { _udp?.Close(); } catch { }
            _udp?.Dispose();
            _udp = null;
        }

        private void StartReceiver()
        {
            StopReceiver();

            _cts = new CancellationTokenSource();
            _udp = new UdpClient(port) { Client = {ReceiveBufferSize = 64 * 1024} };
            _ = Task.Run(() => Loop(_cts.Token));
        }

        async Task Loop(CancellationToken ct)
        {
            IPEndPoint any = new IPEndPoint(IPAddress.Any, 0);
            while (!ct.IsCancellationRequested)
            {
                UdpReceiveResult r;
                try { r = await _udp.ReceiveAsync(); }
                catch { break; }

                if (r.Buffer == null) continue;

                GamepadPacket.Read(r.Buffer, out var state);
                lastSeq = state.seq;
                Interlocked.Increment(ref packetsTotal);
                Interlocked.Increment(ref _ppsCounter);

                Previous = Current;
                Current = state;
            }
        }

        public Vector2 LeftStick => Current.LeftStick;
        public Vector2 RightStick => Current.RightStick;
        public float LT => Current.LeftTrigger;
        public float RT => Current.RightTrigger;
        public bool Button(int bit) => Current.IsDown(bit);

        public bool ButtonDown(int bit)
        {
            bool now = (Current.buttons & (1 << bit)) != 0;
            bool prev = (Previous.buttons & (1 << bit)) != 0;
            return now && !prev;
        }

        public bool ButtonUp(int bit)
        {
            bool now = (Current.buttons & (1 << bit)) != 0;
            bool prev = (Previous.buttons & (1 << bit)) != 0;
            return !now && prev;
        }
    }

}
