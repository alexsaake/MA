using Raylib_cs;

namespace ProceduralLandscapeGeneration.Renderers.VertexShader.Cubes;

internal interface ICubesVertexMeshCreator
{
    Mesh CreateTerrainCubesMesh();
    Mesh CreateWaterCubesMesh();
    Mesh CreateSeaLevelMesh();
}