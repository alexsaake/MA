using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations.MapGeneration;
using Raylib_cs;
using System.Diagnostics;
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

    public unsafe Mesh CreateTerrainCubesMesh()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
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

        Mesh mesh = new Mesh();
        int cubes = (int)(myMapGenerationConfiguration.HeightMapPlaneSize * myMapGenerationConfiguration.RockTypeCount * myMapGenerationConfiguration.LayerCount);
        int vertexCount = cubes * 24;
        int triangleCount = cubes * 12;
        AllocateMeshData(&mesh, vertexCount, triangleCount);

        int indicesIndex = 0;
        int verticesIndex = 0;
        for (uint layer = 0; layer < myMapGenerationConfiguration.LayerCount; layer++)
        {
            for (uint y = 0; y < myMapGenerationConfiguration.HeightMapSideLength; y++)
            {
                for (uint x = 0; x < myMapGenerationConfiguration.HeightMapSideLength; x++)
                {
                    AddLayerCubes(mesh, ref verticesIndex, ref indicesIndex, x, y, layer);
                }
            }
        }

        Raylib.UploadMesh(&mesh, false);
        stopwatch.Stop();
        Console.WriteLine($"Cube mesh creator: {stopwatch.Elapsed}");

        return mesh;
    }
    private void AddLayerCubes(Mesh mesh, ref int verticesIndex, ref int indicesIndex, uint x, uint y, uint layer)
    {
        uint index = myMapGenerationConfiguration.GetIndex(x, y);
        for (int rockType = 0; rockType < myMapGenerationConfiguration.RockTypeCount; rockType++)
        {
            int topIndex = (int)(index + rockType * myMapGenerationConfiguration.HeightMapPlaneSize + (layer * myMapGenerationConfiguration.RockTypeCount + layer) * myMapGenerationConfiguration.HeightMapPlaneSize);
            int bottomIndex = -1;
            if (rockType > 0)
            {
                bottomIndex = (int)(index + (rockType - 1) * myMapGenerationConfiguration.HeightMapPlaneSize + (layer * myMapGenerationConfiguration.RockTypeCount + layer) * myMapGenerationConfiguration.HeightMapPlaneSize);
            }
            else if (layer > 0)
            {
                bottomIndex = (int)(index + layer * myMapGenerationConfiguration.RockTypeCount * myMapGenerationConfiguration.HeightMapPlaneSize);
            }
            Color color = myRockTypeColors![rockType];
            if (rockType == myMapGenerationConfiguration.RockTypeCount - 1)
            {
                AddCubeTop(mesh, ref verticesIndex, ref indicesIndex, x, y, color, topIndex);
            }
            AddCubeSides(mesh, ref verticesIndex, ref indicesIndex, x, y, color, topIndex, bottomIndex);
            if (rockType == 0)
            {
                AddCubeBottom(mesh, ref verticesIndex, ref indicesIndex, x, y, color, bottomIndex);
            }
        }
    }

    private void AddCubeTop(Mesh mesh, ref int verticesIndex, ref int indicesIndex, uint x, uint y, Color color, int topIndex)
    {
        AddQuadIndices(mesh, ref indicesIndex, verticesIndex);
        AddVertex(mesh, ref verticesIndex, new Vector3(x - Offset, y - Offset, 0), color, myUpNormal, topIndex, 1);
        AddVertex(mesh, ref verticesIndex, new Vector3(x + Offset, y - Offset, 0), color, myUpNormal, topIndex, 1);
        AddVertex(mesh, ref verticesIndex, new Vector3(x - Offset, y + Offset, 0), color, myUpNormal, topIndex, 1);
        AddVertex(mesh, ref verticesIndex, new Vector3(x + Offset, y + Offset, 0), color, myUpNormal, topIndex, 1);
    }

    private void AddCubeSides(Mesh mesh, ref int verticesIndex, ref int indicesIndex, uint x, uint y, Color color, int topIndex, int bottomIndex)
    {
        AddQuadIndices(mesh, ref indicesIndex, verticesIndex);
        AddVertex(mesh, ref verticesIndex, new Vector3(x - Offset, y + Offset, 0), color, myLeftNormal, bottomIndex, 0);
        AddVertex(mesh, ref verticesIndex, new Vector3(x - Offset, y - Offset, 0), color, myLeftNormal, bottomIndex, 0);
        AddVertex(mesh, ref verticesIndex, new Vector3(x - Offset, y + Offset, 0), color, myLeftNormal, topIndex, 0);
        AddVertex(mesh, ref verticesIndex, new Vector3(x - Offset, y - Offset, 0), color, myLeftNormal, topIndex, 0);

        AddQuadIndices(mesh, ref indicesIndex, verticesIndex);
        AddVertex(mesh, ref verticesIndex, new Vector3(x + Offset, y - Offset, 0), color, myRightNormal, bottomIndex, 0);
        AddVertex(mesh, ref verticesIndex, new Vector3(x + Offset, y + Offset, 0), color, myRightNormal, bottomIndex, 0);
        AddVertex(mesh, ref verticesIndex, new Vector3(x + Offset, y - Offset, 0), color, myRightNormal, topIndex, 0);
        AddVertex(mesh, ref verticesIndex, new Vector3(x + Offset, y + Offset, 0), color, myRightNormal, topIndex, 0);

        AddQuadIndices(mesh, ref indicesIndex, verticesIndex);
        AddVertex(mesh, ref verticesIndex, new Vector3(x - Offset, y - Offset, 0), color, myFrontNormal, bottomIndex, 0);
        AddVertex(mesh, ref verticesIndex, new Vector3(x + Offset, y - Offset, 0), color, myFrontNormal, bottomIndex, 0);
        AddVertex(mesh, ref verticesIndex, new Vector3(x - Offset, y - Offset, 0), color, myFrontNormal, topIndex, 0);
        AddVertex(mesh, ref verticesIndex, new Vector3(x + Offset, y - Offset, 0), color, myFrontNormal, topIndex, 0);

        AddQuadIndices(mesh, ref indicesIndex, verticesIndex);
        AddVertex(mesh, ref verticesIndex, new Vector3(x + Offset, y + Offset, 0), color, myBackNormal, bottomIndex, 0);
        AddVertex(mesh, ref verticesIndex, new Vector3(x - Offset, y + Offset, 0), color, myBackNormal, bottomIndex, 0);
        AddVertex(mesh, ref verticesIndex, new Vector3(x + Offset, y + Offset, 0), color, myBackNormal, topIndex, 0);
        AddVertex(mesh, ref verticesIndex, new Vector3(x - Offset, y + Offset, 0), color, myBackNormal, topIndex, 0);
    }

    private void AddCubeBottom(Mesh mesh, ref int verticesIndex, ref int indicesIndex, uint x, uint y, Color color, int bottomIndex)
    {
        AddQuadIndices(mesh, ref indicesIndex, verticesIndex);
        AddVertex(mesh, ref verticesIndex, new Vector3(x + Offset, y - Offset, 0), color, myUpNormal, bottomIndex, 2);
        AddVertex(mesh, ref verticesIndex, new Vector3(x - Offset, y - Offset, 0), color, myUpNormal, bottomIndex, 2);
        AddVertex(mesh, ref verticesIndex, new Vector3(x + Offset, y + Offset, 0), color, myUpNormal, bottomIndex, 2);
        AddVertex(mesh, ref verticesIndex, new Vector3(x - Offset, y + Offset, 0), color, myUpNormal, bottomIndex, 2);
    }

    public unsafe Mesh CreateWaterCubesMesh()
    {
        Mesh mesh = new Mesh();
        int cubes = (int)(myMapGenerationConfiguration.HeightMapPlaneSize * myMapGenerationConfiguration.LayerCount);
        int vertexCount = cubes * 8;
        int triangleCount = cubes * 10;
        AllocateMeshData(&mesh, vertexCount, triangleCount);

        int indicesIndex = 0;
        int verticesIndex = 0;
        for (uint layer = 0; layer < myMapGenerationConfiguration.LayerCount; layer++)
        {
            for (uint y = 0; y < myMapGenerationConfiguration.HeightMapSideLength; y++)
            {
                for (uint x = 0; x < myMapGenerationConfiguration.HeightMapSideLength; x++)
                {
                    uint gridHydraulicErosionCellIndex = (uint)(myMapGenerationConfiguration.GetIndex(x, y) + layer * myMapGenerationConfiguration.HeightMapPlaneSize);
                    AddCube(mesh, ref verticesIndex, ref indicesIndex, x, y, gridHydraulicErosionCellIndex);
                }
            }
        }

        Raylib.UploadMesh(&mesh, false);

        return mesh;
    }

    private void AddCube(Mesh mesh, ref int verticesIndex, ref int indicesIndex, uint x, uint y, uint gridHydraulicErosionCellIndex)
    {
        AddCubeIndices(mesh, ref indicesIndex, verticesIndex);
        AddVertex(mesh, ref verticesIndex, new Vector3(x - Offset, y - Offset, 0), Color.White, Vector3.UnitZ, gridHydraulicErosionCellIndex, 0);
        AddVertex(mesh, ref verticesIndex, new Vector3(x + Offset, y - Offset, 0), Color.White, Vector3.UnitZ, gridHydraulicErosionCellIndex, 0);
        AddVertex(mesh, ref verticesIndex, new Vector3(x - Offset, y + Offset, 0), Color.White, Vector3.UnitZ, gridHydraulicErosionCellIndex, 0);
        AddVertex(mesh, ref verticesIndex, new Vector3(x + Offset, y + Offset, 0), Color.White, Vector3.UnitZ, gridHydraulicErosionCellIndex, 0);

        AddVertex(mesh, ref verticesIndex, new Vector3(x - Offset, y - Offset, 0), Color.White, Vector3.UnitZ, gridHydraulicErosionCellIndex, 1);
        AddVertex(mesh, ref verticesIndex, new Vector3(x + Offset, y - Offset, 0), Color.White, Vector3.UnitZ, gridHydraulicErosionCellIndex, 1);
        AddVertex(mesh, ref verticesIndex, new Vector3(x - Offset, y + Offset, 0), Color.White, Vector3.UnitZ, gridHydraulicErosionCellIndex, 1);
        AddVertex(mesh, ref verticesIndex, new Vector3(x + Offset, y + Offset, 0), Color.White, Vector3.UnitZ, gridHydraulicErosionCellIndex, 1);
    }

    private unsafe void AddCubeIndices(Mesh mesh, ref int indicesIndex, int verticesIndex)
    {
        //top
        mesh.Indices[indicesIndex * 30 + 0] = (uint)verticesIndex;
        mesh.Indices[indicesIndex * 30 + 1] = (uint)(verticesIndex + 1);
        mesh.Indices[indicesIndex * 30 + 2] = (uint)(verticesIndex + 3);
        mesh.Indices[indicesIndex * 30 + 3] = (uint)verticesIndex;
        mesh.Indices[indicesIndex * 30 + 4] = (uint)(verticesIndex + 3);
        mesh.Indices[indicesIndex * 30 + 5] = (uint)(verticesIndex + 2);

        //left
        mesh.Indices[indicesIndex * 30 + 6] = (uint)(verticesIndex + 6);
        mesh.Indices[indicesIndex * 30 + 7] = (uint)(verticesIndex + 4);
        mesh.Indices[indicesIndex * 30 + 8] = (uint)(verticesIndex + 0);
        mesh.Indices[indicesIndex * 30 + 9] = (uint)(verticesIndex + 6);
        mesh.Indices[indicesIndex * 30 + 10] = (uint)(verticesIndex + 0);
        mesh.Indices[indicesIndex * 30 + 11] = (uint)(verticesIndex + 2);

        //right
        mesh.Indices[indicesIndex * 30 + 12] = (uint)(verticesIndex + 5);
        mesh.Indices[indicesIndex * 30 + 13] = (uint)(verticesIndex + 7);
        mesh.Indices[indicesIndex * 30 + 14] = (uint)(verticesIndex + 3);
        mesh.Indices[indicesIndex * 30 + 15] = (uint)(verticesIndex + 5);
        mesh.Indices[indicesIndex * 30 + 16] = (uint)(verticesIndex + 3);
        mesh.Indices[indicesIndex * 30 + 17] = (uint)(verticesIndex + 1);

        //front
        mesh.Indices[indicesIndex * 30 + 18] = (uint)(verticesIndex + 4);
        mesh.Indices[indicesIndex * 30 + 19] = (uint)(verticesIndex + 5);
        mesh.Indices[indicesIndex * 30 + 20] = (uint)(verticesIndex + 1);
        mesh.Indices[indicesIndex * 30 + 21] = (uint)(verticesIndex + 4);
        mesh.Indices[indicesIndex * 30 + 22] = (uint)(verticesIndex + 1);
        mesh.Indices[indicesIndex * 30 + 23] = (uint)(verticesIndex + 0);

        //back
        mesh.Indices[indicesIndex * 30 + 24] = (uint)(verticesIndex + 7);
        mesh.Indices[indicesIndex * 30 + 25] = (uint)(verticesIndex + 6);
        mesh.Indices[indicesIndex * 30 + 26] = (uint)(verticesIndex + 2);
        mesh.Indices[indicesIndex * 30 + 27] = (uint)(verticesIndex + 7);
        mesh.Indices[indicesIndex * 30 + 28] = (uint)(verticesIndex + 2);
        mesh.Indices[indicesIndex * 30 + 29] = (uint)(verticesIndex + 3);

        indicesIndex++;
    }

    public unsafe Mesh CreateSeaLevelMesh()
    {
        Mesh mesh = new();
        AllocateMeshData(&mesh, 4, 2);

        int indicesIndex = 0;
        int verticesIndex = 0;
        AddQuadIndices(mesh, ref indicesIndex, verticesIndex);
        AddVertex(mesh, ref verticesIndex, new Vector3(0, 0, 0), Color.White, Vector3.UnitZ, 0, 0);
        AddVertex(mesh, ref verticesIndex, new Vector3((int)myMapGenerationConfiguration.HeightMapSideLength, 0, 0), Color.White, Vector3.UnitZ, 0, 0);
        AddVertex(mesh, ref verticesIndex, new Vector3(0, (int)myMapGenerationConfiguration.HeightMapSideLength, 0), Color.White, Vector3.UnitZ, 0, 0);
        AddVertex(mesh, ref verticesIndex, new Vector3((int)myMapGenerationConfiguration.HeightMapSideLength, (int)myMapGenerationConfiguration.HeightMapSideLength, 0), Color.White, Vector3.UnitZ, 0, 0);

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
        mesh->AllocTexCoords();
        mesh->AllocIndices();
    }

    private unsafe void AddQuadIndices(Mesh mesh, ref int indicesIndex, int verticesIndex)
    {
        mesh.Indices[indicesIndex * 6 + 0] = (uint)verticesIndex;
        mesh.Indices[indicesIndex * 6 + 1] = (uint)(verticesIndex + 1);
        mesh.Indices[indicesIndex * 6 + 2] = (uint)(verticesIndex + 3);
        mesh.Indices[indicesIndex * 6 + 3] = (uint)verticesIndex;
        mesh.Indices[indicesIndex * 6 + 4] = (uint)(verticesIndex + 3);
        mesh.Indices[indicesIndex * 6 + 5] = (uint)(verticesIndex + 2);

        indicesIndex++;
    }

    private unsafe void AddVertex(Mesh mesh, ref int verticesIndex, Vector3 position, Color color, Vector3 normal, float heightMapIndex, float cubeFace)
    {
        mesh.Vertices[verticesIndex * 3 + 0] = position.X;
        mesh.Vertices[verticesIndex * 3 + 1] = position.Y;
        mesh.Vertices[verticesIndex * 3 + 2] = position.Z;

        mesh.Colors[verticesIndex * 4 + 0] = color.R;
        mesh.Colors[verticesIndex * 4 + 1] = color.G;
        mesh.Colors[verticesIndex * 4 + 2] = color.B;
        mesh.Colors[verticesIndex * 4 + 3] = color.A;

        mesh.Normals[verticesIndex * 3 + 0] = normal.X;
        mesh.Normals[verticesIndex * 3 + 1] = normal.Y;
        mesh.Normals[verticesIndex * 3 + 2] = normal.Z;

        mesh.TexCoords[verticesIndex * 2 + 0] = heightMapIndex;
        mesh.TexCoords[verticesIndex * 2 + 1] = cubeFace;

        verticesIndex++;
    }
}
