namespace ProceduralLandscapeGeneration.Simulation
{
    internal class Random : IRandom
    {
        private readonly System.Random myRandom;

        public Random()
        {
            myRandom = new System.Random(Configuration.Seed);
        }

        public int Next(int maxValue)
        {
            return myRandom.Next(maxValue);
        }

        public int Next(int minValue, int maxValue)
        {
            return myRandom.Next(minValue, maxValue);
        }
    }
}
