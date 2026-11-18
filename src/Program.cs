using System;

namespace TelemetryLink
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("TelemetryLink Pipeline Started...");

            var mlp = new MlpLab();

            var codec = new HuffmanCodec();

            var packer = new FramePacker();

            var security = new Security();

        }
    }
}