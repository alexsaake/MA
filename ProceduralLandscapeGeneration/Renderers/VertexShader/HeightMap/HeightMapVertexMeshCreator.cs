using ProceduralLandscapeGeneration.Configurations.MapGeneration;
using Raylib_cs;
using System.Diagnostics;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Renderers.VertexShader.HeightMap;

internal class HeightMapVertexMeshCreator : IHeightMapVertexMeshCreator
{
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;

    public HeightMapVertexMeshCreator(IMapGenerationConfiguration mapGenerationConfiguration)
    {
        myMapGenerationConfiguration = mapGenerationConfiguration;
    }

    public unsafe Mesh CreateTerrainHeightMapMesh()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        Mesh mesh = new();
        int vertexCount = (int)myMapGenerationConfiguration.HeightMapPlaneSize;
        int triangleCount = (int)((myMapGenerationConfiguration.HeightMapSideLength - 1) * (myMapGenerationConfiguration.HeightMapSideLength - 1) * 2);
        AllocateMeshData(&mesh, vertexCount, triangleCount);

        int indicesIndex = 0;
        int verticesIndex = 0;
        for (int y = 0; y < myMapGenerationConfiguration.HeightMapSideLength; y++)
        {
            for (int x = 0; x < myMapGenerationConfiguration.HeightMapSideLength; x++)
            {
                if (x < myMapGenerationConfiguration.HeightMapSideLength - 1
                    && y < myMapGenerationConfiguration.HeightMapSideLength - 1)
                {
                    AddQuadIndices(mesh, ref indicesIndex, verticesIndex, myMapGenerationConfiguration.HeightMapSideLength);
                }
                AddVertex(mesh, ref verticesIndex, new Vector3(x, y, 0));
            }
        }

        Raylib.UploadMesh(&mesh, false);
        stopwatch.Stop();
        Console.WriteLine($"Heightmap mesh creator: {stopwatch.Elapsed}");

        return mesh;
    }

    public unsafe Mesh CreateSeaLevelMesh()
    {
        Mesh mesh = new();
        AllocateMeshData(&mesh, 4, 2);

        int indicesIndex = 0;
        int verticesIndex = 0;
        AddQuadIndices(mesh, ref indicesIndex, verticesIndex, 2);
        AddVertex(mesh, ref verticesIndex, new Vector3(0, 0, 0));
        AddVertex(mesh, ref verticesIndex, new Vector3((int)myMapGenerationConfiguration.HeightMapSideLength, 0, 0));
        AddVertex(mesh, ref verticesIndex, new Vector3(0, (int)myMapGenerationConfiguration.HeightMapSideLength, 0));
        AddVertex(mesh, ref verticesIndex, new Vector3((int)myMapGenerationConfiguration.HeightMapSideLength, (int)myMapGenerationConfiguration.HeightMapSideLength, 0));

        Raylib.UploadMesh(&mesh, false);

        return mesh;
    }

    private static unsafe void AllocateMeshData(Mesh* mesh, int vertexCount, int triangleCount)
    {
        mesh->VertexCount = vertexCount;
        mesh->TriangleCount = triangleCount;

        mesh->AllocVertices();
        mesh->AllocIndices();
    }

    private unsafe void AddQuadIndices(Mesh mesh, ref int indicesIndex, int verticesIndex, uint sideLength)
    {
        mesh.Indices[indicesIndex * 6 + 0] = (uint)verticesIndex;
        mesh.Indices[indicesIndex * 6 + 1] = (uint)(verticesIndex + 1);
        mesh.Indices[indicesIndex * 6 + 2] = (uint)(verticesIndex + sideLength + 1);
        mesh.Indices[indicesIndex * 6 + 3] = (uint)verticesIndex;
        mesh.Indices[indicesIndex * 6 + 4] = (uint)(verticesIndex + sideLength + 1);
        mesh.Indices[indicesIndex * 6 + 5] = (uint)(verticesIndex + sideLength);

        indicesIndex++;
    }

    private unsafe void AddVertex(Mesh mesh, ref int verticesIndex, Vector3 position)
    {
        mesh.Vertices[verticesIndex * 3 + 0] = position.X;
        mesh.Vertices[verticesIndex * 3 + 1] = position.Y;
        mesh.Vertices[verticesIndex * 3 + 2] = position.Z;

        verticesIndex++;
    }
}
