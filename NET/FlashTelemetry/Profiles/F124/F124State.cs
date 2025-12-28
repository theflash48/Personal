using System;

namespace FlashTelemetry.Profiles.F124
{
    public sealed class F124State
    {
        private readonly object _lock = new();

        private int _speedKph;
        private int _rpm;
        private int _gear;
        private int _drs;

        private int _ersMode;
        private int _ersPct;

        private DateTime _lastUpdateUtc = DateTime.MinValue;

        public void UpdateTelemetry(int speedKph, int rpm, int gear, int drs)
        {
            lock (_lock)
            {
                _speedKph = speedKph;
                _rpm = rpm;
                _gear = gear;
                _drs = drs;
                _lastUpdateUtc = DateTime.UtcNow;
            }
        }

        public void UpdateErs(int ersMode, int ersPct)
        {
            lock (_lock)
            {
                _ersMode = ersMode;
                _ersPct = Math.Clamp(ersPct, 0, 100);
                _lastUpdateUtc = DateTime.UtcNow;
            }
        }

        public bool TrySnapshot(TimeSpan maxAge, out Snapshot snap)
        {
            lock (_lock)
            {
                if (_lastUpdateUtc == DateTime.MinValue)
                {
                    snap = default;
                    return false;
                }

                if (DateTime.UtcNow - _lastUpdateUtc > maxAge)
                {
                    snap = default;
                    return false;
                }

                snap = new Snapshot(
                    SpeedKph: _speedKph,
                    Rpm: _rpm,
                    Gear: _gear,
                    Drs: _drs,
                    ErsMode: _ersMode,
                    ErsPct: _ersPct,
                    LastUpdateUtc: _lastUpdateUtc
                );
                return true;
            }
        }

        public readonly record struct Snapshot(
            int SpeedKph,
            int Rpm,
            int Gear,
            int Drs,
            int ErsMode,
            int ErsPct,
            DateTime LastUpdateUtc
        );
    }
}
