using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FlashTelemetry.Profiles.F124
{
    public sealed class F124UdpReceiver : IDisposable
    {
        private readonly int _port;
        private readonly F124State _state;
        private readonly Action<string> _log;

        private UdpClient? _udp;
        private CancellationTokenSource? _cts;
        private Task? _task;

        private long _rxCount;
        private bool _loggedFirst;

        public long ReceivedPackets => Interlocked.Read(ref _rxCount);

        public F124UdpReceiver(int port, F124State state, Action<string> log)
        {
            _port = port;
            _state = state;
            _log = log;
        }

        public void Start()
        {
            if (_task != null) return;

            _cts = new CancellationTokenSource();
            _udp = new UdpClient(new IPEndPoint(IPAddress.Any, _port));
            _udp.Client.ReceiveBufferSize = 4 * 1024 * 1024;

            _log($"[F1] Escuchando UDP en 0.0.0.0:{_port}");

            _task = Task.Run(() => Loop(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
            try { _udp?.Close(); } catch { }
            _udp?.Dispose();

            _udp = null;
            _cts?.Dispose();
            _cts = null;
            _task = null;

            _loggedFirst = false;
        }

        private async Task Loop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _udp != null)
            {
                UdpReceiveResult res;
                try
                {
                    res = await _udp.ReceiveAsync(ct);
                }
                catch (OperationCanceledException) { break; }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex)
                {
                    _log($"[F1] RX error: {ex.Message}");
                    try { await Task.Delay(200, ct); } catch { }
                    continue;
                }

                Interlocked.Increment(ref _rxCount);

                if (!_loggedFirst)
                {
                    _loggedFirst = true;
                    _log($"[F1] Primer paquete recibido desde {res.RemoteEndPoint.Address}:{res.RemoteEndPoint.Port} (len={res.Buffer.Length})");
                }

                Parse(res.Buffer);
            }
        }

        private void Parse(byte[] buf)
        {
            // Header 2024 = 29 bytes
            if (buf.Length < 29) return;

            ReadOnlySpan<byte> s = buf;
            int o = 0;

            ushort packetFormat = ReadU16(s, ref o); // 2024
            _ = ReadU8(s, ref o); // gameYear
            _ = ReadU8(s, ref o); // gameMajorVersion
            _ = ReadU8(s, ref o); // gameMinorVersion
            _ = ReadU8(s, ref o); // packetVersion
            byte packetId = ReadU8(s, ref o);
            _ = ReadU64(s, ref o); // sessionUID
            _ = ReadF32(s, ref o); // sessionTime
            _ = ReadU32(s, ref o); // frameIdentifier
            _ = ReadU32(s, ref o); // overallFrameIdentifier
            byte playerCarIndex = ReadU8(s, ref o);
            _ = ReadU8(s, ref o); // secondaryPlayerCarIndex

            if (packetFormat != 2024 || playerCarIndex >= 22) return;

            const int headerSize = 29;

            switch (packetId)
            {
                case 6:
                    ParseCarTelemetry(s, headerSize, playerCarIndex);
                    break;

                case 7:
                    ParseCarStatus(s, headerSize, playerCarIndex);
                    break;

                default:
                    break;
            }
        }

        private void ParseCarTelemetry(ReadOnlySpan<byte> s, int headerSize, int carIndex)
        {
            // CarTelemetryData = 60 bytes
            const int carSize = 60;

            int o = headerSize + carIndex * carSize;
            if (o + carSize > s.Length) return;

            ushort speedKph = ReadU16(s, ref o);
            _ = ReadF32(s, ref o); // throttle
            _ = ReadF32(s, ref o); // steer
            _ = ReadF32(s, ref o); // brake
            _ = ReadU8(s, ref o);  // clutch
            sbyte gear = ReadI8(s, ref o);
            ushort rpm = ReadU16(s, ref o);
            byte drs = ReadU8(s, ref o);

            _state.UpdateTelemetry(speedKph: speedKph, rpm: rpm, gear: gear, drs: drs);
        }

        private void ParseCarStatus(ReadOnlySpan<byte> s, int headerSize, int carIndex)
        {
            // CarStatusData = 55 bytes
            const int carSize = 55;

            int o = headerSize + carIndex * carSize;
            if (o + carSize > s.Length) return;

            _ = ReadU8(s, ref o);  // tractionControl
            _ = ReadU8(s, ref o);  // antiLockBrakes
            _ = ReadU8(s, ref o);  // fuelMix
            _ = ReadU8(s, ref o);  // frontBrakeBias
            _ = ReadU8(s, ref o);  // pitLimiterStatus
            _ = ReadF32(s, ref o); // fuelInTank
            _ = ReadF32(s, ref o); // fuelCapacity
            _ = ReadF32(s, ref o); // fuelRemainingLaps
            _ = ReadU16(s, ref o); // maxRPM
            _ = ReadU16(s, ref o); // idleRPM
            _ = ReadU8(s, ref o);  // maxGears
            _ = ReadU8(s, ref o);  // drsAllowed
            _ = ReadU16(s, ref o); // drsActivationDistance
            _ = ReadU8(s, ref o);  // actualTyreCompound
            _ = ReadU8(s, ref o);  // visualTyreCompound
            _ = ReadU8(s, ref o);  // tyresAgeLaps
            _ = ReadI8(s, ref o);  // vehicleFiaFlags
            _ = ReadF32(s, ref o); // enginePowerICE
            _ = ReadF32(s, ref o); // enginePowerMGUK

            float ersStoreEnergyJ = ReadF32(s, ref o);
            byte ersDeployMode = ReadU8(s, ref o);

            // Aproximación: energía máx ~4MJ -> 100%
            float ratio = ersStoreEnergyJ / 4_000_000f;
            ratio = Math.Clamp(ratio, 0f, 1f); // <-- FIX: MathF.Clamp no existe, usamos Math.Clamp
            int ersPct = (int)MathF.Round(ratio * 100f);

            _state.UpdateErs(ersMode: ersDeployMode, ersPct: ersPct);
        }

        private static byte ReadU8(ReadOnlySpan<byte> s, ref int o) => s[o++];

        private static sbyte ReadI8(ReadOnlySpan<byte> s, ref int o) => unchecked((sbyte)s[o++]);

        private static ushort ReadU16(ReadOnlySpan<byte> s, ref int o)
        {
            ushort v = BinaryPrimitives.ReadUInt16LittleEndian(s.Slice(o, 2));
            o += 2;
            return v;
        }

        private static uint ReadU32(ReadOnlySpan<byte> s, ref int o)
        {
            uint v = BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(o, 4));
            o += 4;
            return v;
        }

        private static ulong ReadU64(ReadOnlySpan<byte> s, ref int o)
        {
            ulong v = BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(o, 8));
            o += 8;
            return v;
        }

        private static float ReadF32(ReadOnlySpan<byte> s, ref int o)
        {
            int i = BinaryPrimitives.ReadInt32LittleEndian(s.Slice(o, 4));
            o += 4;
            return BitConverter.Int32BitsToSingle(i);
        }

        public void Dispose() => Stop();
    }
}
