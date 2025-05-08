using ProceduralLandscapeGeneration.Configurations;
using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Renderers;

internal class VertexMeshCreator : IVertexMeshCreator
{
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;

    public VertexMeshCreator(IMapGenerationConfiguration mapGenerationConfiguration)
    {
        myMapGenerationConfiguration = mapGenerationConfiguration;
    }

    public unsafe Mesh CreateHeightMapMesh()
    {
        Mesh mesh = new();
        int vertexCount = (int)(myMapGenerationConfiguration.HeightMapSideLength * myMapGenerationConfiguration.HeightMapSideLength);
        int triangleCount = (int)((myMapGenerationConfiguration.HeightMapSideLength - 1) * (myMapGenerationConfiguration.HeightMapSideLength - 1) * 2);
        AllocateMeshData(&mesh, vertexCount, triangleCount);

        int indexIndex = 0;
        int vertexIndex = 0;
        for (int y = 0; y < myMapGenerationConfiguration.HeightMapSideLength; y++)
        {
            for (int x = 0; x < myMapGenerationConfiguration.HeightMapSideLength; x++)
            {
                if (x < myMapGenerationConfiguration.HeightMapSideLength - 1
                    && y < myMapGenerationConfiguration.HeightMapSideLength - 1)
                {
                    AddQuadIndices(mesh, ref indexIndex, vertexIndex, myMapGenerationConfiguration.HeightMapSideLength);
                }
                AddVertex(mesh, ref vertexIndex, new Vector3(x, y, 0), Color.White);
            }
        }

        Raylib.UploadMesh(&mesh, false);

        return mesh;
    }

    public unsafe Mesh CreateSeaLevelMesh()
    {
        Mesh mesh = new();
        AllocateMeshData(&mesh, 4, 2);

        int indexIndex = 0;
        int vertexIndex = 0;
        Color oceanColor = new Color(0, 94, 184, 127);
        AddQuadIndices(mesh, ref indexIndex, vertexIndex, 2);
        AddVertex(mesh, ref vertexIndex, new Vector3(0, 0, 0), oceanColor);
        AddVertex(mesh, ref vertexIndex, new Vector3((int)myMapGenerationConfiguration.HeightMapSideLength, 0, 0), oceanColor);
        AddVertex(mesh, ref vertexIndex, new Vector3(0, (int)myMapGenerationConfiguration.HeightMapSideLength, 0), oceanColor);
        AddVertex(mesh, ref vertexIndex, new Vector3((int)myMapGenerationConfiguration.HeightMapSideLength, (int)myMapGenerationConfiguration.HeightMapSideLength, 0), oceanColor);

        Raylib.UploadMesh(&mesh, false);

        return mesh;
    }

    private static unsafe void AllocateMeshData(Mesh* mesh, int vertexCount, int triangleCount)
    {
        mesh->VertexCount = vertexCount;
        mesh->TriangleCount = triangleCount;

        mesh->AllocVertices();
        mesh->AllocColors();
        mesh->AllocIndices();
    }

    private unsafe void AddQuadIndices(Mesh mesh, ref int indexIndex, int vertexIndex, uint sideLength)
    {
        mesh.Indices[indexIndex * 6 + 0] = (uint)vertexIndex;
        mesh.Indices[indexIndex * 6 + 1] = (uint)(vertexIndex + 1);
        mesh.Indices[indexIndex * 6 + 2] = (uint)(vertexIndex + sideLength + 1);
        mesh.Indices[indexIndex * 6 + 3] = (uint)vertexIndex;
        mesh.Indices[indexIndex * 6 + 4] = (uint)(vertexIndex + sideLength + 1);
        mesh.Indices[indexIndex * 6 + 5] = (uint)(vertexIndex + sideLength);

        indexIndex++;
    }

    private unsafe void AddVertex(Mesh mesh, ref int vertexIndex, Vector3 position, Color color)
    {
        mesh.Vertices[vertexIndex * 3 + 0] = position.X;
        mesh.Vertices[vertexIndex * 3 + 1] = position.Y;
        mesh.Vertices[vertexIndex * 3 + 2] = position.Z;

        mesh.Colors[vertexIndex * 4 + 0] = color.R;
        mesh.Colors[vertexIndex * 4 + 1] = color.G;
        mesh.Colors[vertexIndex * 4 + 2] = color.B;
        mesh.Colors[vertexIndex * 4 + 3] = color.A;

        vertexIndex++;
    }
}
