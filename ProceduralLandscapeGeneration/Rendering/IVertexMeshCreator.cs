using ProceduralLandscapeGeneration.Common;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Rendering;

internal interface IVertexMeshCreator
{
    Mesh CreateHeightMapMesh(HeightMap heightMap);
    Mesh CreateSeaLevelMesh();
}