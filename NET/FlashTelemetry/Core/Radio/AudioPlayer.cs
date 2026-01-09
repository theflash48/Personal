using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace FlashTelemetry.Core.Radio
{
    public sealed class AudioPlayer
    {
        public Task PlayDefaultBeepAsync(string outputDeviceId, float volume, CancellationToken ct = default)
            => PlayDefaultBeepAsync(outputDeviceId, volume, frequencyHz: 880, durationMs: 80, ct);

        public Task PlayDefaultBeepAsync(string outputDeviceId, float volume, int durationMs, CancellationToken ct = default)
            => PlayDefaultBeepAsync(outputDeviceId, volume, frequencyHz: 880, durationMs: durationMs, ct);

        public Task PlayDefaultBeepAsync(string outputDeviceId, float volume, int frequencyHz, int durationMs, CancellationToken ct = default)
        {
            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                using var en = new MMDeviceEnumerator();
                using var dev = en.GetDevice(outputDeviceId);

                var signal = new SignalGenerator(44100, 1)
                {
                    Frequency = frequencyHz,
                    Gain = Math.Clamp(volume, 0f, 1f),
                    Type = SignalGeneratorType.Sin
                };

                var take = new OffsetSampleProvider(signal)
                {
                    Take = TimeSpan.FromMilliseconds(durationMs)
                };

                using var wo = new WasapiOut(dev, AudioClientShareMode.Shared, true, 50);
                wo.Init(take.ToWaveProvider16());
                wo.Play();

                // Timeout defensivo: duración + margen
                WaitForStop(wo, TimeSpan.FromMilliseconds(durationMs + 800), ct);
            }, ct);
        }

        public Task PlayWavFileAsync(string wavPath, string outputDeviceId, float volume, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(wavPath) || !File.Exists(wavPath))
                throw new FileNotFoundException("WAV no encontrado", wavPath);

            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                using var en = new MMDeviceEnumerator();
                using var dev = en.GetDevice(outputDeviceId);

                using var reader = new AudioFileReader(wavPath) { Volume = Math.Clamp(volume, 0f, 1f) };
                using var wo = new WasapiOut(dev, AudioClientShareMode.Shared, true, 50);
                wo.Init(reader);
                wo.Play();

                var timeout = reader.TotalTime + TimeSpan.FromSeconds(2);
                WaitForStop(wo, timeout, ct);
            }, ct);
        }

        public Task PlayWavBytesAsync(byte[] wavBytes, string outputDeviceId, float volume, CancellationToken ct = default)
        {
            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                using var en = new MMDeviceEnumerator();
                using var dev = en.GetDevice(outputDeviceId);

                using var ms = new MemoryStream(wavBytes);
                using var rdr = new WaveFileReader(ms);
                using var vol = new WaveChannel32(rdr) { Volume = Math.Clamp(volume, 0f, 1f) };

                using var wo = new WasapiOut(dev, AudioClientShareMode.Shared, true, 50);
                wo.Init(vol);
                wo.Play();

                var timeout = rdr.TotalTime + TimeSpan.FromSeconds(2);
                WaitForStop(wo, timeout, ct);
            }, ct);
        }

        private static void WaitForStop(WasapiOut wo, TimeSpan timeout, CancellationToken ct)
        {
            using var done = new ManualResetEventSlim(false);
            Exception? playbackException = null;

            wo.PlaybackStopped += (_, e) =>
            {
                playbackException = e.Exception;
                done.Set();
            };

            var sw = Stopwatch.StartNew();

            while (true)
            {
                // Espera breve para reaccionar a cancelación
                if (done.Wait(50))
                    break;

                ct.ThrowIfCancellationRequested();

                if (sw.Elapsed > timeout)
                {
                    try { wo.Stop(); } catch { /* ignore */ }
                    done.Wait(500);
                    break;
                }
            }

            if (playbackException != null)
                throw playbackException;
        }
    }
}
