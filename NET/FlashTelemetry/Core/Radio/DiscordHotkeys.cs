using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlashTelemetry.Core.Radio
{
    public sealed class DiscordHotkeys
    {
        private readonly Action<string> _log;

        public DiscordHotkeys(Action<string> log)
        {
            _log = log;
        }

        public async Task<bool> ApplyOnOpenAsync(RadioSettings s, CancellationToken ct = default)
            => await ApplyAsync(s, when: "open", ct);

        public async Task<bool> ApplyOnCloseAsync(RadioSettings s, CancellationToken ct = default)
            => await ApplyAsync(s, when: "close", ct);

        private async Task<bool> ApplyAsync(RadioSettings s, string when, CancellationToken ct)
        {
            try
            {
                return s.DiscordAction switch
                {
                    DiscordAction.None => true,
                    DiscordAction.MuteToggle => await SendAsync(s.DiscordMuteHotkey, ct),
                    DiscordAction.DeafenToggle => await SendAsync(s.DiscordDeafenHotkey, ct),
                    _ => true
                };
            }
            catch (Exception ex)
            {
                _log($"[RADIO] Discord {when}: ERROR -> {ex.Message}");
                return false;
            }
        }

        public Task<bool> SendAsync(string hotkeyText, CancellationToken ct = default)
            => SendAsyncStatic(hotkeyText, _log, ct);

        public static Task<bool> SendAsyncStatic(string hotkeyText, Action<string>? log = null, CancellationToken ct = default)
        {
            if (!HotkeyParser.TryParse(hotkeyText, out var hk, out var err))
            {
                log?.Invoke($"[RADIO] Hotkey inválido: {err}");
                return Task.FromResult(false);
            }

            return Task.Run(() =>
            {
                try
                {
                    KeySender.Send(hk);
                    log?.Invoke($"[RADIO] Discord hotkey OK -> {hk}");
                    return true;
                }
                catch (Exception ex)
                {
                    log?.Invoke($"[RADIO] Discord hotkey ERROR -> {ex.Message}");
                    return false;
                }
            }, ct);
        }
    }
}
