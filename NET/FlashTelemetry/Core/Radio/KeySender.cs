using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlashTelemetry.Core.Radio
{
    public static class KeySender
    {
        private const int INPUT_KEYBOARD = 1;

        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        // “Pulsación humana”
        private const int MOD_SETTLE_MS = 15;
        private const int KEY_HOLD_MS = 90;

        public static Task SendAsync(Hotkey hk, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.Run(() => Send(hk), ct);
        }

        public static void Send(Hotkey hk)
        {
            // Modificadores izquierdos (más consistentes con keybinds)
            var modsDown = new List<INPUT>(4);
            var modsUp = new List<INPUT>(4);

            void AddMod(Keys k)
            {
                modsDown.Add(MakeVk(k, keyUp: false));
                modsUp.Add(MakeVk(k, keyUp: true));
            }

            if (hk.Ctrl) AddMod(Keys.LControlKey);
            if (hk.Alt) AddMod(Keys.LMenu);
            if (hk.Shift) AddMod(Keys.LShiftKey);
            if (hk.Win) AddMod(Keys.LWin);

            // 1) Mods down
            if (modsDown.Count > 0) SendInputs(modsDown);
            Thread.Sleep(MOD_SETTLE_MS);

            // 2) Key down
            SendInputs(new List<INPUT> { MakeVk(hk.Key, keyUp: false) });

            // 3) Hold
            Thread.Sleep(KEY_HOLD_MS);

            // 4) Key up
            SendInputs(new List<INPUT> { MakeVk(hk.Key, keyUp: true) });

            // 5) Mods up (reverse)
            if (modsUp.Count > 0)
            {
                modsUp.Reverse();
                SendInputs(modsUp);
            }
        }

        private static void SendInputs(List<INPUT> inputs)
        {
            int n = inputs.Count;
            if (n <= 0) return;

            uint sent = SendInput((uint)n, inputs.ToArray(), Marshal.SizeOf(typeof(INPUT)));
            if (sent != (uint)n)
            {
                int err = Marshal.GetLastWin32Error();
                throw new Win32Exception(err, $"SendInput falló. Sent={sent}/{n} Win32Error={err}");
            }
        }

        private static INPUT MakeVk(Keys key, bool keyUp)
        {
            uint flags = keyUp ? KEYEVENTF_KEYUP : 0u;
            if (IsExtendedKey(key)) flags |= KEYEVENTF_EXTENDEDKEY;

            return new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = (ushort)key,
                        wScan = 0,
                        dwFlags = flags,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
        }

        private static bool IsExtendedKey(Keys key) => key switch
        {
            Keys.RControlKey or Keys.RMenu => true,

            // Navegación
            Keys.Insert or Keys.Delete or Keys.Home or Keys.End => true,
            Keys.Prior or Keys.Next => true, // PgUp/PgDn
            Keys.Left or Keys.Right or Keys.Up or Keys.Down => true,

            // Numpad especial
            Keys.NumLock or Keys.Divide => true,

            Keys.LWin or Keys.RWin => true,

            _ => false
        };

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(
            uint nInputs,
            [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs,
            int cbSize);

        // IMPORTANTE: union completo para sizeof(INPUT) correcto en x64
        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }
    }
}
