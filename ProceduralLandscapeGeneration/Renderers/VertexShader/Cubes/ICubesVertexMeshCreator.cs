using Raylib_cs;

namespace ProceduralLandscapeGeneration.Renderers.VertexShader.Cubes
{
    internal interface ICubesVertexMeshCreator
    {
        Mesh CreateCubesMesh();
        Mesh CreateSeaLevelMesh();
    }
}