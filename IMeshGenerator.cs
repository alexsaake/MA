using Raylib_CsLo;
using System.Numerics;

namespace ProceduralLandscapeGeneration
{
    internal interface IMeshGenerator
    {
        Dictionary<Vector3, Mesh> GenerateChunkMeshes(HeightMap heightMap);
    }
}