using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace FlashTelemetry.Core.Radio
{
    public sealed class WhisperService
    {
        public async Task<string?> TranscribeAsync(
            string wavPath,
            string whisperExePath,
            string modelPath,
            string language,
            string extraArgs,
            Action<string> log,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(whisperExePath) || !File.Exists(whisperExePath))
            {
                log("[RADIO] STT: whisper-cli.exe no encontrado (ruta inválida).");
                return null;
            }

            if (string.IsNullOrWhiteSpace(modelPath) || !File.Exists(modelPath))
            {
                log("[RADIO] STT: modelo .bin no encontrado (ruta inválida).");
                return null;
            }

            if (string.IsNullOrWhiteSpace(wavPath) || !File.Exists(wavPath))
            {
                log("[RADIO] STT: WAV no encontrado.");
                return null;
            }

            string inputForWhisper = wavPath;
            string? tempNormalized = null;

            try
            {
                // 1) Normalizar audio (mono 16kHz PCM16) para mejorar precisión
                (inputForWhisper, tempNormalized) = Ensure16kMonoPcm16(wavPath, log);

                // 2) Construir args
                var args = new StringBuilder();
                args.Append("-m ").Append('"').Append(modelPath).Append('"');
                args.Append(" -f ").Append('"').Append(inputForWhisper).Append('"');

                if (!string.IsNullOrWhiteSpace(language))
                    args.Append(" -l ").Append(language.Trim());

                if (!string.IsNullOrWhiteSpace(extraArgs))
                    args.Append(' ').Append(extraArgs.Trim());

                var psi = new ProcessStartInfo
                {
                    FileName = whisperExePath,
                    Arguments = args.ToString(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                };

                log("[RADIO] STT: ejecutando whisper...");

                using var p = new Process { StartInfo = psi };

                try
                {
                    p.Start();

                    // Leer salida en paralelo para evitar bloqueos
                    var stdoutTask = p.StandardOutput.ReadToEndAsync();
                    var stderrTask = p.StandardError.ReadToEndAsync();

                    await p.WaitForExitAsync(ct);

                    var stdout = await stdoutTask;
                    var stderr = await stderrTask;

                    // IMPORTANTE: si falla, NO devolvemos texto (evita que el TTS lea basura tipo system_info)
                    if (p.ExitCode != 0)
                    {
                        log($"[RADIO] STT: whisper exit={p.ExitCode}");
                        if (!string.IsNullOrWhiteSpace(stderr))
                            log($"[RADIO] STT stderr: {stderr.Trim()}");
                        return null;
                    }

                    // En ejecución correcta, la transcripción debe venir por stdout
                    var text = ExtractText(stdout);

                    if (!string.IsNullOrWhiteSpace(text))
                        log($"[RADIO] STT -> {text}");

                    return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
                }
                catch (OperationCanceledException)
                {
                    log("[RADIO] STT cancelado.");
                    return null;
                }
                catch (Exception ex)
                {
                    log($"[RADIO] STT ERROR -> {ex.Message}");
                    return null;
                }
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(tempNormalized))
                {
                    try { File.Delete(tempNormalized); } catch { /* ignore */ }
                }
            }
        }

        private static (string pathToUse, string? tempPath) Ensure16kMonoPcm16(string srcWav, Action<string> log)
        {
            try
            {
                using (var r = new WaveFileReader(srcWav))
                {
                    var wf = r.WaveFormat;

                    bool alreadyOk =
                        wf.Encoding == WaveFormatEncoding.Pcm &&
                        wf.SampleRate == 16000 &&
                        wf.Channels == 1 &&
                        wf.BitsPerSample == 16;

                    if (alreadyOk)
                        return (srcWav, null);
                }

                var temp = Path.Combine(Path.GetTempPath(), $"flashtelemetry_stt_{Guid.NewGuid():N}.wav");

                using var reader = new AudioFileReader(srcWav); // float
                ISampleProvider sample = reader;

                // downmix estéreo -> mono (caso típico)
                if (sample.WaveFormat.Channels == 2)
                {
                    sample = new StereoToMonoSampleProvider(sample)
                    {
                        LeftVolume = 0.5f,
                        RightVolume = 0.5f
                    };
                }
                else if (sample.WaveFormat.Channels != 1)
                {
                    log($"[RADIO] STT: audio con {sample.WaveFormat.Channels} canales; se usa WAV original (sin normalizar).");
                    return (srcWav, null);
                }

                if (sample.WaveFormat.SampleRate != 16000)
                    sample = new WdlResamplingSampleProvider(sample, 16000);

                WaveFileWriter.CreateWaveFile16(temp, sample);

                log("[RADIO] STT: WAV normalizado a mono 16kHz PCM16.");
                return (temp, temp);
            }
            catch (Exception ex)
            {
                log($"[RADIO] STT: no se pudo normalizar WAV ({ex.Message}). Se usa WAV original.");
                return (srcWav, null);
            }
        }

        private static string ExtractText(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";

            // Si usas -nt, suele venir una línea limpia por salida estándar.
            // Si NO usas -nt, a veces viene con timestamps: [00:00:00.000 --> ...] texto
            var sb = new StringBuilder();

            foreach (var raw in s.Split('\n'))
            {
                var t = raw.Trim();
                if (t.Length == 0) continue;

                // Ignorar líneas de info/ruido si alguna se colara en stdout
                if (t.StartsWith("system_info:", StringComparison.OrdinalIgnoreCase)) continue;
                if (t.StartsWith("main:", StringComparison.OrdinalIgnoreCase)) continue;
                if (t.StartsWith("whisper_", StringComparison.OrdinalIgnoreCase)) continue;

                // Caso timestamps
                var idx = t.IndexOf(']');
                if (idx >= 0 && idx + 1 < t.Length && t.StartsWith("["))
                {
                    var rest = t[(idx + 1)..].Trim();
                    if (rest.Length > 0)
                    {
                        if (sb.Length > 0) sb.Append(' ');
                        sb.Append(rest);
                    }
                    continue;
                }

                // Caso salida limpia
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(t);
            }

            return sb.ToString();
        }
    }
}
