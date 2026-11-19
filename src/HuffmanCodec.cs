using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

        public byte[] Compress(byte[] data)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            var frequencies = new Dictionary<byte, int>();
            foreach (byte b in data)
            {
                if (!frequencies.ContainsKey(b)) frequencies[b] = 0;
                frequencies[b]++;
            }

            Node root = BuildTree(frequencies);
            var codes = new Dictionary<byte, List<bool>>();
            GenerateCodes(root, new List<bool>(), codes);

            List<bool> encodedBits = new List<bool>();
            foreach (byte b in data)
            {
                encodedBits.AddRange(codes[b]);
            }

            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write(data.Length);
                writer.Write((ushort)frequencies.Count);
                foreach (var kvp in frequencies)
                {
                    writer.Write(kvp.Key);
                    writer.Write(kvp.Value);
                }

                byte[] packedBytes = PackBits(encodedBits);
                writer.Write(packedBytes.Length);
                writer.Write(packedBytes);

                return ms.ToArray();
            }
        }

        public byte[] Decompress(byte[] compressedData)
        {
            if (compressedData == null || compressedData.Length == 0) return Array.Empty<byte>();

            using (MemoryStream ms = new MemoryStream(compressedData))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                int originalLength = reader.ReadInt32();
                ushort dictCount = reader.ReadUInt16();

                var frequencies = new Dictionary<byte, int>();
                for (int i = 0; i < dictCount; i++)
                {
                    byte symbol = reader.ReadByte();
                    int freq = reader.ReadInt32();
                    frequencies[symbol] = freq;
                }

                int packedLength = reader.ReadInt32();
                byte[] packedBytes = reader.ReadBytes(packedLength);

                Node root = BuildTree(frequencies);
                List<byte> decoded = new List<byte>(originalLength);

                if (root.IsLeaf)
                {
                    for (int i = 0; i < originalLength; i++) decoded.Add(root.Symbol);
                    return decoded.ToArray();
                }

                Node current = root;
                BitArray bits = new BitArray(packedBytes);

                for (int i = 0; i < bits.Length && decoded.Count < originalLength; i++)
                {
                    current = bits[i] ? current.Right : current.Left;
                    if (current.IsLeaf)
                    {
                        decoded.Add(current.Symbol);
                        current = root;
                    }
                }

                return decoded.ToArray();
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