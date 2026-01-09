using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
            if (string.IsNullOrWhiteSpace(whisperExePath) || !System.IO.File.Exists(whisperExePath))
            {
                log("[RADIO] STT: whisper-cli.exe no encontrado (ruta inválida).");
                return null;
            }
            if (string.IsNullOrWhiteSpace(modelPath) || !System.IO.File.Exists(modelPath))
            {
                log("[RADIO] STT: modelo .bin no encontrado (ruta inválida).");
                return null;
            }
            if (string.IsNullOrWhiteSpace(wavPath) || !System.IO.File.Exists(wavPath))
            {
                log("[RADIO] STT: WAV no encontrado.");
                return null;
            }

            // Args mínimos seguros
            var args = new StringBuilder();
            args.Append("-m ").Append('"').Append(modelPath).Append('"');
            args.Append(" -f ").Append('"').Append(wavPath).Append('"');
            if (!string.IsNullOrWhiteSpace(language))
                args.Append(" -l ").Append(language.Trim());

            if (!string.IsNullOrWhiteSpace(extraArgs))
                args.Append(' ').Append(extraArgs);

            var psi = new ProcessStartInfo
            {
                FileName = whisperExePath,
                Arguments = args.ToString(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            log($"[RADIO] STT: ejecutando whisper...");
            // log($"[RADIO] STT CMD: {psi.FileName} {psi.Arguments}");

            using var p = new Process { StartInfo = psi };

            try
            {
                p.Start();

                var stdout = await p.StandardOutput.ReadToEndAsync();
                var stderr = await p.StandardError.ReadToEndAsync();

                await p.WaitForExitAsync(ct);

                if (p.ExitCode != 0)
                {
                    log($"[RADIO] STT: whisper exit={p.ExitCode}");
                    if (!string.IsNullOrWhiteSpace(stderr))
                        log($"[RADIO] STT stderr: {stderr.Trim()}");
                }

                var text = ExtractText(stdout);
                if (string.IsNullOrWhiteSpace(text))
                {
                    // a veces imprime cosas por stderr según builds
                    text = ExtractText(stderr);
                }

                if (!string.IsNullOrWhiteSpace(text))
                    log($"[RADIO] STT -> {text}");

                return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
            }
            catch (Exception ex)
            {
                log($"[RADIO] STT ERROR -> {ex.Message}");
                return null;
            }
        }

        private static string ExtractText(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";

            // Whisper suele sacar líneas: [00:00:00.000 --> 00:00:02.000]  texto
            // Nos quedamos con lo de después del ']'
            var sb = new StringBuilder();
            foreach (var line in s.Split('\n'))
            {
                var t = line.Trim();
                if (t.Length == 0) continue;

                var idx = t.IndexOf(']');
                if (idx >= 0 && idx + 1 < t.Length)
                {
                    var rest = t[(idx + 1)..].Trim();
                    if (rest.Length > 0)
                    {
                        if (sb.Length > 0) sb.Append(' ');
                        sb.Append(rest);
                    }
                }
            }
            return sb.ToString();
        }
    }
}
