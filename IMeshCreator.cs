using Raylib_cs;

namespace ProceduralLandscapeGeneration
{
    internal interface IMeshCreator
    {
        Mesh CreateMesh(HeightMap heightMap);
    }
}