using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            Stopwatch sw = Stopwatch.StartNew();

            var mlp = new MlpLab();
            var codec = new HuffmanCodec();
            var packer = new FramePacker();
            var security = new Security();

            byte[] aesKey = new byte[32];
            byte[] hmacKey = new byte[32];
            RandomNumberGenerator.Fill(aesKey);
            RandomNumberGenerator.Fill(hmacKey);

            int baseCapacity = 450;
            mlp.AdaptWeights(0.01, 0.05);
            int dynamicCapacity = mlp.PredictChannelCapacity(0.75, 0.30, 0.05, baseCapacity);

            Console.WriteLine($"Прогнозована пропускна здатність каналу: {dynamicCapacity} Б (базова: {baseCapacity} Б)\n");

            var telemetryQueue = new List<TelemetryFrame>
            {
                new TelemetryFrame { Id = 1, Type = "CRITICAL_ALERT_TARGET_LOCK", Priority = 100, RawData = Encoding.UTF8.GetBytes("TARGET_COORDINATES:50.4501,30.5234;ALT:120m;STATUS:LOCKED_ON_TARGET_CONFIRMED") },
                new TelemetryFrame { Id = 2, Type = "BATTERY_AND_MOTORS", Priority = 75, RawData = Encoding.UTF8.GetBytes("BAT:78%;TEMP_M1:45C;TEMP_M2:46C;TEMP_M3:44C;TEMP_M4:45C;VOLTAGE:22.4V") },
                new TelemetryFrame { Id = 3, Type = "VIDEO_KEYFRAME_CHUNK", Priority = 60, RawData = Enumerable.Repeat((byte)0xAB, 250).ToArray() },
                new TelemetryFrame { Id = 4, Type = "ROUTINE_GPS_LOG", Priority = 30, RawData = Encoding.UTF8.GetBytes("LOG_ENTRY_00492:WIND_SPEED_4MS_NORTH_WEST_STABLE_FLIGHT_MODE_ACTIVE") },
                new TelemetryFrame { Id = 5, Type = "DEBUG_DIAGNOSTICS_DUMP", Priority = 10, RawData = Encoding.UTF8.GetBytes("SYS_MEM_ALLOC_OK_THREAD_POOL_IDLE_GC_GEN0_CLEAN_NO_WARNINGS_FOUND_IN_BUFFER") }
            };

            Console.WriteLine("Етап 1. Стиснення вхідних кадрів (алгоритм Гаффмана):");
            int totalRawBytes = 0;
            int totalCompressedBytes = 0;

            foreach (var frame in telemetryQueue)
            {
                frame.CompressedData = codec.Compress(frame.RawData);
                totalRawBytes += frame.RawData.Length;
                totalCompressedBytes += frame.Weight;
                double ratio = codec.GetCompressionRatio(frame.RawData.Length, frame.Weight);
                Console.WriteLine($"  Кадр #{frame.Id} [{frame.Type}] | Пріоритет: {frame.Priority} | {frame.RawData.Length} Б -> {frame.Weight} Б (Коеф: {ratio})");
            }

            Console.WriteLine("\nЕтап 2. Оптимізація пакування за пріоритетом та вагою (Рюкзак 0/1):");
            List<TelemetryFrame> selectedFrames = packer.PackKnapsack01(telemetryQueue, dynamicCapacity);
            int totalPriority = selectedFrames.Sum(f => f.Priority);
            int totalWeight = selectedFrames.Sum(f => f.Weight);
            double utilization = packer.CalculateUtilization(selectedFrames, dynamicCapacity);

            Console.WriteLine($"  Відібрано кадрів: {selectedFrames.Count}/{telemetryQueue.Count} | Сумарний пріоритет: {totalPriority} | Заповнення каналу: {totalWeight}/{dynamicCapacity} Б ({utilization}%)");
            foreach (var f in selectedFrames)
            {
                Console.WriteLine($"   -> Включено: #{f.Id} ({f.Type})");
            }

            byte[] serializedPayload = packer.SerializePacket(selectedFrames);

            Console.WriteLine("\nЕтап 3. Криптографічний захист (AES-256 + HMAC-SHA256):");
            byte[] securePacket = security.EncryptAndSign(serializedPayload, aesKey, hmacKey);
            Console.WriteLine($"  Сформовано захищений пакет: {securePacket.Length} Б\n");

            Console.WriteLine("Етап 4. Перевірка стійкості до модифікації даних у каналі:");
            byte[] corruptedPacket = security.SimulateMitmAttack(securePacket);
            try
            {
                security.VerifyAndDecrypt(corruptedPacket, aesKey, hmacKey);
            }
            catch (CryptographicException ex)
            {
                Console.WriteLine($"  Перевірка цілісності спрацювала коректно: {ex.Message}\n");
            }

            Console.WriteLine("Етап 5. Прийом, верифікація та відновлення вихідних даних:");
            byte[] decryptedPayload = security.VerifyAndDecrypt(securePacket, aesKey, hmacKey);
            Console.WriteLine("  1. Підпис HMAC підтверджено, корисне навантаження розшифровано.");

            List<TelemetryFrame> unpackedFrames = packer.DeserializePacket(decryptedPayload);
            Console.WriteLine($"  2. Десеріалізовано кадрів: {unpackedFrames.Count}");

            Console.WriteLine("  3. Декомпресія та перевірка вмісту:");
            foreach (var frame in unpackedFrames)
            {
                byte[] restoredData = codec.Decompress(frame.CompressedData);
                string contentPreview = frame.Type == "VIDEO_KEYFRAME_CHUNK" 
                    ? $"[Бінарний масив: {restoredData.Length} Б, заголовок: 0x{restoredData[0]:X2}]" 
                    : Encoding.UTF8.GetString(restoredData);

                Console.WriteLine($"     Кадр #{frame.Id} [{frame.Type}] (Пріоритет {frame.Priority}): {contentPreview}");
            }

            sw.Stop();
            double overallCompression = codec.GetCompressionRatio(totalRawBytes, totalCompressedBytes);
            Console.WriteLine($"\nПідсумкові метрики: загальний коеф. стиснення = {overallCompression}, час виконання = {sw.ElapsedMilliseconds} мс.");
        }
    }
}