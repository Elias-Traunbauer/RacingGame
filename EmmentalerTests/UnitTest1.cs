using EmmentalerModel;

namespace EmmentalerTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            Emmentaler emmentaler = new Emmentaler(2, 1, 3);
            Assert.IsNotNull(emmentaler);

            byte[] bytes = emmentaler.ToBinary();

            Emmentaler emmentaler2 = Emmentaler.FromBinary(bytes);

            Assert.IsNotNull(emmentaler2);

            Assert.AreEqual(emmentaler.InputNeuronCount, emmentaler2.InputNeuronCount);

            Assert.AreEqual(emmentaler.OutputNeuronCount, emmentaler2.OutputNeuronCount);

            Assert.AreEqual(emmentaler.HiddenNeuronCounts.Length, emmentaler2.HiddenNeuronCounts.Length);

            for (int i = 0; i < emmentaler.HiddenNeuronCounts.Length; i++)
            {
                Assert.AreEqual(emmentaler.HiddenNeuronCounts[i], emmentaler2.HiddenNeuronCounts[i]);
            }

            Assert.AreEqual(emmentaler.Weights.Length, emmentaler2.Weights.Length);

            for (int i = 0; i < emmentaler.Weights.Length; i++)
            {
                Assert.AreEqual(emmentaler.Weights[i].Length, emmentaler2.Weights[i].Length);

                for (int j = 0; j < emmentaler.Weights[i].Length; j++)
                {
                    Assert.AreEqual(emmentaler.Weights[i][j], emmentaler2.Weights[i][j]);
                }
            }

            Assert.AreEqual(emmentaler.Biases.Length, emmentaler2.Biases.Length);

            for (int i = 0; i < emmentaler.Biases.Length; i++)
            {
                Assert.AreEqual(emmentaler.Biases[i].Length, emmentaler2.Biases[i].Length);

                for (int j = 0; j < emmentaler.Biases[i].Length; j++)
                {
                    Assert.AreEqual(emmentaler.Biases[i][j], emmentaler2.Biases[i][j]);
                }
            }
        }
    }
}