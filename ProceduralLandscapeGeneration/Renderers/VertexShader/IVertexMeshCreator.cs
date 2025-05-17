using Raylib_cs;

namespace ProceduralLandscapeGeneration.Renderers.VertexShader;

internal interface IVertexMeshCreator
{
    Mesh CreateHeightMapMesh();
    Mesh CreateSeaLevelMesh();
}