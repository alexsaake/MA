using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations.MapGeneration;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Renderers.VertexShader.Cubes;

internal class CubesVertexMeshCreator : ICubesVertexMeshCreator
{
    private const float Offset = 0.5f;
    private static Vector3 myLeftNormal = new Vector3(-1f, 0f, 0f);
    private static Vector3 myRightNormal = new Vector3(1f, 0f, 0f);
    private static Vector3 myFrontNormal = new Vector3(0f, -1f, 0f);
    private static Vector3 myBackNormal = new Vector3(0f, 1f, 0f);
    private static Vector3 myDownNormal = new Vector3(0f, 0f, -1f);
    private static Vector3 myUpNormal = new Vector3(0f, 0f, 1f);
    private static Color myBedrockColor = new Color(0.6f, 0.6f, 0.6f, 1.0f);
    private static Color myCoarseSedimentColor = new Color(0.5f, 0.3f, 0.3f, 1.0f);
    private static Color myFineSedimentColor = new Color(1.0f, 0.9f, 0.6f, 1.0f);
    private static Color[]? myRockTypeColors;

    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IShaderBuffers myShaderBuffers;

    public CubesVertexMeshCreator(IMapGenerationConfiguration mapGenerationConfiguration, IShaderBuffers shaderBuffers)
    {
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myShaderBuffers = shaderBuffers;
    }

    public unsafe Mesh CreateCubesMesh()
    {
        switch (myMapGenerationConfiguration.RockTypeCount)
        {
            case 1:
                myRockTypeColors = new Color[] { myBedrockColor };
                break;
            case 2:
                myRockTypeColors = new Color[] { myBedrockColor, myFineSedimentColor };
                break;
            case 3:
                myRockTypeColors = new Color[] { myBedrockColor, myCoarseSedimentColor, myFineSedimentColor };
                break;
        }
        float[] heightMap = LoadHeightMap();

        Mesh mesh = new();
        int cubes = (int)(myMapGenerationConfiguration.HeightMapPlaneSize * myMapGenerationConfiguration.RockTypeCount * myMapGenerationConfiguration.LayerCount);
        int vertexCount = cubes * 24;
        int triangleCount = cubes * 12;
        AllocateMeshData(&mesh, vertexCount, triangleCount);

        int indexIndex = 0;
        int vertexIndex = 0;
        for (int layer = 0; layer < myMapGenerationConfiguration.LayerCount; layer++)
        {
            for (uint y = 0; y < myMapGenerationConfiguration.HeightMapSideLength; y++)
            {
                for (uint x = 0; x < myMapGenerationConfiguration.HeightMapSideLength; x++)
                {
                    AddLayerCubes(mesh, ref vertexIndex, ref indexIndex, heightMap, x, y, layer);
                }
            }
        }

        Raylib.UploadMesh(&mesh, false);

        return mesh;
    }

