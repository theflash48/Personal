using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using FlashTelemetry.Core;
using FlashTelemetry.Core.Radio;
using FlashTelemetry.Net;
using FlashTelemetry.Profiles;
using FlashTelemetry.Profiles.ETS2;
using FlashTelemetry.Protocol;

namespace FlashTelemetry
{
    public partial class MainWindow : Window
    {
        private const int CtrlPort = 5001;
        private const int DataPort = 5002;

        private const int DataHz = 120;

        private static readonly IPAddress Esp32FixedIp = IPAddress.Parse("192.168.0.33");

        private readonly ProfileManager _profiles = new();
        private readonly UdpControlServer _ctrl = new(CtrlPort);
        private readonly UdpDataSender _data = new();

        private readonly ProfileRuntime _runtime;

        private IPEndPoint? _lastEspEndpoint;
        private bool _started;

        private uint _seq;

        // RADIO runtime
        private readonly RadioEngine _radio;

        public MainWindow()
        {
            InitializeComponent();

            _runtime = new ProfileRuntime(
                profileManager: _profiles,
                log: AppendLog
            );

            // Asegura store único (el mismo que usa el UI partial)
            _radioStore ??= new RadioSettingsStore();
            _radio = new RadioEngine(_radioStore, () => _profiles.ActiveProfileId, AppendLog);

            TxtLastEsp.Text = "-";
            TxtActiveProfile.Text = $"{_profiles.ActiveProfileName} ({_profiles.ActiveProfileId})";

            // UI: lista de perfiles
            CmbProfiles.ItemsSource = ProfileCatalog.Profiles
                .Select(kv => new
                {
                    Id = kv.Key,
                    Label = kv.Value.Enabled ? $"{kv.Value.Name} ({kv.Key})" : $"{kv.Value.Name} ({kv.Key}) - Próximamente",
                })
                .ToList();

            CmbProfiles.DisplayMemberPath = "Label";
            CmbProfiles.SelectedValuePath = "Id";
            CmbProfiles.SelectedValue = _profiles.ActiveProfileId;

            // DATA sender -> ESP32 fijo
            _data.Configure(
                target: new IPEndPoint(Esp32FixedIp, DataPort),
                buildPayload: BuildDataPayload,
                log: AppendLog
            );

            // CTRL events
            _ctrl.Log += AppendLog;

            _ctrl.HelloReceived += async hello =>
            {
                _lastEspEndpoint = hello.Remote;

                Dispatcher.Invoke(() =>
                {
                    TxtLastEsp.Text = $"{hello.DeviceId} @ {hello.Remote.Address}";
                    AppendLog($"[CTRL] HELLO de {hello.DeviceId} sel={hello.SelectedProfileId} proto={hello.Proto}");
                });

                string ack = ControlProtocol.BuildHelloAck(_profiles.ActiveProfileId);
                await _ctrl.SendAsync(ack, hello.Remote);
            };

            _ctrl.SetProfileReceived += async msg =>
            {
                Dispatcher.Invoke(() =>
                {
                    AppendLog($"[CTRL] SET_PROFILE id={msg.ProfileId} desde {msg.Remote.Address}");
                });

                bool ok = _profiles.TrySetActive(msg.ProfileId);

                string reply = ControlProtocol.BuildProfileAck(_profiles.ActiveProfileId);
                await _ctrl.SendAsync(reply, msg.Remote);

                if (!ok)
                    Dispatcher.Invoke(() => AppendLog($"[CORE] Perfil {msg.ProfileId} no permitido (Próximamente o desconocido)."));
            };

            _ctrl.PingReceived += async ping =>
            {
                string pong = ControlProtocol.BuildPong(ping.Seq);
                await _ctrl.SendAsync(pong, ping.Remote);
            };

            // PTT toggle (IMPORTANTE: NO bloquear RxLoop y NO dejar que una excepción mate el listener)
            _ctrl.PttToggleReceived += pttMsg =>
            {
                if (!_started) return Task.CompletedTask;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _radio.ToggleAsync();
                    }
                    catch (Exception ex)
                    {
                        AppendLog($"[RADIO] ERROR en ToggleAsync: {ex.Message}");
                    }
                });

