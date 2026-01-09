using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace FlashTelemetry.Net
{
    public sealed class UdpDataSender : IDisposable
    {
        private readonly UdpClient _udp = new();
        private System.Threading.Timer? _timer;

        private IPEndPoint? _target;
        private Func<string>? _buildPayload;
        private Action<string>? _log;

        private readonly object _lock = new();

        public void Configure(IPEndPoint target, Func<string> buildPayload, Action<string> log)
        {
            _target = target;
            _buildPayload = buildPayload;
            _log = log;
        }

        public void Start(int hz)
        {
            if (hz <= 0) throw new ArgumentOutOfRangeException(nameof(hz));
            int periodMs = Math.Max(1, 1000 / hz);

            Stop();
            _timer = new System.Threading.Timer(_ => Tick(), null, 0, periodMs);
            _log?.Invoke($"[DATA] Start {hz} Hz (period={periodMs}ms)");
        }

        public void Stop()
        {
            _timer?.Dispose();
            _timer = null;
        }

        private void Tick()
        {
            lock (_lock)
            {
                try
                {
                    if (_target == null || _buildPayload == null) return;

                    string msg = _buildPayload();
                    if (string.IsNullOrWhiteSpace(msg)) return;

                    byte[] bytes = Encoding.UTF8.GetBytes(msg);
                    _udp.Send(bytes, bytes.Length, _target);
                }
                catch (Exception ex)
                {
                    _log?.Invoke($"[DATA] Error TX: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            Stop();
            _udp.Dispose();
        }
    }
}
