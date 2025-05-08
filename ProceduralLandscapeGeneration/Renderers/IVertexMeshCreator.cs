using Raylib_cs;

namespace ProceduralLandscapeGeneration.Renderers;

internal interface IVertexMeshCreator
{
    Mesh CreateHeightMapMesh();
    Mesh CreateSeaLevelMesh();
}