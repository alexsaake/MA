using Raylib_CsLo;

namespace ProceduralLandscapeGeneration
{
    internal interface IMeshGenerator
    {
        Mesh GenerateTerrainMesh(HeightMap heightMap, float heightMultiplier);
    }
}