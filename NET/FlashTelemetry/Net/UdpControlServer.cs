using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FlashTelemetry.Protocol;

namespace FlashTelemetry.Net
{
    public sealed class UdpControlServer : IDisposable
    {
        private readonly int _port;
        private UdpClient? _udp;
        private CancellationTokenSource? _cts;
        private Task? _rxTask;

        public event Action<string>? Log;

        public event Func<HelloMessage, Task>? HelloReceived;
        public event Func<SetProfileMessage, Task>? SetProfileReceived;
        public event Func<PingMessage, Task>? PingReceived;

        // NUEVO
        public event Func<PttToggleMessage, Task>? PttToggleReceived;

        public UdpControlServer(int port)
        {
            _port = port;
        }

        public void Start()
        {
            if (_rxTask != null) return;

            _cts = new CancellationTokenSource();
            _udp = new UdpClient(new IPEndPoint(IPAddress.Any, _port));
            _udp.Client.ReceiveBufferSize = 1 * 1024 * 1024;

            Log?.Invoke($"[CTRL] Escuchando UDP en 0.0.0.0:{_port}");

            _rxTask = Task.Run(() => RxLoop(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();

            try { _udp?.Close(); } catch { }
            _udp?.Dispose();
            _udp = null;

            _cts?.Dispose();
            _cts = null;

            _rxTask = null;
        }

        public async Task SendAsync(string msg, IPEndPoint remote)
        {
            if (_udp == null) return;

            byte[] bytes = Encoding.UTF8.GetBytes(msg);
            await _udp.SendAsync(bytes, bytes.Length, remote);

            Log?.Invoke($"[CTRL] TX {remote.Address}:{remote.Port}: {msg}");
        }

        private async Task RxLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _udp != null)
            {
                UdpReceiveResult res;
                try
                {
                    res = await _udp.ReceiveAsync(ct);
                }
                catch (OperationCanceledException) { break; }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex)
                {
                    Log?.Invoke($"[CTRL] RX error: {ex.Message}");
                    try { await Task.Delay(200, ct); } catch { }
                    continue;
                }

                string msg = Encoding.UTF8.GetString(res.Buffer).Trim();
                if (string.IsNullOrWhiteSpace(msg))
                    continue;

                if (msg.StartsWith("HELLO", StringComparison.OrdinalIgnoreCase))
                {
                    var hello = new HelloMessage(
                        Remote: res.RemoteEndPoint,
                        SelectedProfileId: ParseInt(msg, "sel", -1),
                        Proto: ParseInt(msg, "proto", 1),
                        DeviceId: ParseStr(msg, "dev") ?? "ESP32",
                        Token: ParseStr(msg, "token") ?? "0"
                    );

                    if (HelloReceived != null)
                        await HelloReceived.Invoke(hello);
                }
                else if (msg.StartsWith("SET_PROFILE", StringComparison.OrdinalIgnoreCase))
                {
                    var sp = new SetProfileMessage(
                        Remote: res.RemoteEndPoint,
                        ProfileId: ParseInt(msg, "id", -1),
                        Token: ParseStr(msg, "token") ?? "0"
                    );

                    if (SetProfileReceived != null)
                        await SetProfileReceived.Invoke(sp);
                }
                else if (msg.StartsWith("PTT_TOGGLE", StringComparison.OrdinalIgnoreCase))
                {
                    var ptt = new PttToggleMessage(
                        Remote: res.RemoteEndPoint,
                        Token: ParseStr(msg, "token") ?? "0"
                    );

                    if (PttToggleReceived != null)
                        await PttToggleReceived.Invoke(ptt);
                    else
                        Log?.Invoke($"[CTRL] RX {res.RemoteEndPoint.Address}:{res.RemoteEndPoint.Port}: {msg}");
                }
                else if (msg.StartsWith("PTT", StringComparison.OrdinalIgnoreCase))
                {
                    // Compat: "PTT state=1" (solo disparamos toggle cuando state==1)
                    int state = ParseInt(msg, "state", int.MinValue);
                    if (state == int.MinValue) state = ParseInt(msg, "v", 0);

                    if (state == 1)
                    {
                        var ptt = new PttToggleMessage(
                            Remote: res.RemoteEndPoint,
                            Token: ParseStr(msg, "token") ?? "0"
                        );

                        if (PttToggleReceived != null)
                            await PttToggleReceived.Invoke(ptt);
                        else
                            Log?.Invoke($"[CTRL] RX {res.RemoteEndPoint.Address}:{res.RemoteEndPoint.Port}: {msg}");
                    }
                }
                else if (msg.StartsWith("PING", StringComparison.OrdinalIgnoreCase))
                {
                    var ping = new PingMessage(
                        Remote: res.RemoteEndPoint,
                        Seq: ParseInt(msg, "seq", 0)
                    );

                    if (PingReceived != null)
                        await PingReceived.Invoke(ping);
                }
                else
                {
                    Log?.Invoke($"[CTRL] RX {res.RemoteEndPoint.Address}:{res.RemoteEndPoint.Port}: {msg}");
                }
            }
        }

        private static int ParseInt(string s, string key, int defVal)
        {
            string? v = ParseStr(s, key);
            return int.TryParse(v, out int n) ? n : defVal;
        }

        private static string? ParseStr(string s, string key)
        {
            int idx = s.IndexOf(key + "=", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;

            idx += key.Length + 1;
            int end = s.IndexOf(' ', idx);
            if (end < 0) end = s.Length;

            return s.Substring(idx, end - idx).Trim();
        }

        public void Dispose() => Stop();
    }
}
