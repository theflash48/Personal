using System.Windows;
using System.Windows.Controls;
using FlashTelemetry.Core;
using FlashTelemetry.Core.Radio;
using FlashTelemetry.UI;

namespace FlashTelemetry
{
    public partial class MainWindow : Window
    {
        private RadioSettingsStore? _radioStore;
        private bool _radioUiUpdating;

        // Servicios para la ventana de ajustes (solo UI)
        private AudioDeviceService? _radioDevices;
        private TtsService? _radioTts;
        private AudioPlayer? _radioPlayer;

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _radioStore ??= new RadioSettingsStore();

            if (CmbProfiles != null)
            {
                CmbProfiles.SelectionChanged -= CmbProfiles_SelectionChanged;
                CmbProfiles.SelectionChanged += CmbProfiles_SelectionChanged;
            }

            RefreshRadioUiFromSelectedProfile();
        }

        private void CmbProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshRadioUiFromSelectedProfile();
        }

        private int GetSelectedProfileId()
        {
            // En MainWindow.xaml.cs el combo usa SelectedValuePath="Id"
            if (CmbProfiles?.SelectedValue is int idInt)
                return idInt;

            return ProfileCatalog.F1_24;
        }

        private void RefreshRadioUiFromSelectedProfile()
        {
            if (_radioStore == null || ChkRadioEnabled == null || BtnRadio == null)
                return;

            var profileId = GetSelectedProfileId();
            var s = _radioStore.GetForProfile(profileId);

            _radioUiUpdating = true;
            try
            {
                ChkRadioEnabled.IsChecked = s.Enabled;
                BtnRadio.IsEnabled = s.Enabled; // OFF => silencio total
            }
            finally
            {
                _radioUiUpdating = false;
            }
        }

        private void ChkRadioEnabled_Checked(object sender, RoutedEventArgs e) => SetRadioEnabled(true);
        private void ChkRadioEnabled_Unchecked(object sender, RoutedEventArgs e) => SetRadioEnabled(false);

        private void SetRadioEnabled(bool enabled)
        {
            if (_radioStore == null || _radioUiUpdating)
                return;

            var profileId = GetSelectedProfileId();
            var s = _radioStore.GetForProfile(profileId);
            s.Enabled = enabled;
            _radioStore.SetForProfile(profileId, s);

            if (BtnRadio != null)
                BtnRadio.IsEnabled = enabled;
        }

        private void BtnRadio_Click(object sender, RoutedEventArgs e)
        {
            _radioStore ??= new RadioSettingsStore();

            var profileId = GetSelectedProfileId();
            var s = _radioStore.GetForProfile(profileId);

            if (!s.Enabled)
                return;

            // Lazy init de servicios UI
            _radioDevices ??= new AudioDeviceService();
            _radioTts ??= new TtsService();
            _radioPlayer ??= new AudioPlayer();

            var win = new RadioSettingsWindow(profileId, _radioStore, _radioDevices, _radioTts, _radioPlayer)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var ok = win.ShowDialog();
            if (ok == true)
            {
                RefreshRadioUiFromSelectedProfile();
            }
        }
    }
}