    private unsafe float[] LoadHeightMap()
    {
        float[] heightMap = new float[myMapGenerationConfiguration.HeightMapSize];
        fixed (void* heightMapPointer = heightMap)
        {
            Rlgl.ReadShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], heightMapPointer, myMapGenerationConfiguration.HeightMapSize * sizeof(float), 0);
        }
        return heightMap;
    }

    private void AddLayerCubes(Mesh mesh, ref int vertexIndex, ref int indexIndex, float[] heightMap, uint x, uint y, int layer)
    {
        uint index = myMapGenerationConfiguration.GetIndex(x, y);
        float rockTypeBottom = 0.0f;
        if (layer > 0)
        {
            rockTypeBottom = heightMap[index + layer * myMapGenerationConfiguration.RockTypeCount * myMapGenerationConfiguration.HeightMapPlaneSize] * myMapGenerationConfiguration.HeightMultiplier;
        }
        float totalTop = rockTypeBottom;
        float totalBottom = rockTypeBottom;
        for (int rockType = 0; rockType < myMapGenerationConfiguration.RockTypeCount; rockType++)
        {
            float rockTypeTop = heightMap[index + rockType * myMapGenerationConfiguration.HeightMapPlaneSize + layer * myMapGenerationConfiguration.RockTypeCount * myMapGenerationConfiguration.HeightMapPlaneSize] * myMapGenerationConfiguration.HeightMultiplier;
            if (rockTypeTop == 0.0f)
            {
                continue;
            }
            if (rockType > 0)
            {
                totalBottom = totalTop;
            }
            totalTop += rockTypeTop;
            Color color = myRockTypeColors![rockType];
            AddCube(mesh, ref vertexIndex, ref indexIndex, x, y, totalBottom, totalTop, color);
        }
    }

    private void AddCube(Mesh mesh, ref int vertexIndex, ref int indexIndex, uint x, uint y, float bottom, float top, Color color)
    {
        AddQuadIndices(mesh, ref indexIndex, vertexIndex);
        AddVertex(mesh, ref vertexIndex, new Vector3(x - Offset, y - Offset, top), color, myUpNormal);
        AddVertex(mesh, ref vertexIndex, new Vector3(x + Offset, y - Offset, top), color, myUpNormal);
        AddVertex(mesh, ref vertexIndex, new Vector3(x - Offset, y + Offset, top), color, myUpNormal);
        AddVertex(mesh, ref vertexIndex, new Vector3(x + Offset, y + Offset, top), color, myUpNormal);

        AddQuadIndices(mesh, ref indexIndex, vertexIndex);
        AddVertex(mesh, ref vertexIndex, new Vector3(x - Offset, y + Offset, bottom), color, myLeftNormal);
        AddVertex(mesh, ref vertexIndex, new Vector3(x - Offset, y - Offset, bottom), color, myLeftNormal);
        AddVertex(mesh, ref vertexIndex, new Vector3(x - Offset, y + Offset, top), color, myLeftNormal);
        AddVertex(mesh, ref vertexIndex, new Vector3(x - Offset, y - Offset, top), color, myLeftNormal);

        AddQuadIndices(mesh, ref indexIndex, vertexIndex);
        AddVertex(mesh, ref vertexIndex, new Vector3(x + Offset, y - Offset, bottom), color, myRightNormal);
        AddVertex(mesh, ref vertexIndex, new Vector3(x + Offset, y + Offset, bottom), color, myRightNormal);
        AddVertex(mesh, ref vertexIndex, new Vector3(x + Offset, y - Offset, top), color, myRightNormal);
        AddVertex(mesh, ref vertexIndex, new Vector3(x + Offset, y + Offset, top), color, myRightNormal);

        AddQuadIndices(mesh, ref indexIndex, vertexIndex);
        AddVertex(mesh, ref vertexIndex, new Vector3(x - Offset, y - Offset, bottom), color, myFrontNormal);
        AddVertex(mesh, ref vertexIndex, new Vector3(x + Offset, y - Offset, bottom), color, myFrontNormal);
        AddVertex(mesh, ref vertexIndex, new Vector3(x - Offset, y - Offset, top), color, myFrontNormal);
        AddVertex(mesh, ref vertexIndex, new Vector3(x + Offset, y - Offset, top), color, myFrontNormal);

        AddQuadIndices(mesh, ref indexIndex, vertexIndex);
        AddVertex(mesh, ref vertexIndex, new Vector3(x + Offset, y + Offset, bottom), color, myBackNormal);
        AddVertex(mesh, ref vertexIndex, new Vector3(x - Offset, y + Offset, bottom), color, myBackNormal);
        AddVertex(mesh, ref vertexIndex, new Vector3(x + Offset, y + Offset, top), color, myBackNormal);
        AddVertex(mesh, ref vertexIndex, new Vector3(x - Offset, y + Offset, top), color, myBackNormal);
    }

    public unsafe Mesh CreateSeaLevelMesh()
    {
        Mesh mesh = new();
        AllocateMeshData(&mesh, 4, 2);

        int indexIndex = 0;
        int vertexIndex = 0;
        AddQuadIndices(mesh, ref indexIndex, vertexIndex);
        AddVertex(mesh, ref vertexIndex, new Vector3(0, 0, 0), Color.White, Vector3.UnitZ);
        AddVertex(mesh, ref vertexIndex, new Vector3((int)myMapGenerationConfiguration.HeightMapSideLength, 0, 0), Color.White, Vector3.UnitZ);
        AddVertex(mesh, ref vertexIndex, new Vector3(0, (int)myMapGenerationConfiguration.HeightMapSideLength, 0), Color.White, Vector3.UnitZ);
        AddVertex(mesh, ref vertexIndex, new Vector3((int)myMapGenerationConfiguration.HeightMapSideLength, (int)myMapGenerationConfiguration.HeightMapSideLength, 0), Color.White, Vector3.UnitZ);

        Raylib.UploadMesh(&mesh, false);

        return mesh;
    }

    private static unsafe void AllocateMeshData(Mesh* mesh, int vertexCount, int triangleCount)
    {
        mesh->VertexCount = vertexCount;
        mesh->TriangleCount = triangleCount;

        mesh->AllocVertices();
        mesh->AllocColors();
        mesh->AllocNormals();
        mesh->AllocIndices();
    }

    private unsafe void AddQuadIndices(Mesh mesh, ref int indexIndex, int vertexIndex)
    {
        mesh.Indices[indexIndex * 6 + 0] = (uint)vertexIndex;
        mesh.Indices[indexIndex * 6 + 1] = (uint)(vertexIndex + 1);
        mesh.Indices[indexIndex * 6 + 2] = (uint)(vertexIndex + 3);
        mesh.Indices[indexIndex * 6 + 3] = (uint)vertexIndex;
        mesh.Indices[indexIndex * 6 + 4] = (uint)(vertexIndex + 3);
        mesh.Indices[indexIndex * 6 + 5] = (uint)(vertexIndex + 2);

        indexIndex++;
    }

    private unsafe void AddVertex(Mesh mesh, ref int vertexIndex, Vector3 position, Color color, Vector3 normal)
    {
        mesh.Vertices[vertexIndex * 3 + 0] = position.X;
        mesh.Vertices[vertexIndex * 3 + 1] = position.Y;
        mesh.Vertices[vertexIndex * 3 + 2] = position.Z;

        mesh.Colors[vertexIndex * 4 + 0] = color.R;
        mesh.Colors[vertexIndex * 4 + 1] = color.G;
        mesh.Colors[vertexIndex * 4 + 2] = color.B;
        mesh.Colors[vertexIndex * 4 + 3] = color.A;

        mesh.Normals[vertexIndex * 3 + 0] = normal.X;
        mesh.Normals[vertexIndex * 3 + 1] = normal.Y;
        mesh.Normals[vertexIndex * 3 + 2] = normal.Z;

        vertexIndex++;
    }
}
