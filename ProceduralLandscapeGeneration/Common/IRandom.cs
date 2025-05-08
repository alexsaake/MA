namespace ProceduralLandscapeGeneration.Common;

internal interface IRandom
{
    int Next(int maxValue);
    int Next(int minValue, int maxValue);

    float NextFloat();
    float NextFloat(float minValue, float maxValue);
}