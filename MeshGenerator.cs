using Raylib_CsLo;
using System.Numerics;

namespace ProceduralLandscapeGeneration
{
    internal class MeshGenerator : IMeshGenerator
    {
        public unsafe Dictionary<Vector3, Mesh> GenerateChunkMeshes(HeightMap heightMap)
        {
            Dictionary<Vector3, Mesh> chunkMeshes = new Dictionary<Vector3, Mesh>();

            int dataPerChunk = (int)Math.Sqrt(Configuration.MaximumModelVertices);
            int xChunks = (int)MathF.Ceiling((float)heightMap.Width / dataPerChunk);
            int yChunks = (int)MathF.Ceiling((float)heightMap.Height / dataPerChunk);

            for (int xChunk = 0; xChunk < xChunks; xChunk++)
            {
                for (int yChunk = 0; yChunk < yChunks; yChunk++)
                {
                    HeightMap heightMapPart = heightMap.GetPart(xChunk * dataPerChunk, xChunk * dataPerChunk + dataPerChunk, yChunk * dataPerChunk, yChunk * dataPerChunk + dataPerChunk);

                    float topLeftX = (heightMapPart.Width - 1) / -2f;
                    float topLeftZ = (heightMapPart.Height - 1) / 2f;

                    Mesh mesh = new();
                    int triangles = (heightMapPart.Width - 1) * (heightMapPart.Height - 1) * 6;
                    AllocateMeshData(&mesh, triangles);

                    ushort vertexIndex = 0;

                    for (int y = 0; y < heightMapPart.Height; y++)
                    {
                        for (int x = 0; x < heightMapPart.Width; x++)
                        {
                            mesh.vertices[vertexIndex * 3 + 0] = topLeftX + x;
                            mesh.vertices[vertexIndex * 3 + 1] = heightMapPart.Data[x, y] * Configuration.HeightMultiplier;
                            mesh.vertices[vertexIndex * 3 + 2] = topLeftZ - y;
                            Vector3 normal = heightMapPart.GetNormal(x, y);
                            mesh.normals[vertexIndex * 3 + 0] = normal.X;
                            mesh.normals[vertexIndex * 3 + 1] = normal.Y;
                            mesh.normals[vertexIndex * 3 + 2] = normal.Z;

                            if (x < heightMapPart.Width - 1 && y < heightMapPart.Height - 1)
                            {
                                mesh.indices[vertexIndex * 6 + 0] = vertexIndex;
                                mesh.indices[vertexIndex * 6 + 1] = (ushort)(vertexIndex + heightMapPart.Width + 1);
                                mesh.indices[vertexIndex * 6 + 2] = (ushort)(vertexIndex + heightMapPart.Width);
                                mesh.indices[vertexIndex * 6 + 3] = (ushort)(vertexIndex + heightMapPart.Width + 1);
                                mesh.indices[vertexIndex * 6 + 4] = vertexIndex;
                                mesh.indices[vertexIndex * 6 + 5] = (ushort)(vertexIndex + 1);
                            }

                            vertexIndex++;
                        }
                    }

                    Raylib.UploadMesh(&mesh, false);

                    chunkMeshes.Add(new Vector3(xChunk * (heightMapPart.Width - 1), 0, -yChunk * (heightMapPart.Height - 1)), mesh);
                }
            }

            return chunkMeshes;
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
