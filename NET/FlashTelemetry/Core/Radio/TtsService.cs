using System;
using System.IO;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;

namespace FlashTelemetry.Core.Radio
{
    public sealed class TtsService
    {
        private readonly SpeechSynthesizer _tts = new();

        public Task SpeakToDeviceAsync(string text, string outputDeviceId, float volume01, CancellationToken ct = default)
        {
            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                // Sintetiza a WAV en memoria
                using var ms = new MemoryStream();
                _tts.SetOutputToWaveStream(ms);
                _tts.Volume = (int)Math.Clamp(volume01 * 100f, 0f, 100f);

                // Por defecto: voz actual del sistema. (Luego si quieres, selector de voz)
                _tts.Speak(text);

                _tts.SetOutputToNull();
                var wav = ms.ToArray();

                // Reproduce por el dispositivo elegido
                var player = new AudioPlayer();
                player.PlayWavBytesAsync(wav, outputDeviceId, Math.Clamp(volume01, 0f, 1f), ct).GetAwaiter().GetResult();
            }, ct);
        }

        // Alias por compatibilidad si algún sitio llama SpeakAsync(...)
        public Task SpeakAsync(string text, string outputDeviceId, float volume01, CancellationToken ct = default)
            => SpeakToDeviceAsync(text, outputDeviceId, volume01, ct);
    }
}
