namespace FlashTelemetry.Profiles
{
    public interface IProfileModule
    {
        int ProfileId { get; }
        string Name { get; }

        void Start();
        void Stop();

        /// <summary>
        /// Construye el payload "DATA ..." listo para enviar al ESP32.
        /// Debe devolver false si no hay datos válidos/frescos.
        /// </summary>
        bool TryBuildDataPayload(uint seq, out string payload);
    }
}
