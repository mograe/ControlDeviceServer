using ControlDeviceServer.Models;
using ControlDeviceServer.Services;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ControlDeviceServer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly LogBuffer _log = new LogBuffer();
        UdpLinkService _link;

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();

            _log.LineAdded += line => Dispatcher.BeginInvoke(new Action(() =>
            {
                LogBox.AppendText(line);
                LogBox.ScrollToEnd();
            }));

            _log.Cleared += () => Dispatcher.BeginInvoke(new Action(() =>
            {
                LogBox.Clear();
            }));

            _link = new UdpLinkService(_log);
            _link.Telemetry += OnTelemetry;

            HzLabel.Text = ((int)HzSlider.Value).ToString();
            HzSlider.ValueChanged += (_, __) => HzLabel.Text = ((int)HzSlider.Value).ToString();

            DeadzoneLabel.Text = $"{DeadzoneSlider.Value:F2}";
            DeadzoneSlider.ValueChanged += (_, __) => DeadzoneLabel.Text = $"{DeadzoneSlider.Value:F2}";

            var uiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            //uiTimer.Tick += (_, __) => StatusText.Text = SentVal.Text;
            uiTimer.Start();

            _log.Info("Приложение запущено");
        }

        private void LoadSettings()
        {
            ConfigSerialized config = ConfigStore.LoadOrDefault();
            if (config != null)
            {
                IpBox.Text = config.TargetIP;
                PortBox.Text = config.TargetPort.ToString();
                HzSlider.Value = config.SendHz;
                LowLatencyTos.IsChecked = config.LowLatencyTos;
                SetMode(config.InputMode);
                DeadzoneSlider.Value = config.Deadzone;
                InvertLY.IsChecked = config.InvertLeftY;
                InvertRY.IsChecked = config.InvertRightY;
                LogState.IsChecked = config.LogState;
            }
        }

        private void SaveSettings()
        {
            var config = new ConfigSerialized();
            config.TargetIP = IpBox.Text;
            config.TargetPort = int.TryParse(PortBox.Text, out var port) ? port : 7777;
            config.SendHz = HzSlider.Value;
            config.LowLatencyTos = (bool)LowLatencyTos.IsChecked;
            config.InputMode = GetMode();
            config.Deadzone = DeadzoneSlider.Value;
            config.InvertLeftY = (bool)InvertLY.IsChecked;
            config.InvertRightY = (bool)InvertRY.IsChecked;
            config.LogState = (bool)LogState.IsChecked;
            ConfigStore.Save(config);
        }

        private void OnTelemetry(int hz, string padStatus)
        {
            Dispatcher.Invoke(() =>
            {
                FpsVal.Text = $"Текущая частота: {hz} Гц";
                PadHint.Text = padStatus;
            });
        }

        private void SetMode(InputMode inputMode)
        {
            switch (inputMode)
            {
                case InputMode.Gamepad:
                    ModeGamepad.IsChecked = true; 
                    break;
                case InputMode.Keyboard:
                    ModeKeyboard.IsChecked = true;
                    break;
                default:
                    ModeGamepad.IsChecked = true;
                    break;
            }
        }

        private InputMode GetMode()
        {
            if (ModeGamepad.IsChecked == true) return InputMode.Gamepad;
            if (ModeKeyboard.IsChecked == true) return InputMode.Keyboard;
            return InputMode.Gamepad;
        }

        void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            _log.Clear();
            Start();
        }


        private void Start()
        {
            IPAddress ip;
            int port;
            if (!IPAddress.TryParse(IpBox.Text.Trim(), out ip)) { MessageBox.Show("Некорректный IP"); return; }
            if (!int.TryParse(PortBox.Text, out port) || port < 1 || port > 65535) { MessageBox.Show("Некорректный порт"); return; }

            var cfg = new LinkConfig
            {
                TargetIP = ip,
                Port = port,
                SendHz = (int)HzSlider.Value,
                LowLatencyTos = LowLatencyTos.IsChecked == true,
                LogState = LogState.IsChecked == true,
                Deadzone = DeadzoneSlider.Value,
                InvertLeftY = InvertLY.IsChecked == true,
                InvertRightY = InvertRY.IsChecked == true,

                InputMode = GetMode()
            };

            _link.Start(cfg);
            StartBtn.IsEnabled = false;
            StopBtn.IsEnabled = true;
        }

        void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            _link.Stop();
            StartBtn.IsEnabled = true;
            StopBtn.IsEnabled = false;
            _log.Info("Остановлено");
        }

        protected override void OnClosed(EventArgs e)
        {
            SaveSettings();
            base.OnClosed(e);
            _link.Stop();
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (!StartBtn.IsEnabled)
            {
                Start();
            }
        }
    }
}
