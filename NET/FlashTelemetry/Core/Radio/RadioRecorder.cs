using System;
using System.IO;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace FlashTelemetry.Core.Radio
{
    public sealed class RadioRecorder : IDisposable
    {
        private WasapiCapture? _cap;
        private WaveFileWriter? _writer;

        public bool IsRecording => _cap != null;

        public void Start(string inputDeviceId, string wavPath)
        {
            Stop();

            Directory.CreateDirectory(Path.GetDirectoryName(wavPath)!);

            var en = new MMDeviceEnumerator();
            var dev = en.GetDevice(inputDeviceId);

            _cap = new WasapiCapture(dev);
            _writer = new WaveFileWriter(wavPath, _cap.WaveFormat);

            _cap.DataAvailable += (_, e) =>
            {
                _writer?.Write(e.Buffer, 0, e.BytesRecorded);
                _writer?.Flush();
            };

            _cap.RecordingStopped += (_, __) =>
            {
                _writer?.Dispose();
                _writer = null;
                _cap?.Dispose();
                _cap = null;
            };

            _cap.StartRecording();
        }

        public void Stop()
        {
            if (_cap == null) return;
            _cap.StopRecording();
        }

        // Compatibilidad si algún sitio llama StartAsync/StopAsync
        public Task StartAsync(string inputDeviceId, string wavPath) { Start(inputDeviceId, wavPath); return Task.CompletedTask; }
        public Task StopAsync() { Stop(); return Task.CompletedTask; }

        public void Dispose()
        {
            Stop();
            _writer?.Dispose();
            _cap?.Dispose();
        }
    }
}
