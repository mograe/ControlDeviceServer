using ControlDeviceServer.Core;
using ControlDeviceServer.Models;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ControlDeviceServer.Services
{
    public sealed class UdpLinkService
    {
        readonly LogBuffer _log;
        UdpClient _udp;
        CancellationTokenSource _cts;
        IPEndPoint _target;
        LinkConfig _cfg;
        readonly Stopwatch _sw = Stopwatch.StartNew();

        uint _seq;
        int _sentThisSecond;
        DateTime _secTick = DateTime.UtcNow;

        bool _lastPadPresent;

        public event Action<int, string> Telemetry;

        public UdpLinkService(LogBuffer log) => _log = log;

        public void Start(LinkConfig cfg)
        {
            Stop();

            _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
            _udp = new UdpClient();

            if (_cfg.LowLatencyTos)
            {
                try
                {
                    // DSCP AF11 (примерно low-latency), как у тебя было 0x28
                    _udp.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.TypeOfService, 0x28);
                }
                catch { /* не критично */ }
            }

            _target = new IPEndPoint(cfg.TargetIP, cfg.Port);
            _cts = new CancellationTokenSource();

            _seq = 0;
            _sentThisSecond = 0;
            _secTick = DateTime.UtcNow;
            _lastPadPresent = false;

            _log.Info($"Старт → {_target.Address}:{_target.Port}, {_cfg.SendHz} Гц, TOS={(_cfg.LowLatencyTos ? "low-latency" : "default")}");

            var ct = _cts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    switch (_cfg.InputMode)
                    {
                        case InputMode.Gamepad:
                            await SendGamepadLoop(ct);
                            break;

                        case InputMode.Keyboard:
                            await SendKeyboardLoop(ct);
                            break;

                        default:
                            _log.Warn($"Неизвестный InputMode={_cfg.InputMode}. Ничего не запущено.");
                            break;
                    }
                }
                catch (OperationCanceledException) { /* норм */ }
                catch (Exception ex)
                {
                    _log.Error("Фоновая задача отправки упала: " + ex);
                }
            }, ct);
        }

        public void Stop()
        {
            if (_cts != null)
            {
                try { _cts.Cancel(); } catch { }
                _cts.Dispose();
                _cts = null;
            }

            if (_udp != null)
            {
                try { _udp.Close(); } catch { }
                _udp.Dispose();
                _udp = null;
            }
        }

        async Task SendGamepadLoop(CancellationToken ct)
        {
            int intervalMs = ClampInt((int)Math.Round(1000.0 / Math.Max(1, _cfg.SendHz)), 2, 1000);
            var buf = new byte[GamepadPacket.Size];

            _log.Info($"Цикл отправки (геймпад): интервал {intervalMs} мс");

            while (!ct.IsCancellationRequested)
            {
                XInput.XINPUT_STATE state;
                bool padPresent = XInput.TryGetState(out state);

                if (padPresent != _lastPadPresent)
                {
                    _lastPadPresent = padPresent;
                    _log.Info(padPresent ? "Геймпад найден (XInput)" : "Геймпад не найден — ожидание…");
                }

                if (!padPresent)
                {
                    RateOncePerSecond(msg: null);
                    await Task.Delay(100, ct);
                    continue;
                }

                GamepadState gamepadState;
                gamepadState.seq = _seq++;
                gamepadState.ms = (uint)_sw.ElapsedMilliseconds;

                gamepadState.lx = (short)FilterAxis(state.gamepad.sThumbLX, false, _cfg.Deadzone);
                gamepadState.ly = (short)FilterAxis(state.gamepad.sThumbLY, _cfg.InvertLeftY, _cfg.Deadzone);
                gamepadState.rx = (short)FilterAxis(state.gamepad.sThumbRX, false, _cfg.Deadzone);
                gamepadState.ry = (short)FilterAxis(state.gamepad.sThumbRY, _cfg.InvertRightY, _cfg.Deadzone);

                gamepadState.lt = (ushort)ClampInt(state.gamepad.bLeftTrigger * 4, 0, 1023);
                gamepadState.rt = (ushort)ClampInt(state.gamepad.bRightTrigger * 4, 0, 1023);
                gamepadState.buttons = MapButtons(state.gamepad.wButtons);

                if (_cfg.LogGamepad)
                    _log.Info(gamepadState.ToString());

                GamepadPacket.Write(buf, gamepadState);

                try
                {
                    await _udp.SendAsync(buf, buf.Length, _target);
                }
                catch (SocketException e)
                {
                    _log.Error("Ошибка отправки UDP: " + e.Message);
                }
                catch (ObjectDisposedException)
                {
                    break; // Stop() закрыл сокет
                }

                _sentThisSecond++;
                RateOncePerSecond(null);

                await Task.Delay(intervalMs, ct);
            }

            _log.Info("Цикл отправки (геймпад) завершён");
        }

    async Task SendKeyboardLoop(CancellationToken ct)
        {
            int intervalMs = ClampInt((int)Math.Round(1000.0 / Math.Max(1, _cfg.SendHz)), 2, 1000);
            var buf = new byte[KeyboardPacket.Size];
            var keys = new byte[256];
            var prev = new byte[256];
            bool hasPrev = false;

            _log.Info($"Цикл отправки (клавиатура): интервал {intervalMs} мс");

            while (!ct.IsCancellationRequested)
            {
                if (!KeyboardStateProvider.TryGetState(keys))
                {
                    RateOncePerSecondKeyboard("Не удалось получить состояние клавиатуры");
                    await Task.Delay(100, ct);
                    continue;
                }

                // --- ЛОГ ТОЛЬКО ИЗМЕНЕНИЙ ---
                if (hasPrev)
                {
                    for (int vk = 0; vk < 256; vk++)
                    {
                        bool wasDown = (prev[vk] & 0x80) != 0;
                        bool isDown = (keys[vk] & 0x80) != 0;

                        if (wasDown != isDown)
                        {
                            string name = ((Keys)vk).ToString();
                            _log.Info(isDown ? $"KEY DOWN: {vk} {name}" : $"KEY UP: {vk} {name}");
                        }
                    }
                }
                Buffer.BlockCopy(keys, 0, prev, 0, 256);
                hasPrev = true;
                // ---------------------------

                uint seq = _seq++;
                uint ms = (uint)_sw.ElapsedMilliseconds;

                KeyboardPacket.Write(buf, seq, ms, keys);

                try { await _udp?.SendAsync(buf, buf.Length, _target); }
                catch (ObjectDisposedException) { break; }
                catch (SocketException ex) { _log.Warn("Ошибка отправки UDP: " + ex.Message); }

                _sentThisSecond++;
                RateOncePerSecondKeyboard(null);

                await Task.Delay(intervalMs, ct);
            }
        }

        void RateOncePerSecondKeyboard(string msg)
        {
            var now = DateTime.UtcNow;
            if ((now - _secTick).TotalSeconds >= 1)
            {
                Telemetry?.Invoke(_sentThisSecond, "клавиатура");
                _sentThisSecond = 0;
                _secTick = now;
                if (!string.IsNullOrEmpty(msg)) _log.Info(msg);
            }
        }

        void RateOncePerSecond(string msg)
        {
            var now = DateTime.UtcNow;
            if ((now - _secTick).TotalSeconds >= 1)
            {
                Telemetry?.Invoke(_sentThisSecond, _lastPadPresent ? "найден" : "нет");
                _sentThisSecond = 0;
                _secTick = now;
                if (!string.IsNullOrEmpty(msg)) _log.Info(msg);
            }
        }

        static int FilterAxis(int v, bool invert, double deadzone)
        {
            if (invert) v = -v;
            int abs = v < 0 ? -v : v;
            int threshold = (int)(deadzone * 32767);
            if (abs < threshold) return 0;
            return ClampInt(v, -32768, 32767);
        }

        static int ClampInt(int v, int min, int max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        static ushort MapButtons(int flags)
        {
            ushort m = 0;
            Action<int, int> B = (flag, bit) => { if ((flags & flag) != 0) m |= (ushort)(1 << bit); };

            B(XInput.XINPUT_GAMEPAD_A, 0);
            B(XInput.XINPUT_GAMEPAD_B, 1);
            B(XInput.XINPUT_GAMEPAD_X, 2);
            B(XInput.XINPUT_GAMEPAD_Y, 3);
            B(XInput.XINPUT_GAMEPAD_LEFT_SHOULDER, 4);
            B(XInput.XINPUT_GAMEPAD_RIGHT_SHOULDER, 5);
            B(XInput.XINPUT_GAMEPAD_BACK, 6);
            B(XInput.XINPUT_GAMEPAD_START, 7);
            B(XInput.XINPUT_GAMEPAD_LEFT_THUMB, 8);
            B(XInput.XINPUT_GAMEPAD_RIGHT_THUMB, 9);
            B(XInput.XINPUT_GAMEPAD_DPAD_UP, 10);
            B(XInput.XINPUT_GAMEPAD_DPAD_DOWN, 11);
            B(XInput.XINPUT_GAMEPAD_DPAD_LEFT, 12);
            B(XInput.XINPUT_GAMEPAD_DPAD_RIGHT, 13);

            return m;
        }
    }
}
