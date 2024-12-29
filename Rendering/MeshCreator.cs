using ProceduralLandscapeGeneration.Common;
using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Rendering
{
    internal class MeshCreator : IMeshCreator
    {
        public unsafe Mesh CreateMesh(HeightMap heightMap)
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

        private unsafe void AddVertex(Mesh mesh, ref int vertexIndex, HeightMap heightMap, int x, int y)
        {
            float height = heightMap.Height[x, y] * Configuration.HeightMultiplier;
            mesh.Vertices[vertexIndex * 3 + 0] = x;
            mesh.Vertices[vertexIndex * 3 + 1] = y;
            mesh.Vertices[vertexIndex * 3 + 2] = height;


            Vector3 normal = heightMap.GetScaledNormal(x, y);
            mesh.Normals[vertexIndex * 3 + 0] = normal.X;
            mesh.Normals[vertexIndex * 3 + 1] = normal.Y;
            mesh.Normals[vertexIndex * 3 + 2] = normal.Z;

            vertexIndex++;
        }

        private static unsafe void AllocateMeshData(Mesh* mesh, int triangleCount)
        {
            mesh->VertexCount = triangleCount * 3;
            mesh->TriangleCount = triangleCount;

            mesh->Vertices = (float*)Raylib.MemAlloc((uint)(mesh->VertexCount * 3 * sizeof(float)));
            mesh->Normals = (float*)Raylib.MemAlloc((uint)(mesh->VertexCount * 3 * sizeof(float)));

            mesh->Indices = null;
        }
    }
}
