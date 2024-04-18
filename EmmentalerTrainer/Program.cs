using EmmentalerModel;
using GameLogic;
using static GameLogic.IGameAgent;
using RacingGameAIWithBoost;

namespace EmmentalerTrainer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            float trainingRate = 0.01f;
            float trainintIterations = 1000;

            float trainTestSplit = 0.9f;

            string pathToModel = Path.GetDirectoryName(Path.GetDirectoryName(Directory.GetCurrentDirectory()))[0..^3] + "model.emmentaler";
            string pathToTrainingBatch = Path.GetDirectoryName(Path.GetDirectoryName(Directory.GetCurrentDirectory()))[0..^3]/* + "training.data"*/;
            string[] files = Directory.GetFiles(pathToTrainingBatch);

            Emmentaler emmentaler;

            Console.WriteLine("Looking for emmentaler at \"" + pathToModel + "\"");
            if (File.Exists(pathToModel))
            {
                emmentaler = Emmentaler.FromBinary(File.ReadAllBytes(pathToModel));
                Console.WriteLine("Loaded emmentaler");
            }
            else
            {
                emmentaler = new Emmentaler(21, 5, [35, 25]);
                Console.WriteLine("Created emmentaler");
            }

            Console.WriteLine("Looking for training batch at \"" + pathToTrainingBatch + "\"");

            Experience[] trainingBatch = [];
            foreach (var item in files)
            {
                if (item.EndsWith(".data"))
                {
                    if (File.Exists(item))
                    {
                        trainingBatch = [..trainingBatch, ..RacingGameAIWithBoost.GameAgent.LoadTrainingBatch(File.ReadAllBytes(item))];
                        Console.WriteLine("Loaded training batch " + item);
                    }
                    else
                    {
                        Console.WriteLine("No training batch found");
                    }
                }
            }
            int cntBefore = trainingBatch.Length;
            trainingBatch = trainingBatch.Where(x => x.Actions.Any(y => y != 0)).ToArray();
            Console.WriteLine("Removed " + (cntBefore - trainingBatch.Length) + " zero actions");

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
                Console.WriteLine("Iteration " + i + "/" + trainintIterations);
                for (int j = 0; j < trainingData.Length; j++)
                {
                    float[] state = trainingData[j].State;
                    float[] actions = trainingData[j].Actions;

                    emmentaler.Backpropagate(state, actions, trainingRate);
                }
                // loss update

                float totalLosss = 0;
                for (int j = 0; j < testData.Length; j++)
                {
                    float[] state = testData[j].State;
                    float[] actions = testData[j].Actions;

                    float loss = emmentaler.Loss(state, actions);
                    totalLosss += loss;
                }

                Console.WriteLine("Average loss: " + totalLosss / testData.Length);
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
