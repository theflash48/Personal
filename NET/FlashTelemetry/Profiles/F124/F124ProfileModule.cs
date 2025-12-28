using System;

namespace FlashTelemetry.Profiles.F124
{
    public sealed class F124ProfileModule : FlashTelemetry.Profiles.IProfileModule
    {
        private readonly Action<string> _log;
        private readonly F124State _state = new();
        private readonly F124UdpReceiver _rx;

        private bool _running;

        public int ProfileId => Core.ProfileCatalog.F1_24;
        public string Name => "F1 24";

        public F124ProfileModule(int listenPort, Action<string> log)
        {
            _log = log;
            _rx = new F124UdpReceiver(listenPort, _state, log);
        }

        public void Start()
        {
            if (_running) return;
            _running = true;

            _log("[MOD] F1 24: Start (UDP 20777)");
            _rx.Start();
        }

        public void Stop()
        {
            if (!_running) return;
            _running = false;

            _log("[MOD] F1 24: Stop");
            _rx.Stop();
        }

        public bool TryBuildDataPayload(uint seq, out string payload)
        {
            payload = string.Empty;

            // Si el módulo no está en marcha, no enviamos
            if (!_running) return false;

            // 60Hz del juego => esto es un margen razonable
            if (!_state.TrySnapshot(maxAge: TimeSpan.FromMilliseconds(500), out var snap))
                return false;

            // Formato DATA (texto, fácil de debug)
            // Gear: -1..8 (N=0)
            payload =
                $"DATA p={ProfileId} seq={seq} rpm={snap.Rpm} spd={snap.SpeedKph} gear={snap.Gear} drs={snap.Drs} ersMode={snap.ErsMode} ersPct={snap.ErsPct}";
            return true;
        }
    }
}
