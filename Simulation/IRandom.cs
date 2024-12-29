namespace ProceduralLandscapeGeneration.Simulation
{
    internal interface IRandom
    {
        int Next(int maxValue);
        int Next(int minValue, int maxValue);
    }
}