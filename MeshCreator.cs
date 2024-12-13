using Raylib_CsLo;
using System.Numerics;

namespace ProceduralLandscapeGeneration
{
    internal class MeshCreator : IMeshCreator
    {
        public unsafe Dictionary<Vector3, Mesh> GenerateChunkMeshes(HeightMap heightMap)
        {
            Dictionary<Vector3, Mesh> chunkMeshes = new Dictionary<Vector3, Mesh>();

            int maximumDataPerChunk = (int)Math.Sqrt(Configuration.MaximumModelVertices);
            int xChunks = (int)MathF.Floor((float)heightMap.Width / maximumDataPerChunk);
            int yChunks = (int)MathF.Floor((float)heightMap.Height / maximumDataPerChunk);

            for (int xChunk = 0; xChunk < xChunks; xChunk++)
            {
                for (int yChunk = 0; yChunk < yChunks; yChunk++)
                {
                    int currentChunkXFrom = xChunk * maximumDataPerChunk;
                    int currentChunkXTo = xChunk * maximumDataPerChunk + maximumDataPerChunk;
                    int currentChunkYFrom = yChunk * maximumDataPerChunk;
                    int currentChunkYTo = yChunk * maximumDataPerChunk + maximumDataPerChunk;
                    HeightMap heightMapPart = heightMap.GetHeightMapPart(currentChunkXFrom, currentChunkXTo, currentChunkYFrom, currentChunkYTo);

                    Mesh mesh = new();
                    int triangles = (heightMapPart.Width - 1) * (heightMapPart.Height - 1) * 6;
                    AllocateMeshData(&mesh, triangles);

                    ushort vertexIndex = 0;

                    for (int y = 0; y < heightMapPart.Height; y++)
                    {
                        for (int x = 0; x < heightMapPart.Width; x++)
                        {
                            mesh.vertices[vertexIndex * 3 + 0] = x;
                            mesh.vertices[vertexIndex * 3 + 1] = y;
                            mesh.vertices[vertexIndex * 3 + 2] = heightMapPart.Data[x, y] * Configuration.HeightMultiplier;
                            Vector3 normal = heightMapPart.GetScaledNormal(x, y);
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

                    Vector3 position = new Vector3(xChunk * (heightMapPart.Width - 1), yChunk * (heightMapPart.Height - 1), 0);
                    chunkMeshes.Add(position, mesh);
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
