using Raylib_CsLo;
using System.Numerics;

namespace ProceduralLandscapeGeneration
{
    internal class MeshGenerator : IMeshGenerator
    {
        public unsafe Mesh GenerateTerrainMesh(HeightMap heightMap, float heightMultiplier)
        {
            float topLeftX = (heightMap.Width - 1) / -2f;
            float topLeftZ = (heightMap.Height - 1) / 2f;

            Mesh mesh = new();
            int triangles = (heightMap.Width - 1) * (heightMap.Height - 1) * 6;
            AllocateMeshData(&mesh, triangles);

            ushort vertexIndex = 0;

            for (int y = 0; y < heightMap.Height; y++)
            {
                for (int x = 0; x < heightMap.Width; x++)
                {
                    mesh.vertices[vertexIndex * 3 + 0] = topLeftX + x;
                    mesh.vertices[vertexIndex * 3 + 1] = heightMap.Data[x, y] * heightMultiplier;
                    mesh.vertices[vertexIndex * 3 + 2] = topLeftZ - y;
                    Vector3 normal = heightMap.GetNormal(x, y);
                    mesh.normals[vertexIndex * 3 + 0] = normal.X;
                    mesh.normals[vertexIndex * 3 + 1] = normal.Y;
                    mesh.normals[vertexIndex * 3 + 2] = normal.Z;

                    if (x < heightMap.Width - 1 && y < heightMap.Height - 1)
                    {
                        mesh.indices[vertexIndex * 6 + 0] = vertexIndex;
                        mesh.indices[vertexIndex * 6 + 1] = (ushort)(vertexIndex + heightMap.Width + 1);
                        mesh.indices[vertexIndex * 6 + 2] = (ushort)(vertexIndex + heightMap.Width);
                        mesh.indices[vertexIndex * 6 + 3] = (ushort)(vertexIndex + heightMap.Width + 1);
                        mesh.indices[vertexIndex * 6 + 4] = vertexIndex;
                        mesh.indices[vertexIndex * 6 + 5] = (ushort)(vertexIndex + 1);
                    }

                    vertexIndex++;
                }
            }

            Raylib.UploadMesh(&mesh, false);

            return mesh;
        }

        private static unsafe void AllocateMeshData(Mesh* mesh, int triangleCount)
        {
            mesh->vertexCount = triangleCount * 3;
            mesh->triangleCount = triangleCount;

            mesh->vertices = (float*)Raylib.MemAlloc((uint)(mesh->vertexCount * 3 * sizeof(float)));
            mesh->normals = (float*)Raylib.MemAlloc((uint)(mesh->vertexCount * 3 * sizeof(float)));

            mesh->indices = (ushort*)Raylib.MemAlloc((uint)(mesh->vertexCount * 3 * sizeof(ushort)));
        }
    }
}
