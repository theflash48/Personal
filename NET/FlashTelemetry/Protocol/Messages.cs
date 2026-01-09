using System.Net;

namespace FlashTelemetry.Protocol
{
    public readonly record struct HelloMessage(
        IPEndPoint Remote,
        int SelectedProfileId,
        int Proto,
        string DeviceId,
        string Token
    );

    public readonly record struct SetProfileMessage(
        IPEndPoint Remote,
        int ProfileId,
        string Token
    );

    public readonly record struct PingMessage(
        IPEndPoint Remote,
        int Seq
    );

    // NUEVO
    public readonly record struct PttToggleMessage(
        IPEndPoint Remote,
        string Token
    );
}
