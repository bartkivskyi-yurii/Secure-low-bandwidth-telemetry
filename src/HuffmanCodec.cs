using System;
using System.Collections.Generic;
using System.Linq;

namespace TelemetryLink
{
    public class HuffmanCodec
    {
        private class Node : IComparable<Node>
        {
            public byte Symbol { get; set; }
            public int Frequency { get; set; }
            public Node Left { get; set; }
            public Node Right { get; set; }

            public bool IsLeaf => Left == null && Right == null;

            public int CompareTo(Node other)
            {
                return Frequency.CompareTo(other.Frequency);
            }
        }

        private Node BuildTree(Dictionary<byte, int> frequencies)
        {
            var nodes = frequencies.Select(kvp => new Node { Symbol = kvp.Key, Frequency = kvp.Value }).ToList();
            while (nodes.Count > 1)
            {
                nodes.Sort();
                Node left = nodes[0];
                Node right = nodes[1];
                nodes.RemoveRange(0, 2);

                Node parent = new Node
                {
                    Frequency = left.Frequency + right.Frequency,
                    Left = left,
                    Right = right
                };
                nodes.Add(parent);
            }
            return nodes[0];
        }

        private void GenerateCodes(Node node, List<bool> currentPath, Dictionary<byte, List<bool>> codes)
        {
            if (node == null) return;
            if (node.IsLeaf)
            {
                codes[node.Symbol] = currentPath.Count > 0 ? new List<bool>(currentPath) : new List<bool> { false };
                return;
            }
            currentPath.Add(false);
            GenerateCodes(node.Left, currentPath, codes);
            currentPath.RemoveAt(currentPath.Count - 1);

            currentPath.Add(true);
            GenerateCodes(node.Right, currentPath, codes);
            currentPath.RemoveAt(currentPath.Count - 1);
        }

        private byte[] PackBits(List<bool> bits)
        {
            int byteCount = (bits.Count + 7) / 8;
            byte[] bytes = new byte[byteCount];
            for (int i = 0; i < bits.Count; i++)
            {
                if (bits[i])
                {
                    bytes[i / 8] |= (byte)(1 << (i % 8));
                }
            }
            return bytes;
        }
    }
}