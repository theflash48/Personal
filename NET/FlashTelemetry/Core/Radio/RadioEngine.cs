using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FlashTelemetry.Core.Radio
{
    public sealed class RadioEngine : IDisposable
    {
        private readonly SemaphoreSlim _gate = new(1, 1);

        private readonly RadioSettingsStore _store;
        private readonly Func<int> _getActiveProfileId;
        private readonly Action<string> _log;

        private readonly AudioDeviceService _devices = new();
        private readonly TtsService _tts = new();
        private readonly AudioPlayer _player = new();
        private readonly RadioRecorder _recorder = new();
        private readonly DiscordHotkeys _discord;
        private readonly WhisperService _whisper = new();

        private bool _open;
        private string? _currentWavPath;

        // Post-proceso (STT+TTS) para no bloquear el PTT
        private CancellationTokenSource? _postCts;
        private Task? _postTask;

        public RadioEngine(RadioSettingsStore store, Func<int> getActiveProfileId, Action<string> log)
        {
            _store = store;
            _getActiveProfileId = getActiveProfileId;
            _log = log;
            _discord = new DiscordHotkeys(log);
        }

        public async Task ToggleAsync(CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct);
            try
            {
                if (_open) await CloseCoreAsync(ct);
                else await OpenCoreAsync(ct);
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task ForceCloseAsync(CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct);
            try
            {
                if (_open) await CloseCoreAsync(ct);
            }
            finally
            {
                _gate.Release();
            }
        }

        private async Task OpenCoreAsync(CancellationToken ct)
        {
            // Si había post-proceso (Base hablando), lo cancelamos para liberar audio/flujo
            CancelPostProcess();

            var profileId = _getActiveProfileId();
            var s = _store.GetForProfile(profileId);

            if (!s.Enabled)
            {
                _log("[RADIO] Ignorado: Team Radio desactivado.");
                return;
            }

            var outId = _devices.ResolveOutputIdOrDefault(s.OutputDeviceId);
            var inId = _devices.ResolveInputIdOrDefault(s.InputDeviceId);

            // 1) Discord primero (silencio)
            await _discord.ApplyOnOpenAsync(s, ct);

            // 2) Sonido OPEN (usuario)
            await PlayOpenAsync(s, outId, ct);

            // 3) Grabación
            _currentWavPath = BuildRecordingPath(profileId, s);
            _recorder.Start(inId, _currentWavPath);

            _open = true;
            _log($"[RADIO] OPEN -> grabando: {_currentWavPath}");
        }

        private async Task CloseCoreAsync(CancellationToken ct)
        {
            var profileId = _getActiveProfileId();
            var s = _store.GetForProfile(profileId);

            var outId = _devices.ResolveOutputIdOrDefault(s.OutputDeviceId);

            // 1) Parar grabación
            _recorder.Stop();

            // 2) Sonido CLOSE (usuario)
            await PlayCloseAsync(s, outId, ct);

            // 3) Revertir Discord (unmute/undeafen)
            await _discord.ApplyOnCloseAsync(s, ct);

            _open = false;
            _log("[RADIO] CLOSE");

            // Captura WAV y limpia estado para la siguiente apertura
            var wavPath = _currentWavPath;
            _currentWavPath = null;

            // 4) Lanzar STT+TTS en segundo plano (NO bloquear el PTT)
            if (s.SttEnabled && !string.IsNullOrWhiteSpace(wavPath))
            {
                StartPostProcess(profileId, s, outId, wavPath);
            }
        }

        private void StartPostProcess(int profileId, RadioSettings s, string outId, string wavPath)
        {
            CancelPostProcess();

            _postCts = new CancellationTokenSource();
            var ct = _postCts.Token;

            _postTask = Task.Run(async () =>
            {
                try
                {
                    var text = await _whisper.TranscribeAsync(
                        wavPath: wavPath,
                        whisperExePath: s.WhisperExePath,
                        modelPath: s.WhisperModelPath,
                        language: s.WhisperLanguage,
                        extraArgs: s.WhisperExtraArgs,
                        log: _log,
                        ct: ct);

                    if (string.IsNullOrWhiteSpace(text))
                        return;

                    var ttsVol = (float)Math.Clamp(s.TtsVolume, 0.0, 1.0);

                    // Canal Base: OPEN -> hablar -> CLOSE
                    await PlayOpenAsync(s, outId, ct);

                    _log($"[RADIO] TTS (eco) -> {text}");
                    await _tts.SpeakToDeviceAsync(text, outId, ttsVol, ct);

                    await PlayCloseAsync(s, outId, ct);
                }
                catch (OperationCanceledException)
                {
                    _log("[RADIO] Post-proceso cancelado.");
                }
                catch (Exception ex)
                {
                    _log($"[RADIO] STT/TTS ERROR -> {ex.Message}");
                }
            }, ct);
        }

        private void CancelPostProcess()
        {
            try
            {
                _postCts?.Cancel();
                _postCts?.Dispose();
            }
            catch { /* ignore */ }
            finally
            {
                _postCts = null;
                _postTask = null;
            }
        }

        private async Task PlayOpenAsync(RadioSettings s, string outId, CancellationToken ct)
        {
            var vol = (float)Math.Clamp(s.BeepVolume, 0.0, 1.0);

            if (!string.IsNullOrWhiteSpace(s.RadioOpenWavPath) && File.Exists(s.RadioOpenWavPath))
                await _player.PlayWavFileAsync(s.RadioOpenWavPath, outId, vol, ct);
            else
                await _player.PlayDefaultBeepAsync(outId, vol, ct);
        }

        private async Task PlayCloseAsync(RadioSettings s, string outId, CancellationToken ct)
        {
            var vol = (float)Math.Clamp(s.BeepVolume, 0.0, 1.0);

            if (!string.IsNullOrWhiteSpace(s.RadioCloseWavPath) && File.Exists(s.RadioCloseWavPath))
                await _player.PlayWavFileAsync(s.RadioCloseWavPath, outId, vol, ct);
            else
                await _player.PlayDefaultBeepAsync(outId, vol, ct);
        }

        private static string BuildRecordingPath(int profileId, RadioSettings s)
        {
            string root;
            if (!string.IsNullOrWhiteSpace(s.RecordingsFolderPath) && Directory.Exists(s.RecordingsFolderPath))
            {
                root = Path.Combine(s.RecordingsFolderPath, profileId.ToString());
            }
            else
            {
                root = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "FlashTelemetry",
                    "recordings",
                    profileId.ToString());
            }

            Directory.CreateDirectory(root);

            var name = $"radio_{DateTime.Now:yyyyMMdd_HHmmss}.wav";
            return Path.Combine(root, name);
        }

        public void Dispose()
        {
            CancelPostProcess();
            _recorder.Dispose();
        }
    }
}
