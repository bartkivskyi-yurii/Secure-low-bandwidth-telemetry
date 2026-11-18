using System;
using System.Collections.Generic;
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
    }
}