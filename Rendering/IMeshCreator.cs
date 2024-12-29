using ProceduralLandscapeGeneration.Common;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Rendering
{
    internal interface IMeshCreator
    {
        Mesh CreateMesh(HeightMap heightMap);
    }
}