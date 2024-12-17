using Raylib_CsLo;
using System.Numerics;

namespace ProceduralLandscapeGeneration
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
            float height = heightMap.Value[x, y].Height * Configuration.HeightMultiplier;
            mesh.vertices[vertexIndex * 3 + 0] = x;
            mesh.vertices[vertexIndex * 3 + 1] = y;
            mesh.vertices[vertexIndex * 3 + 2] = height;


            Vector3 normal = heightMap.GetScaledNormal(x, y);
            mesh.normals[vertexIndex * 3 + 0] = normal.X;
            mesh.normals[vertexIndex * 3 + 1] = normal.Y;
            mesh.normals[vertexIndex * 3 + 2] = normal.Z;

            Color color = heightMap.Value[x, y].Type.GetColor();
            mesh.colors[vertexIndex * 4 + 0] = color.r;
            mesh.colors[vertexIndex * 4 + 1] = color.g;
            mesh.colors[vertexIndex * 4 + 2] = color.b;
            mesh.colors[vertexIndex * 4 + 3] = color.a;

            vertexIndex++;
        }

        private static unsafe void AllocateMeshData(Mesh* mesh, int triangleCount)
        {
            mesh->vertexCount = triangleCount * 3;
            mesh->triangleCount = triangleCount;

            mesh->vertices = (float*)Raylib.MemAlloc((uint)(mesh->vertexCount * 3 * sizeof(float)));
            mesh->normals = (float*)Raylib.MemAlloc((uint)(mesh->vertexCount * 3 * sizeof(float)));
            mesh->colors = (byte*)Raylib.MemAlloc((uint)(mesh->vertexCount * 4 * sizeof(byte)));

            mesh->indices = null;
        }
    }
}
