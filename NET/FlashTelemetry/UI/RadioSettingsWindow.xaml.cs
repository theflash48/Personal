using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using FlashTelemetry.Core.Radio;

namespace FlashTelemetry.UI
{
    public partial class RadioSettingsWindow : Window
    {
        private readonly int _profileId;
        private readonly RadioSettingsStore _store;
        private readonly AudioDeviceService _devices;
        private readonly TtsService _tts;
        private readonly AudioPlayer _player;

        private RadioSettings _settings;
        private ObservableCollection<RadioAlias> _aliases = new();

        public RadioSettingsWindow(int profileId, RadioSettingsStore store, AudioDeviceService devices, TtsService tts, AudioPlayer player)
        {
            InitializeComponent();

            _profileId = profileId;
            _store = store;
            _devices = devices;
            _tts = tts;
            _player = player;

            _settings = _store.GetForProfile(_profileId);

            Loaded += (_, __) => InitUi();

            BtnTestVoice.Click += async (_, __) => await TestVoiceAsync();

            // Tests con retardo para poder cambiar foco si quieres observarlo en otra app
            BtnTestMute.Click += async (_, __) => await TestHotkeyDelayedAsync(TxtMuteHotkey.Text, BtnTestMute);
            BtnTestDeafen.Click += async (_, __) => await TestHotkeyDelayedAsync(TxtDeafenHotkey.Text, BtnTestDeafen);

            BtnPickOpenWav.Click += (_, __) => PickWav(TxtOpenWav);
            BtnPickCloseWav.Click += (_, __) => PickWav(TxtCloseWav);
            BtnTestOpenWav.Click += async (_, __) => await TestWavAsync(TxtOpenWav.Text);
            BtnTestCloseWav.Click += async (_, __) => await TestWavAsync(TxtCloseWav.Text);

            BtnPickRecordingsFolder.Click += (_, __) => PickFolder(TxtRecordingsFolder);

            BtnPickWhisperExe.Click += (_, __) => PickExe(TxtWhisperExe);
            BtnPickWhisperModel.Click += (_, __) => PickBin(TxtWhisperModel);

            BtnAddAlias.Click += (_, __) => _aliases.Add(new RadioAlias());
            BtnRemoveAlias.Click += (_, __) =>
            {
                if (GridAliases.SelectedItem is RadioAlias a) _aliases.Remove(a);
            };

            BtnCancel.Click += (_, __) => { DialogResult = false; Close(); };
            BtnSave.Click += (_, __) => { SaveAndClose(); };
        }

        private void InitUi()
        {
            var outs = _devices.GetOutputDevices();
            var ins = _devices.GetInputDevices();

            CmbOutput.ItemsSource = outs;
            CmbInput.ItemsSource = ins;

            var outId = _devices.ResolveOutputIdOrDefault(_settings.OutputDeviceId);
            var inId = _devices.ResolveInputIdOrDefault(_settings.InputDeviceId);

            CmbOutput.SelectedItem = outs.FirstOrDefault(d => d.Id == outId) ?? outs.FirstOrDefault();
            CmbInput.SelectedItem = ins.FirstOrDefault(d => d.Id == inId) ?? ins.FirstOrDefault();

            SldTtsVol.Value = Clamp01(_settings.TtsVolume);
            SldBeepVol.Value = Clamp01(_settings.BeepVolume);

            CmbDiscordAction.ItemsSource = new[]
            {
                DiscordAction.None,
                DiscordAction.MuteToggle,
                DiscordAction.DeafenToggle
            };
            CmbDiscordAction.SelectedItem = _settings.DiscordAction;

            TxtMuteHotkey.Text = _settings.DiscordMuteHotkey;
            TxtDeafenHotkey.Text = _settings.DiscordDeafenHotkey;

            TxtOpenWav.Text = _settings.RadioOpenWavPath;
            TxtCloseWav.Text = _settings.RadioCloseWavPath;

            TxtRecordingsFolder.Text = _settings.RecordingsFolderPath;

            ChkSttEnabled.IsChecked = _settings.SttEnabled;
            TxtWhisperExe.Text = _settings.WhisperExePath;
            TxtWhisperModel.Text = _settings.WhisperModelPath;
            TxtWhisperLang.Text = string.IsNullOrWhiteSpace(_settings.WhisperLanguage) ? "es" : _settings.WhisperLanguage;
            TxtWhisperArgs.Text = _settings.WhisperExtraArgs ?? "";

            _aliases = new ObservableCollection<RadioAlias>(_settings.Aliases ?? new());
            GridAliases.ItemsSource = _aliases;
        }

