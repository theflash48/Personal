namespace FlashTelemetry.Core.Radio
{
    public sealed class AudioDeviceInfo
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";

        public override string ToString() => Name;
    }
}
