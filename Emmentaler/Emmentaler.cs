
using System.Diagnostics.CodeAnalysis;

namespace EmmentalerModel
{
    public class Emmentaler
    {
        public Emmentaler(int inputCnt, int outputCnt, params int[] hiddenLayers)
        {
            // Set the number of input, output and hidden neurons
            InputNeuronCount = inputCnt;
            OutputNeuronCount = outputCnt;
            HiddenNeuronCounts = hiddenLayers;

            // Initialize the weights
            InitializeWeights();
        }

        public Emmentaler(Emmentaler emmentaler)
        {
            // Set the number of input, output and hidden neurons
            InputNeuronCount = emmentaler.InputNeuronCount;
            OutputNeuronCount = emmentaler.OutputNeuronCount;
            HiddenNeuronCounts = emmentaler.HiddenNeuronCounts;

            this.Weights = emmentaler.Weights.Select(x => x.ToArray()).ToArray();
            this.Biases = emmentaler.Biases.Select(x => x.ToArray()).ToArray();

            RandomlyAdjustWeights(0.1f);
        }

        public void RandomlyAdjustWeights(float maxAdjustment)
        {
            for (int i = 0; i < Weights.Length; i++)
            {
                for (int j = 0; j < Weights[i].Length; j++)
                {
                    Weights[i][j] += (float)(Random.Shared.NextDouble() * 2 - 1) * maxAdjustment;
                }
            }

            for (int i = 0; i < Biases.Length; i++)
            {
                for (int j = 0; j < Biases[i].Length; j++)
                {
                    Biases[i][j] += (float)(Random.Shared.NextDouble() * 2 - 1) * maxAdjustment / 5;
                }
            }
        }

        [MemberNotNull(nameof(Weights), nameof(Biases))]
        private void InitializeWeights()
        {
            Weights = new float[HiddenNeuronCounts.Length + 1][];

            // Initialize the weights for the hidden layers
            for (int i = 0; i < HiddenNeuronCounts.Length; i++)
            {
                Weights[i] = new float[HiddenNeuronCounts[i] * HiddenNeuronCounts[i]];
                for (int j = 0; j < Weights[i].Length; j++)
                {
                    Weights[i][j] = (float)(Random.Shared.NextDouble() * 2 - 1) / 10f;
                }
            }

            // Initialize the weights for the output layer
            Weights[^1] = new float[HiddenNeuronCounts[^1] * OutputNeuronCount];
            for (int i = 0; i < Weights[^1].Length; i++)
            {
                Weights[^1][i] = (float)(Random.Shared.NextDouble() * 2 - 1) / 10f;
            }

            // Initialize the biases
            Biases = new float[HiddenNeuronCounts.Length + 1][];

            for (int i = 0; i < Biases.Length; i++)
            {
                Biases[i] = new float[i < HiddenNeuronCounts.Length ? HiddenNeuronCounts[i] : OutputNeuronCount];
                for (int j = 0; j < Biases[i].Length; j++)
                {
                    Biases[i][j] = 0;
                }
            }
        }

        public float[] Predict(float[] input)
        {
            // Initialize the neuron values
            float[][] neurons = new float[HiddenNeuronCounts.Length + 2][];
            neurons[0] = new float[InputNeuronCount];
            for (int i = 0; i < InputNeuronCount; i++)
            {
                neurons[0][i] = input[i];
            }

            // Calculate the neuron values for the hidden layers
            for (int i = 0; i < HiddenNeuronCounts.Length; i++)
            {
                neurons[i + 1] = new float[HiddenNeuronCounts[i]];
                for (int j = 0; j < HiddenNeuronCounts[i]; j++)
                {
                    float sum = 0;
                    for (int k = 0; k < neurons[i].Length; k++)
                    {
                        sum += neurons[i][k] * Weights[i][j * neurons[i].Length + k];
                    }
                    neurons[i + 1][j] = Sigmoid(sum + Biases[i][j]);
                }
            }

            // Calculate the neuron values for the output layer
            neurons[^1] = new float[OutputNeuronCount];
            for (int i = 0; i < OutputNeuronCount; i++)
            {
                float sum = 0;
                for (int j = 0; j < neurons[^2].Length; j++)
                {
                    sum += neurons[^2][j] * Weights[^1][i * neurons[^2].Length + j];
                }
                neurons[^1][i] = Sigmoid(sum + Biases[^1][i]);
            }

            return neurons[^1];
        }

        private static float Sigmoid(float v)
        {
            return 1 / (1 + (float)Math.Exp(-v));
        }

        public float[][] Weights { get; private set; }
        public float[][] Biases { get; private set; }

        public int InputNeuronCount { get; }
        public int OutputNeuronCount { get; }
        public int[] HiddenNeuronCounts { get; }
    }
}
