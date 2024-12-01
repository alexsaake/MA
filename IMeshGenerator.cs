using Raylib_CsLo;

namespace ProceduralLandscapeGeneration
{
    internal interface IMeshGenerator
    {
        Mesh GenerateTerrainMesh(float[,] noiseMap, float heightMultiplier);
    }
}