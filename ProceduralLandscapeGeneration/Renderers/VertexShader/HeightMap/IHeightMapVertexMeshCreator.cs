using Raylib_cs;

namespace ProceduralLandscapeGeneration.Renderers.VertexShader.HeightMap;

internal interface IHeightMapVertexMeshCreator
{
    Mesh CreateHeightMapMesh();
    Mesh CreateSeaLevelMesh();
}