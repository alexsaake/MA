using Raylib_cs;

namespace ProceduralLandscapeGeneration.Rendering;

internal interface IVertexMeshCreator
{
    Mesh CreateHeightMapMesh();
    Mesh CreateSeaLevelMesh();
}