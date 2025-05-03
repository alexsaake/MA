using System.Numerics;

namespace ProceduralLandscapeGeneration.Common;

internal interface IPoissonDiskSampler
{
    List<Vector2> GeneratePoints(float radius, uint heightMapSideLength, int samplesBeforeRejection = 30);
}