                return Task.CompletedTask;
            };

            // Cuando cambia el perfil activo, actualiza UI y runtime
            _profiles.ActiveProfileChanged += id =>
            {
                Dispatcher.Invoke(() =>
                {
                    TxtActiveProfile.Text = $"{_profiles.ActiveProfileName} ({_profiles.ActiveProfileId})";
                    AppendLog($"[CORE] Perfil activo cambiado a {_profiles.ActiveProfileName} ({id})");

                    // sincroniza combo
                    CmbProfiles.SelectedValue = id;

                    // refresca UI radio (checkbox/botón) por perfil activo
                    RefreshRadioUiFromSelectedProfile();
                });

                _runtime.SwitchTo(id, startIfRunning: _started);
            };
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (_started) return;

            _started = true;
            _seq = 0;

            _ctrl.Start();
            _runtime.StartActiveModule();

            _data.Start(hz: DataHz);

            AppendLog($"[CORE] START -> CTRL {CtrlPort} / DATA {DataPort} @ {DataHz}Hz -> {Esp32FixedIp}");
        }

        private async void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            if (!_started) return;

            _started = false;

            _data.Stop();
            _runtime.StopActiveModule();
            _ctrl.Stop();

            // Si estaba grabando, cierra
            await _radio.ForceCloseAsync();

            AppendLog("[CORE] STOP");
        }

        private void BtnApplyProfile_Click(object sender, RoutedEventArgs e)
        {
            if (CmbProfiles.SelectedValue is not int id) return;

            bool ok = _profiles.TrySetActive(id);
            TxtApplyResult.Text = ok ? "OK" : "No permitido";

            if (_lastEspEndpoint != null)
            {
                _ = _ctrl.SendAsync(ControlProtocol.BuildHelloAck(_profiles.ActiveProfileId), _lastEspEndpoint);
            }
        }

        private string BuildDataPayload()
        {
            if (_lastEspEndpoint == null) return string.Empty;

            _seq++;

            IProfileModule module = _runtime.ActiveModule;

            if (!module.TryBuildDataPayload(_seq, out string payload))
                return string.Empty;

            if (_seq % 400 == 0)
                Dispatcher.Invoke(() => AppendLog($"[DATA] TX seq={_seq} (p={module.ProfileId})"));

            return payload;
        }

        private void AppendLog(string line)
        {
            Dispatcher.Invoke(() =>
            {
                TxtLog.AppendText(line + Environment.NewLine);
                TxtLog.ScrollToEnd();
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            try { _radio.Dispose(); } catch { }
            base.OnClosed(e);
        }
    }

    internal sealed class ProfileRuntime
    {
        private readonly ProfileManager _profileManager;
        private readonly Action<string> _log;

        private readonly IProfileModule _ets2;
        private readonly IProfileModule _am2Soon;
        private readonly IProfileModule _f124;
        private readonly IProfileModule _f125Soon;

        public IProfileModule ActiveModule { get; private set; }

        public ProfileRuntime(ProfileManager profileManager, Action<string> log)
        {
            _profileManager = profileManager;
            _log = log;

            _ets2 = new FlashTelemetry.Profiles.ETS2.Ets2ProfileModule(log);
            _am2Soon = new ComingSoonProfileModule(ProfileCatalog.AM2, "Automobilista 2", log);
            _f124 = new FlashTelemetry.Profiles.F124.F124ProfileModule(listenPort: 20777, log: log);
            _f125Soon = new ComingSoonProfileModule(ProfileCatalog.F1_25, "F1 25", log);

            ActiveModule = Resolve(_profileManager.ActiveProfileId);
        }

        public void SwitchTo(int profileId, bool startIfRunning)
        {
            var next = Resolve(profileId);

            if (ReferenceEquals(next, ActiveModule))
                return;

            _log($"[RT] Switch {ActiveModule.Name} -> {next.Name}");

            ActiveModule.Stop();

            ActiveModule = next;

            if (startIfRunning)
                ActiveModule.Start();
        }

        public void StartActiveModule() => ActiveModule.Start();
        public void StopActiveModule() => ActiveModule.Stop();

        private IProfileModule Resolve(int id)
        {
            return id switch
            {
                ProfileCatalog.ETS2 => _ets2,
                ProfileCatalog.AM2 => _am2Soon,
                ProfileCatalog.F1_24 => _f124,
                ProfileCatalog.F1_25 => _f125Soon,
                _ => new ComingSoonProfileModule(id, $"Unknown({id})", _log),
            };
        }
    }
}
