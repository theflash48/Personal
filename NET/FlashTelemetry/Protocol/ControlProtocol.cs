namespace FlashTelemetry.Protocol
{
    public static class ControlProtocol
    {
        public static string BuildHelloAck(int activeProfileId)
            => $"HELLO_ACK active={activeProfileId} proto=1";

        public static string BuildProfileAck(int activeProfileId)
            => $"PROFILE_ACK active={activeProfileId}";

        public static string BuildPong(int seq)
            => $"PONG seq={seq}";
    }
}
