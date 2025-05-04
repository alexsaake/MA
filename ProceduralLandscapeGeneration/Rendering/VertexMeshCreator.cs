using ProceduralLandscapeGeneration.Common;
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

    public unsafe Mesh CreateHeightMapMesh(HeightMap heightMap)
    {
        Mesh mesh = new();
        int triangles = (heightMap.Width - 1) * (heightMap.Depth - 1) * 2;
        AllocateMeshData(&mesh, triangles);

        int vertexIndex = 0;

        for (int x = 0; x < heightMap.Width - 1; x++)
        {
            for (int y = 0; y < heightMap.Depth - 1; y++)
            {
                AddVertex(mesh, ref vertexIndex, heightMap, x, y);
                AddVertex(mesh, ref vertexIndex, heightMap, x + 1, y);
                AddVertex(mesh, ref vertexIndex, heightMap, x + 1, y + 1);
                AddVertex(mesh, ref vertexIndex, heightMap, x, y);
                AddVertex(mesh, ref vertexIndex, heightMap, x + 1, y + 1);
                AddVertex(mesh, ref vertexIndex, heightMap, x, y + 1);
            }
        }

        Raylib.UploadMesh(&mesh, false);

        return mesh;
    }

    public unsafe Mesh CreateSeaLevelMesh()
    {
        Mesh mesh = new();
        int triangles = 2;
        AllocateMeshData(&mesh, triangles);

        int vertexIndex = 0;

        AddVertex(mesh, ref vertexIndex, 0, 0, myConfiguration.SeaLevel);
        AddVertex(mesh, ref vertexIndex, (int)myConfiguration.HeightMapSideLength, 0, myConfiguration.SeaLevel);
        AddVertex(mesh, ref vertexIndex, (int)myConfiguration.HeightMapSideLength, (int)myConfiguration.HeightMapSideLength, myConfiguration.SeaLevel);
        AddVertex(mesh, ref vertexIndex, 0, 0, myConfiguration.SeaLevel);
        AddVertex(mesh, ref vertexIndex, (int)myConfiguration.HeightMapSideLength, (int)myConfiguration.HeightMapSideLength, myConfiguration.SeaLevel);
        AddVertex(mesh, ref vertexIndex, 0, (int)myConfiguration.HeightMapSideLength, myConfiguration.SeaLevel);

        Raylib.UploadMesh(&mesh, false);

        return mesh;
    }

    private unsafe void AddVertex(Mesh mesh, ref int vertexIndex, HeightMap heightMap, int x, int y)
    {
        float height = heightMap.Height[x, y] * myConfiguration.HeightMultiplier;
        mesh.Vertices[vertexIndex * 3 + 0] = x;
        mesh.Vertices[vertexIndex * 3 + 1] = y;
        mesh.Vertices[vertexIndex * 3 + 2] = height;


        Vector3 normal = heightMap.GetScaledNormal(x, y);
        mesh.Normals[vertexIndex * 3 + 0] = normal.X;
        mesh.Normals[vertexIndex * 3 + 1] = normal.Y;
        mesh.Normals[vertexIndex * 3 + 2] = normal.Z;

        mesh.Colors[vertexIndex * 4 + 0] = 255;
        mesh.Colors[vertexIndex * 4 + 1] = 255;
        mesh.Colors[vertexIndex * 4 + 2] = 255;
        mesh.Colors[vertexIndex * 4 + 3] = 255;

        vertexIndex++;
    }

    private unsafe void AddVertex(Mesh mesh, ref int vertexIndex, int x, int y, float z)
    {
        mesh.Vertices[vertexIndex * 3 + 0] = x;
        mesh.Vertices[vertexIndex * 3 + 1] = y;
        mesh.Vertices[vertexIndex * 3 + 2] = z;

        mesh.Normals[vertexIndex * 3 + 0] = 0;
        mesh.Normals[vertexIndex * 3 + 1] = 0;
        mesh.Normals[vertexIndex * 3 + 2] = 1;

        mesh.Colors[vertexIndex * 4 + 0] = 0;
        mesh.Colors[vertexIndex * 4 + 1] = 0;
        mesh.Colors[vertexIndex * 4 + 2] = 255;
        mesh.Colors[vertexIndex * 4 + 3] = 100;

        vertexIndex++;
    }

    private static unsafe void AllocateMeshData(Mesh* mesh, int triangleCount)
    {
        mesh->VertexCount = triangleCount * 3;
        mesh->TriangleCount = triangleCount;

        mesh->Vertices = (float*)Raylib.MemAlloc((uint)(mesh->VertexCount * 3 * sizeof(float)));
        mesh->Normals = (float*)Raylib.MemAlloc((uint)(mesh->VertexCount * 3 * sizeof(float)));
        mesh->Colors = (byte*)Raylib.MemAlloc((uint)(mesh->VertexCount * 4 * sizeof(byte)));

        mesh->Indices = null;
    }
}
