
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

            RandomlyAdjustWeights(0.05f);
        }

        private Emmentaler()
        {

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
                    Biases[i][j] += (float)(Random.Shared.NextDouble() * 2 - 1) * maxAdjustment;
                }
            }
        }

        [MemberNotNull(nameof(Weights), nameof(Biases))]
        private void InitializeWeights()
        {
            Weights = new float[HiddenNeuronCounts.Length + 1][];

            // initialize the weights for the input layer
            Weights[0] = new float[InputNeuronCount * HiddenNeuronCounts[0]];
            for (int i = 0; i < Weights[0].Length; i++)
            {
                Weights[0][i] = (float)(Random.Shared.NextDouble() * 2 - 1) / 5f;
            }

            // Initialize the weights for the hidden layers
            for (int i = 1; i < HiddenNeuronCounts.Length; i++)
            {
                Weights[i] = new float[HiddenNeuronCounts[i] * HiddenNeuronCounts[i - 1]];
                for (int j = 0; j < Weights[i].Length; j++)
                {
                    Weights[i][j] = (float)(Random.Shared.NextDouble() * 2 - 1) / 5f;
                }
            }

            // Initialize the weights for the output layer
            Weights[^1] = new float[HiddenNeuronCounts[^1] * OutputNeuronCount];
            for (int i = 0; i < Weights[^1].Length; i++)
            {
                Weights[^1][i] = (float)(Random.Shared.NextDouble() * 2 - 1) / 5f;
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

        public float[][] lastNeurons = [];

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
            lastNeurons = neurons.ToArray();
            return neurons[^1];
        }

        private static float Sigmoid(float v)
        {
            return 1 / (1 + (float)Math.Exp(-v));
        }

        public float[][] Weights { get; private set; }
        public float[][] Biases { get; private set; }

        public int InputNeuronCount { get; private set; }
        public int OutputNeuronCount { get; private set; }
        public int[] HiddenNeuronCounts { get; private set; }

        public byte[] ToBinary()
        {
            byte[] result = new byte[Weights.SelectMany(x => x).Count() * sizeof(float) + Biases.SelectMany(x => x).Count() * sizeof(float) + sizeof(int) * 3 + HiddenNeuronCounts.Count() * sizeof(int)];

            int offset = 0;

            Buffer.BlockCopy(BitConverter.GetBytes(InputNeuronCount), 0, result, offset, sizeof(int));
            offset += sizeof(int);

            Buffer.BlockCopy(BitConverter.GetBytes(OutputNeuronCount), 0, result, offset, sizeof(int));
            offset += sizeof(int);

            Buffer.BlockCopy(BitConverter.GetBytes(HiddenNeuronCounts.Length), 0, result, offset, sizeof(int));
            offset += sizeof(int);

            for (int i = 0; i < HiddenNeuronCounts.Length; i++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(HiddenNeuronCounts[i]), 0, result, offset, sizeof(int));
                offset += sizeof(int);
            }

            for (int i = 0; i < Weights.Length; i++)
            {
                for (int j = 0; j < Weights[i].Length; j++)
                {
                    Buffer.BlockCopy(BitConverter.GetBytes(Weights[i][j]), 0, result, offset, sizeof(float));
                    offset += sizeof(float);
                }
            }

            for (int i = 0; i < Biases.Length; i++)
            {
                for (int j = 0; j < Biases[i].Length; j++)
                {
                    Buffer.BlockCopy(BitConverter.GetBytes(Biases[i][j]), 0, result, offset, sizeof(float));
                    offset += sizeof(float);
                }
            }

            return result;
        }

        public static Emmentaler FromBinary(byte[] bytes)
        {
            Emmentaler emmentaler = new Emmentaler();

            int offset = 0;

            emmentaler.InputNeuronCount = BitConverter.ToInt32(bytes, offset);
            offset += sizeof(int);

            emmentaler.OutputNeuronCount = BitConverter.ToInt32(bytes, offset);
            offset += sizeof(int);

            int hiddenLayerCount = BitConverter.ToInt32(bytes, offset);
            offset += sizeof(int);

            emmentaler.HiddenNeuronCounts = new int[hiddenLayerCount];

            for (int i = 0; i < hiddenLayerCount; i++)
            {
                emmentaler.HiddenNeuronCounts[i] = BitConverter.ToInt32(bytes, offset);
                offset += sizeof(int);
            }

            emmentaler.Weights = new float[hiddenLayerCount + 1][];

            emmentaler.Weights[0] = new float[emmentaler.InputNeuronCount * emmentaler.HiddenNeuronCounts[0]];
            for (int i = 0; i < emmentaler.Weights[0].Length; i++)
            {
                emmentaler.Weights[0][i] = BitConverter.ToSingle(bytes, offset);
                offset += sizeof(float);
            }

            for (int i = 1; i < hiddenLayerCount; i++)
            {
                emmentaler.Weights[i] = new float[emmentaler.HiddenNeuronCounts[i] * emmentaler.HiddenNeuronCounts[i - 1]];
                for (int j = 0; j < emmentaler.Weights[i].Length; j++)
                {
                    emmentaler.Weights[i][j] = BitConverter.ToSingle(bytes, offset);
                    offset += sizeof(float);
                }
            }

            emmentaler.Weights[^1] = new float[emmentaler.HiddenNeuronCounts[^1] * emmentaler.OutputNeuronCount];
            for (int i = 0; i < emmentaler.Weights[^1].Length; i++)
            {
                emmentaler.Weights[^1][i] = BitConverter.ToSingle(bytes, offset);
                offset += sizeof(float);
            }

            emmentaler.Biases = new float[hiddenLayerCount + 1][];

            for (int i = 0; i < hiddenLayerCount + 1; i++)
            {
                emmentaler.Biases[i] = new float[i < hiddenLayerCount ? emmentaler.HiddenNeuronCounts[i] : emmentaler.OutputNeuronCount];
                for (int j = 0; j < emmentaler.Biases[i].Length; j++)
                {
                    emmentaler.Biases[i][j] = BitConverter.ToSingle(bytes, offset);
                    offset += sizeof(float);
                }
            }

            return emmentaler;
        }

        public void Backpropagate(float[] inputs, float[] targets, float learningRate)
        {
            // First, run the forward pass and store all neuron outputs
            float[][] neuronOutputs = ForwardPass(inputs);

            // Calculate output errors for the last layer
            int numLayers = HiddenNeuronCounts.Length + 1;
            float[][] errors = new float[numLayers + 1][];
            errors[numLayers] = new float[OutputNeuronCount];
            for (int i = 0; i < OutputNeuronCount; i++)
            {
                float output = neuronOutputs[numLayers][i];
                errors[numLayers][i] = (output - targets[i]) * output * (1 - output);
            }

            // Backpropagate the errors to compute gradients for each layer
            for (int layer = numLayers - 1; layer >= 0; layer--)
            {
                int currentLayerSize = layer == numLayers - 1 ? OutputNeuronCount : HiddenNeuronCounts[layer];
                int prevLayerSize = layer == 0 ? InputNeuronCount : HiddenNeuronCounts[layer - 1];
                errors[layer] = new float[prevLayerSize];

                for (int j = 0; j < prevLayerSize; j++)
                {
                    float error = 0;
                    for (int k = 0; k < currentLayerSize; k++)
                    {
                        error += Weights[layer][k * prevLayerSize + j] * errors[layer + 1][k];
                        // Update weights
                        Weights[layer][k * prevLayerSize + j] -= learningRate * errors[layer + 1][k] * neuronOutputs[layer][j];
                    }
                    errors[layer][j] = error * neuronOutputs[layer][j] * (1 - neuronOutputs[layer][j]);
                }

                // Update biases for the current layer
                for (int j = 0; j < currentLayerSize; j++)
                {
                    Biases[layer][j] -= learningRate * errors[layer + 1][j];
                }
            }
        }

        private float[][] ForwardPass(float[] inputs)
        {
            int numLayers = HiddenNeuronCounts.Length + 1;
            float[][] neuronOutputs = new float[numLayers + 1][];
            neuronOutputs[0] = inputs;

            for (int layer = 0; layer < numLayers; layer++)
            {
                int layerSize = layer == numLayers - 1 ? OutputNeuronCount : HiddenNeuronCounts[layer];
                int prevLayerSize = layer == 0 ? InputNeuronCount : HiddenNeuronCounts[layer - 1];
                neuronOutputs[layer + 1] = new float[layerSize];

                for (int neuron = 0; neuron < layerSize; neuron++)
                {
                    float sum = Biases[layer][neuron];
                    for (int prevNeuron = 0; prevNeuron < prevLayerSize; prevNeuron++)
                    {
                        sum += neuronOutputs[layer][prevNeuron] * Weights[layer][neuron * prevLayerSize + prevNeuron];
                    }
                    neuronOutputs[layer + 1][neuron] = Sigmoid(sum);
                }
            }

            return neuronOutputs;
        }
    }
}