        private async Task TestVoiceAsync()
        {
            if (CmbOutput.SelectedItem is not AudioDeviceInfo outDev) return;
            var vol = (float)Clamp01(SldTtsVol.Value);
            await _tts.SpeakToDeviceAsync("Radio check", outDev.Id, vol);
        }

        private async Task TestWavAsync(string path)
        {
            if (CmbOutput.SelectedItem is not AudioDeviceInfo outDev) return;
            var vol = (float)Clamp01(SldBeepVol.Value);

            if (!string.IsNullOrWhiteSpace(path) && System.IO.File.Exists(path))
                await _player.PlayWavFileAsync(path, outDev.Id, vol);
            else
                await _player.PlayDefaultBeepAsync(outDev.Id, vol);
        }

        private async Task TestHotkeyDelayedAsync(string? hotkeyText, System.Windows.Controls.Button btn)
        {
            if (!HotkeyParser.TryParse(hotkeyText, out var hk, out var err))
            {
                System.Windows.MessageBox.Show(
                    this,
                    $"Hotkey inválido: {err}",
                    "Test hotkey",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var oldContent = btn.Content;
            btn.IsEnabled = false;
            btn.Content = "Enviando en 0.8s...";

            try
            {
                await Task.Delay(800);
                KeySender.Send(hk);

                btn.Content = "Enviado";
                await Task.Delay(350);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    this,
                    $"Error enviando hotkey: {ex.Message}",
                    "Test hotkey",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                btn.Content = oldContent;
                btn.IsEnabled = true;
            }
        }

        private void SaveAndClose()
        {
            if (CmbOutput.SelectedItem is AudioDeviceInfo outDev) _settings.OutputDeviceId = outDev.Id;
            if (CmbInput.SelectedItem is AudioDeviceInfo inDev) _settings.InputDeviceId = inDev.Id;

            _settings.TtsVolume = Clamp01(SldTtsVol.Value);
            _settings.BeepVolume = Clamp01(SldBeepVol.Value);

            _settings.DiscordAction = (DiscordAction)(CmbDiscordAction.SelectedItem ?? DiscordAction.None);
            _settings.DiscordMuteHotkey = TxtMuteHotkey.Text ?? "";
            _settings.DiscordDeafenHotkey = TxtDeafenHotkey.Text ?? "";

            _settings.RadioOpenWavPath = TxtOpenWav.Text ?? "";
            _settings.RadioCloseWavPath = TxtCloseWav.Text ?? "";

            _settings.RecordingsFolderPath = TxtRecordingsFolder.Text ?? "";

            _settings.SttEnabled = ChkSttEnabled.IsChecked == true;
            _settings.WhisperExePath = TxtWhisperExe.Text ?? "";
            _settings.WhisperModelPath = TxtWhisperModel.Text ?? "";
            _settings.WhisperLanguage = string.IsNullOrWhiteSpace(TxtWhisperLang.Text) ? "es" : TxtWhisperLang.Text.Trim();
            _settings.WhisperExtraArgs = TxtWhisperArgs.Text ?? "";

            _settings.Aliases = _aliases.ToList();

            _store.SaveForProfile(_profileId, _settings);

            DialogResult = true;
            Close();
        }

        private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);

        private static void PickWav(System.Windows.Controls.TextBox target)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "WAV (*.wav)|*.wav|Todos (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
                target.Text = dlg.FileName;
        }

        private static void PickExe(System.Windows.Controls.TextBox target)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "EXE (*.exe)|*.exe|Todos (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
                target.Text = dlg.FileName;
        }

        private static void PickBin(System.Windows.Controls.TextBox target)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "BIN (*.bin)|*.bin|Todos (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
                target.Text = dlg.FileName;
        }

        private static void PickFolder(System.Windows.Controls.TextBox target)
        {
            using var dlg = new System.Windows.Forms.FolderBrowserDialog();
            dlg.Description = "Elige carpeta para guardar grabaciones";
            dlg.ShowNewFolderButton = true;

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                target.Text = dlg.SelectedPath;
        }
    }
}
