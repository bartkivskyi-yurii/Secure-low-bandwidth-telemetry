using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TelemetryLink
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            var codec = new HuffmanCodec();
            var packer = new FramePacker();
            var security = new Security();

            byte[] aesKey = new byte[32];
            byte[] hmacKey = new byte[32];
            RandomNumberGenerator.Fill(aesKey);
            RandomNumberGenerator.Fill(hmacKey);

            int maxCapacity = 300;

            var telemetryQueue = new List<TelemetryFrame>
            {
                new TelemetryFrame { Id = 1, Type = "CRITICAL_ALERT", Priority = 100, RawData = Encoding.UTF8.GetBytes("TARGET_COORDINATES:50.4501,30.5234;ALT:120m") },
                new TelemetryFrame { Id = 2, Type = "BATTERY_STATUS", Priority = 75, RawData = Encoding.UTF8.GetBytes("BAT:78%;TEMP:45C;VOLTAGE:22.4V") },
                new TelemetryFrame { Id = 3, Type = "ROUTINE_LOG", Priority = 30, RawData = Encoding.UTF8.GetBytes("LOG_ENTRY_00492:STABLE_MODE_ACTIVE") }
            };

            foreach (var frame in telemetryQueue)
            {
                frame.CompressedData = codec.Compress(frame.RawData);
                Console.WriteLine($"Кадр #{frame.Id}: {frame.RawData.Length} Б -> {frame.Weight} Б");
            }

            List<TelemetryFrame> selected = packer.PackKnapsack01(telemetryQueue, maxCapacity);
            byte[] rawPayload = selected.SelectMany(f => f.CompressedData).ToArray();
            byte[] securePacket = security.EncryptAndSign(rawPayload, aesKey, hmacKey);

            Console.WriteLine($"Відібрано кадрів: {selected.Count}. Зашифрований пакет: {securePacket.Length} Б.");
        }
    }
}