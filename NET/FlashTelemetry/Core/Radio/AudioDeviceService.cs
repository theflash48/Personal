using System.Collections.Generic;
using System.Linq;
using NAudio.CoreAudioApi;

namespace FlashTelemetry.Core.Radio
{
    public sealed class AudioDeviceService
    {
        public List<AudioDeviceInfo> GetOutputDevices()
        {
            using var en = new MMDeviceEnumerator();
            return en.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                     .Select(d => new AudioDeviceInfo { Id = d.ID, Name = d.FriendlyName })
                     .ToList();
        }

        public List<AudioDeviceInfo> GetInputDevices()
        {
            using var en = new MMDeviceEnumerator();
            return en.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
                     .Select(d => new AudioDeviceInfo { Id = d.ID, Name = d.FriendlyName })
                     .ToList();
        }

        public string GetDefaultOutputDeviceId()
        {
            using var en = new MMDeviceEnumerator();
            return en.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID;
        }

        public string GetDefaultInputDeviceId()
        {
            using var en = new MMDeviceEnumerator();
            return en.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia).ID;
        }

        public string ResolveOutputIdOrDefault(string? wantedId)
        {
            var all = GetOutputDevices();
            if (!string.IsNullOrWhiteSpace(wantedId) && all.Any(d => d.Id == wantedId))
                return wantedId!;
            return GetDefaultOutputDeviceId();
        }

        public string ResolveInputIdOrDefault(string? wantedId)
        {
            var all = GetInputDevices();
            if (!string.IsNullOrWhiteSpace(wantedId) && all.Any(d => d.Id == wantedId))
                return wantedId!;
            return GetDefaultInputDeviceId();
        }
    }
}
