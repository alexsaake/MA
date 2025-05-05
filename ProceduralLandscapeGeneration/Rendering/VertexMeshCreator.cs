using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Rendering;

internal class VertexMeshCreator : IVertexMeshCreator
{
    private readonly IConfiguration myConfiguration;

    public VertexMeshCreator(IConfiguration configuration)
    {
        myConfiguration = configuration;
    }

    public unsafe Mesh CreateHeightMapMesh()
    {
        Mesh mesh = new();
        int vertexCount = (int)(myConfiguration.HeightMapSideLength * myConfiguration.HeightMapSideLength);
        int triangleCount = (int)((myConfiguration.HeightMapSideLength - 1) * (myConfiguration.HeightMapSideLength - 1) * 2);
        AllocateMeshData(&mesh, vertexCount, triangleCount);

        int indexIndex = 0;
        int vertexIndex = 0;
        for (int y = 0; y < myConfiguration.HeightMapSideLength; y++)
        {
            for (int x = 0; x < myConfiguration.HeightMapSideLength; x++)
            {
                if (x < myConfiguration.HeightMapSideLength - 1
                    && y < myConfiguration.HeightMapSideLength - 1)
                {
                    AddQuadIndices(mesh, ref indexIndex, vertexIndex , myConfiguration.HeightMapSideLength);
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
        Color waterColor = new Color(0, 94, 184, 127);
        AddQuadIndices(mesh, ref indexIndex, vertexIndex, 2);
        AddVertex(mesh, ref vertexIndex, new Vector3(0, 0, 0), waterColor);
        AddVertex(mesh, ref vertexIndex, new Vector3((int)myConfiguration.HeightMapSideLength, 0, 0), waterColor);
        AddVertex(mesh, ref vertexIndex, new Vector3(0, (int)myConfiguration.HeightMapSideLength, 0), waterColor);
        AddVertex(mesh, ref vertexIndex, new Vector3((int)myConfiguration.HeightMapSideLength, (int)myConfiguration.HeightMapSideLength, 0), waterColor);

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
