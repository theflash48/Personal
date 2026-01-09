using System.Collections.Generic;

namespace FlashTelemetry.Core
{
    public static class ProfileCatalog
    {
        public const int ETS2 = 1;
        public const int AM2 = 2;
        public const int F1_24 = 3;
        public const int F1_25 = 4;

        // Compatibilidad: en algunas partes (UI) lo referimos como ProfileItem
        public record ProfileItem(string Name, bool Enabled);

        // Lo que ya tenías (lo dejamos, heredando de ProfileItem)
        public record ProfileInfo(string Name, bool Enabled) : ProfileItem(Name, Enabled);

        public static readonly Dictionary<int, ProfileInfo> Profiles = new()
        {
            { ETS2,  new ProfileInfo("ETS2", true) },
            { AM2,   new ProfileInfo("Automobilista 2", false) },
            { F1_24, new ProfileInfo("F1 24", true) },
            { F1_25, new ProfileInfo("F1 25", false) },
        };
    }
}
