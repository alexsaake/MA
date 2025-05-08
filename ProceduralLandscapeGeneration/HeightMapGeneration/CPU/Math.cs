namespace ProceduralLandscapeGeneration.HeightMapGeneration.CPU;

internal static class Math
{
    public static float Lerp(float lower, float upper, float value)
    {
        return (1 - value) * lower + value * upper;
    }

    public static float InverseLerp(float lower, float upper, float value)
    {
        if (value <= lower)
        {
            return 0;
        }
        if (value >= upper)
        {
            return 1;
        }
        return (value - lower) / (upper - lower);
    }
}
