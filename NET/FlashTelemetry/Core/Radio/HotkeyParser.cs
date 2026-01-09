using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace FlashTelemetry.Core.Radio
{
    public readonly struct Hotkey
    {
        public bool Ctrl { get; init; }
        public bool Alt { get; init; }
        public bool Shift { get; init; }
        public bool Win { get; init; }
        public Keys Key { get; init; }

        public override string ToString()
        {
            var parts = new List<string>();
            if (Ctrl) parts.Add("CTRL");
            if (Alt) parts.Add("ALT");
            if (Shift) parts.Add("SHIFT");
            if (Win) parts.Add("WIN");
            parts.Add(Key.ToString());
            return string.Join("+", parts);
        }
    }

    public static class HotkeyParser
    {
        // La UI te mete cosas como "CTRL+ALT+MAYÚS+F11"
        // Dejamos esta firma porque tu código la llama con 3 args.
        public static bool TryParse(string? text, out Hotkey hotkey, out string error)
        {
            hotkey = default;
            error = "";

            if (string.IsNullOrWhiteSpace(text))
            {
                error = "Hotkey vacío.";
                return false;
            }

            var normalized = Normalize(text);
            var parts = normalized.Split('+', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(p => p.Trim())
                                  .Where(p => p.Length > 0)
                                  .ToArray();

            if (parts.Length == 0)
            {
                error = "Hotkey vacío.";
                return false;
            }

            bool ctrl = false, alt = false, shift = false, win = false;
            Keys? key = null;

            foreach (var p in parts)
            {
                var up = p.ToUpperInvariant();

                if (up is "CTRL" or "CONTROL")
                {
                    ctrl = true; continue;
                }
                if (up == "ALT")
                {
                    alt = true; continue;
                }
                if (up is "SHIFT" or "MAYUS" or "MAYÚS" or "MAYUSC" or "MAYUSCULAS")
                {
                    shift = true; continue;
                }
                if (up is "WIN" or "WINDOWS" or "LWIN" or "RWIN")
                {
                    win = true; continue;
                }

                // Si no es modificador, tiene que ser la tecla final
                if (key != null)
                {
                    error = $"Demasiadas teclas finales: '{p}'.";
                    return false;
                }

                if (!TryParseKey(up, out var k))
                {
                    error = $"Tecla no reconocida: '{p}'.";
                    return false;
                }

                key = k;
            }

            if (key == null || key == Keys.None)
            {
                error = "Falta la tecla final (ej: F11).";
                return false;
            }

            hotkey = new Hotkey
            {
                Ctrl = ctrl,
                Alt = alt,
                Shift = shift,
                Win = win,
                Key = key.Value
            };

            return true;
        }

        private static string Normalize(string s)
        {
            // Normaliza variaciones típicas (incluyendo acentos)
            return s.Replace("MAYÚS", "SHIFT", StringComparison.OrdinalIgnoreCase)
                    .Replace("MAYUS", "SHIFT", StringComparison.OrdinalIgnoreCase)
                    .Replace("MAYUSC", "SHIFT", StringComparison.OrdinalIgnoreCase)
                    .Replace("MAYUSCULAS", "SHIFT", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryParseKey(string up, out Keys key)
        {
            key = Keys.None;

            // F1..F24
            if (up.Length >= 2 && up[0] == 'F' && int.TryParse(up[1..], out var fn) && fn is >= 1 and <= 24)
            {
                key = Keys.F1 + (fn - 1);
                return true;
            }

            // Letras A..Z
            if (up.Length == 1 && up[0] is >= 'A' and <= 'Z')
            {
                key = (Keys)Enum.Parse(typeof(Keys), up);
                return true;
            }

            // Dígitos 0..9
            if (up.Length == 1 && up[0] is >= '0' and <= '9')
            {
                key = Keys.D0 + (up[0] - '0');
                return true;
            }

            // Algunos comunes
            return Enum.TryParse(up, ignoreCase: true, out key);
        }
    }
}
