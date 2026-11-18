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

        public int PredictState(double[] features)
        {
            return 0;
        }
    }
}