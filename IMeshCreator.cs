using Raylib_CsLo;

namespace ProceduralLandscapeGeneration
{
    internal interface IMeshCreator
    {
        Mesh CreateMesh(HeightMap heightMap);
    }
}