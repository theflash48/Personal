using System;

namespace FlashTelemetry.Profiles
{
    public sealed class ComingSoonProfileModule : IProfileModule
    {
        private readonly Action<string> _log;

        public int ProfileId { get; }
        public string Name { get; }

        public ComingSoonProfileModule(int profileId, string name, Action<string> log)
        {
            ProfileId = profileId;
            Name = $"{name} (Próximamente)";
            _log = log;
        }

        public void Start() => _log($"[MOD] {Name}: Start (sin implementación)");
        public void Stop() => _log($"[MOD] {Name}: Stop");

        public bool TryBuildDataPayload(uint seq, out string payload)
        {
            payload = string.Empty;
            return false;
        }
    }
}
