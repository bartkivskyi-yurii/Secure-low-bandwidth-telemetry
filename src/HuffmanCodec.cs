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

        private readonly Node _sharedRoot;
        private readonly Dictionary<byte, List<bool>> _sharedCodes;

        public HuffmanCodec()
        {
            var baseFrequencies = new Dictionary<byte, int>();
            for (int i = 0; i < 256; i++)
            {
                baseFrequencies[(byte)i] = 1;
            }

            string commonChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789:;_-.% ";
            foreach (char c in commonChars)
            {
                baseFrequencies[(byte)c] = 50;
            }
            baseFrequencies[0xAB] = 100;

            _sharedRoot = BuildTree(baseFrequencies);
            _sharedCodes = new Dictionary<byte, List<bool>>();
            GenerateCodes(_sharedRoot, new List<bool>(), _sharedCodes);
        }

        public byte[] Compress(byte[] data)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            List<bool> encodedBits = new List<bool>();
            foreach (byte b in data)
            {
                encodedBits.AddRange(_sharedCodes[b]);
            }

            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write((ushort)data.Length);
                writer.Write(PackBits(encodedBits));
                return ms.ToArray();
            }
        }

        public byte[] Decompress(byte[] compressedData)
        {
            if (compressedData == null || compressedData.Length == 0) return Array.Empty<byte>();

            using (MemoryStream ms = new MemoryStream(compressedData))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                ushort originalLength = reader.ReadUInt16();
                byte[] packedBytes = reader.ReadBytes((int)(ms.Length - ms.Position));

                List<byte> decoded = new List<byte>(originalLength);
                Node current = _sharedRoot;
                BitArray bits = new BitArray(packedBytes);

                for (int i = 0; i < bits.Length && decoded.Count < originalLength; i++)
                {
                    current = bits[i] ? current.Right : current.Left;
                    if (current.IsLeaf)
                    {
                        decoded.Add(current.Symbol);
                        current = _sharedRoot;
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

        public double GetCompressionRatio(int originalSize, int compressedSize)
        {
            if (originalSize == 0) return 0;
            return Math.Round((double)compressedSize / originalSize, 3);
        }
    }
}