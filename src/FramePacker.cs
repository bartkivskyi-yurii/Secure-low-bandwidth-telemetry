using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TelemetryLink
{
    public class TelemetryFrame
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public int Priority { get; set; }
        public byte[] RawData { get; set; }
        public byte[] CompressedData { get; set; }
        public int Weight => CompressedData?.Length ?? RawData?.Length ?? 0;
    }

    public class FramePacker
    {
        public List<TelemetryFrame> PackKnapsack01(List<TelemetryFrame> frames, int maxCapacity)
        {
            int n = frames.Count;
            int[,] dp = new int[n + 1, maxCapacity + 1];

            for (int i = 1; i <= n; i++)
            {
                int w = frames[i - 1].Weight;
                int v = frames[i - 1].Priority;

                for (int c = 0; c <= maxCapacity; c++)
                {
                    if (w <= c)
                    {
                        dp[i, c] = Math.Max(dp[i - 1, c], dp[i - 1, c - w] + v);
                    }
                    else
                    {
                        dp[i, c] = dp[i - 1, c];
                    }
                }
            }

            List<TelemetryFrame> selectedFrames = new List<TelemetryFrame>();
            int res = dp[n, maxCapacity];
            int currentCap = maxCapacity;

            for (int i = n; i > 0 && res > 0; i--)
            {
                if (res != dp[i - 1, currentCap])
                {
                    TelemetryFrame item = frames[i - 1];
                    selectedFrames.Add(item);
                    res -= item.Priority;
                    currentCap -= item.Weight;
                }
            }

            selectedFrames.Reverse();
            return selectedFrames;
        }

        public byte[] SerializePacket(List<TelemetryFrame> frames)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write(frames.Count);
                foreach (var frame in frames)
                {
                    writer.Write(frame.Id);
                    writer.Write(frame.Type ?? string.Empty);
                    writer.Write(frame.Priority);
                    writer.Write(frame.CompressedData.Length);
                    writer.Write(frame.CompressedData);
                }
                return ms.ToArray();
            }
        }

        public List<TelemetryFrame> DeserializePacket(byte[] packetBytes)
        {
            List<TelemetryFrame> frames = new List<TelemetryFrame>();
            using (MemoryStream ms = new MemoryStream(packetBytes))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    int id = reader.ReadInt32();
                    string type = reader.ReadString();
                    int priority = reader.ReadInt32();
                    int dataLen = reader.ReadInt32();
                    byte[] compData = reader.ReadBytes(dataLen);

                    frames.Add(new TelemetryFrame
                    {
                        Id = id,
                        Type = type,
                        Priority = priority,
                        CompressedData = compData
                    });
                }
            }
            return frames;
        }
    }
}