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
        int vertexCount = heightMap.Width * heightMap.Depth;
        int triangleCount = (heightMap.Width - 1) * (heightMap.Depth - 1) * 2;
        AllocateMeshData(&mesh, vertexCount, triangleCount);

        int vertexIndex = 0;
        for (int y = 0; y < heightMap.Depth; y++)
        {
            for (int x = 0; x < heightMap.Width; x++)
            {
                AddVertex(mesh, vertexIndex, new Vector3(x, y, 0), Color.White);
                vertexIndex++;
            }
        }
        int indexIndex = 0;
        vertexIndex = 0;
        for (int y = 0; y < heightMap.Depth - 1; y++)
        {
            for (int x = 0; x < heightMap.Width; x++)
            {
                if(x < heightMap.Width - 1)
                {
                    AddQuadIndices(mesh, indexIndex, vertexIndex);
                    indexIndex++;
                }
                vertexIndex++;
            }
        }

        Raylib.UploadMesh(&mesh, false);

        return mesh;
    }

    public unsafe Mesh CreateSeaLevelMesh()
    {
        Mesh mesh = new();
        AllocateMeshData(&mesh, 4, 2);

        int vertexIndex = 0;
        float height = myConfiguration.SeaLevel * myConfiguration.HeightMultiplier;
        Color waterColor = new Color(0, 0, 100, 100);
        AddVertex(mesh, vertexIndex, new Vector3(0, 0, height), waterColor);
        vertexIndex++;
        AddVertex(mesh, vertexIndex, new Vector3((int)myConfiguration.HeightMapSideLength, 0, height), waterColor);
        vertexIndex++;
        AddVertex(mesh, vertexIndex, new Vector3(0, (int)myConfiguration.HeightMapSideLength, height), waterColor);
        vertexIndex++;
        AddVertex(mesh, vertexIndex, new Vector3((int)myConfiguration.HeightMapSideLength, (int)myConfiguration.HeightMapSideLength, height), waterColor);
        int indexIndex = 0;
        vertexIndex = 0;
        AddQuadIndices(mesh, indexIndex, vertexIndex);

        Raylib.UploadMesh(&mesh, false);

        return mesh;
    }

    private static unsafe void AllocateMeshData(Mesh* mesh, int vertexCount, int triangleCount)
    {
        mesh->VertexCount = vertexCount;
        mesh->TriangleCount = triangleCount;

        mesh->AllocVertices();
        mesh->AllocNormals();
        mesh->AllocColors();
        mesh->AllocIndices();
    }

    private unsafe void AddQuadIndices(Mesh mesh, int indexIndex, int vertexIndex)
    {
        mesh.Indices[indexIndex * 6 + 0] = (uint)vertexIndex;
        mesh.Indices[indexIndex * 6 + 1] = (uint)(vertexIndex + 1);
        mesh.Indices[indexIndex * 6 + 2] = (uint)(vertexIndex + myConfiguration.HeightMapSideLength + 1);
        mesh.Indices[indexIndex * 6 + 3] = (uint)vertexIndex;
        mesh.Indices[indexIndex * 6 + 4] = (uint)(vertexIndex + myConfiguration.HeightMapSideLength + 1);
        mesh.Indices[indexIndex * 6 + 5] = (uint)(vertexIndex + myConfiguration.HeightMapSideLength);
    }

    private unsafe void AddVertex(Mesh mesh, int vertexIndex, Vector3 position, Color color)
    {
        mesh.Vertices[vertexIndex * 3 + 0] = position.X;
        mesh.Vertices[vertexIndex * 3 + 1] = position.Y;
        mesh.Vertices[vertexIndex * 3 + 2] = position.Z;

        mesh.Normals[vertexIndex * 3 + 0] = 0;
        mesh.Normals[vertexIndex * 3 + 1] = 0;
        mesh.Normals[vertexIndex * 3 + 2] = 1;

        mesh.Colors[vertexIndex * 4 + 0] = color.R;
        mesh.Colors[vertexIndex * 4 + 1] = color.G;
        mesh.Colors[vertexIndex * 4 + 2] = color.B;
        mesh.Colors[vertexIndex * 4 + 3] = color.A;
    }
}
