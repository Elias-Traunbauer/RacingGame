using EmmentalerModel;
using static GameLogic.IGameAgent;

namespace EmmentalerTrainer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            float trainingRate = 0.01f;
            float trainintIterations = 1000;

            float trainTestSplit = 0.8f;

            string pathToModel = Path.GetDirectoryName(Path.GetDirectoryName(Directory.GetCurrentDirectory()))[0..^3] + "model.emmentaler";
            string pathToTrainingBatch = Path.GetDirectoryName(Path.GetDirectoryName(Directory.GetCurrentDirectory()))[0..^3] + "model.emmentaler";

            Emmentaler emmentaler;

            Console.WriteLine("Looking for emmentaler at \"" + pathToModel + "\"");
            if (File.Exists(pathToModel))
            {
                emmentaler = Emmentaler.FromBinary(File.ReadAllBytes(pathToModel));
                Console.WriteLine("Loaded emmentaler");
            }
            else
            {
                emmentaler = new Emmentaler(21, 5, [40, 30, 20]);
                Console.WriteLine("Created emmentaler");
            }

            Console.WriteLine("Looking for training batch at \"" + pathToTrainingBatch + "\"");

            Experience[] trainingBatch = new Experience[0];

            // TODO: Load training batch

            Console.WriteLine("Loaded training batch");

            Experience[] trainingData = trainingBatch[0..(int)(trainingBatch.Length * trainTestSplit)];
            Experience[] testData = trainingBatch[(int)(trainingData.Length)..];

            Console.WriteLine("Total dataset: " + trainingBatch.Length);
            Console.WriteLine("Train dataset: " + trainingData.Length);
            Console.WriteLine("Test dataset: " + testData.Length);

            Console.WriteLine("Training emmentaler");

            for (int i = 0; i < trainintIterations; i++)
            {
                for (int j = 0; j < trainingData.Length; j++)
                {
                    float[] state = trainingData[j].State;
                    float[] actions = trainingData[j].Actions;

                    emmentaler.Backpropagate(state, actions, trainingRate);
                }
            }

            Console.WriteLine("Testing emmentaler");

            float totalLoss = 0;
            for (int i = 0; i < testData.Length; i++)
            {
                float[] state = testData[i].State;
                float[] actions = testData[i].Actions;

                float loss = emmentaler.Loss(state, actions);
                totalLoss += loss;
            }

            Console.WriteLine("Average loss: " + totalLoss / testData.Length);

            Console.WriteLine("Saving emmentaler to \"" + pathToModel + "\"");
            File.WriteAllBytes(pathToModel, emmentaler.ToBinary());

            Console.WriteLine("Done");
        }
    }
}
