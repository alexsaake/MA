using ProceduralLandscapeGeneration.Config;

namespace ProceduralLandscapeGeneration.Simulation;

internal class Random : IRandom
{
    private readonly System.Random myRandom;

    public Random(IMapGenerationConfiguration mapGenerationConfiguration)
    {
        myRandom = new System.Random(mapGenerationConfiguration.Seed);
    }

    public int Next(int maxValue)
    {
        return myRandom.Next(maxValue);
    }

    public int Next(int minValue, int maxValue)
    {
        return myRandom.Next(minValue, maxValue);
    }

    public float NextFloat()
    {
        return myRandom.NextSingle();
    }

    public float NextFloat(float minValue, float maxValue)
    {
        return minValue + myRandom.NextSingle() * (maxValue - minValue);
    }
}
