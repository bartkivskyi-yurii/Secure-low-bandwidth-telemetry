using System;

namespace TelemetryLink
{
    public class MlpLab
    {
        private readonly double[,] _weightsInputHidden = {
            { 0.45, -0.32, 0.78 },
            { -0.15, 0.85, -0.40 },
            { 0.60, 0.25, -0.65 }
        };

        private readonly double[] _weightsHiddenOutput = { 0.70, -0.55, 0.80 };
        private readonly double[] _biasHidden = { 0.1, -0.2, 0.05 };
        private readonly double _biasOutput = -0.15;

        private double Sigmoid(double x) => 1.0 / (1.0 + Math.Exp(-x));

        public int PredictChannelCapacity(double signalStrength, double interferenceLevel, double packetLossRate, int baseMaxCapacity)
        {
            double[] inputs = { signalStrength, interferenceLevel, packetLossRate };
            double[] hidden = new double[3];

            for (int j = 0; j < 3; j++)
            {
                double sum = _biasHidden[j];
                for (int i = 0; i < 3; i++)
                {
                    sum += inputs[i] * _weightsInputHidden[i, j];
                }
                hidden[j] = Sigmoid(sum);
            }

            double outputSum = _biasOutput;
            for (int j = 0; j < 3; j++)
            {
                outputSum += hidden[j] * _weightsHiddenOutput[j];
            }

            double channelQualityScore = Sigmoid(outputSum);

            if (channelQualityScore > 0.65) return baseMaxCapacity;
            if (channelQualityScore > 0.35) return (int)(baseMaxCapacity * 0.65);
            return (int)(baseMaxCapacity * 0.35);
        }
        
        public void AdaptWeights(double learningRate, double error)
        {
            for (int j = 0; j < 3; j++)
            {
                _weightsHiddenOutput[j] += learningRate * error;
            }
        }
    }
}