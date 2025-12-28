using System;

namespace FlashTelemetry.Profiles.ETS2
{
    public sealed class Ets2ProfileModule : IProfileModule
    {
        private readonly Action<string> _log;

        public int ProfileId => Core.ProfileCatalog.ETS2;
        public string Name => "ETS2";

        public Ets2ProfileModule(Action<string> log)
        {
            _log = log;
        }

        public void Start()
        {
            _log("[MOD] ETS2: Start (placeholder, todavía sin receptor real)");
        }

        public void Stop()
        {
            _log("[MOD] ETS2: Stop");
        }

        public bool TryBuildDataPayload(uint seq, out string payload)
        {
            payload = string.Empty;
            return false;
        }
    }
}
