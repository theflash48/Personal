using System;

namespace FlashTelemetry.Core
{
    public sealed class ProfileManager
    {
        public int ActiveProfileId { get; private set; } = ProfileCatalog.F1_24;

        public string ActiveProfileName =>
            ProfileCatalog.Profiles.TryGetValue(ActiveProfileId, out var info) ? info.Name : $"Unknown({ActiveProfileId})";

        public event Action<int>? ActiveProfileChanged;

        public bool TrySetActive(int id)
        {
            if (!ProfileCatalog.Profiles.TryGetValue(id, out var info))
                return false;

            if (!info.Enabled)
                return false;

            if (ActiveProfileId == id)
                return true;

            ActiveProfileId = id;
            ActiveProfileChanged?.Invoke(id);
            return true;
        }
    }
}
