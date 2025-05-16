using System.Numerics;

namespace ProceduralLandscapeGeneration.HeightMapGeneration.PlateTectonics;

internal interface IPoissonDiskSampler
{
    List<Vector2> GeneratePoints(float radius, uint heightMapSideLength, int samplesBeforeRejection = 30);
}