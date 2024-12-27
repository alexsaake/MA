using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration
{
    internal class HeightMap
    {
        public float[,] Height { get; }

        public int Width => Height.GetLength(0);
        public int Depth => Height.GetLength(1);

        public HeightMap(float[,] heightMap)
        {
            Height = heightMap;
        }

        public HeightMap(float[] heightMap) : this(Get2DHeightMapFrom1D(heightMap)) {}

        private static float[,] Get2DHeightMapFrom1D(float[] heightMap1D)
        {
            int size = (int)MathF.Sqrt(heightMap1D.Length);
            float[,] heightMap2D = new float[size, size];
            for (int i = 0; i < heightMap1D.Length; i++)
            {
                int x = i % size;
                int y = i / size;
                heightMap2D[x, y] = heightMap1D[i];
            }

            return heightMap2D;
        }

        public unsafe HeightMap(uint heightMapShaderBufferId, uint size)
        {
            uint heightMapSize = size * size;
            uint heightMapBufferSize = heightMapSize * sizeof(float);
            float[] heightMapValues = new float[heightMapSize];
            fixed (float* heightMapValuesPointer = heightMapValues)
            {
                Rlgl.ReadShaderBuffer(heightMapShaderBufferId, heightMapValuesPointer, heightMapBufferSize, 0);
            }

            Height = Get2DHeightMapFrom1D(heightMapValues);
        }

        public Vector3 GetNormal(IVector2 position)
        {
            return GetScaledNormal(position.X, position.Y, 1);
        }

        public Vector3 GetScaledNormal(int x, int y)
        {
            return GetScaledNormal(x, y, Configuration.HeightMultiplier);
        }

        private Vector3 GetScaledNormal(int x, int y, int scale)
        {
            if (x < 1 || x > Width - 2
                || y < 1 || y > Depth - 2)
            {
                return new Vector3(0, 0, 1);
            }

            Vector3 normal = new(
            scale * -(Height[x + 1, y - 1] - Height[x - 1, y - 1] + 2 * (Height[x + 1, y] - Height[x - 1, y]) + Height[x + 1, y + 1] - Height[x - 1, y + 1]),
            scale * -(Height[x - 1, y + 1] - Height[x - 1, y - 1] + 2 * (Height[x, y + 1] - Height[x, y - 1]) + Height[x + 1, y + 1] - Height[x + 1, y - 1]),
            1.0f);
            normal = Vector3.Normalize(normal);

            return normal;
        }

        public bool IsOutOfBounds(Vector2 position)
        {
            return IsOutOfBounds((int)position.X, (int)position.Y);
        }

        public bool IsOutOfBounds(IVector2 position)
        {
            return IsOutOfBounds(position.X, position.Y);
        }

        public bool IsOutOfBounds(int x, int y)
        {
            return x < 0
                    || x > Width - 1
                    || y < 0
                    || y > Depth - 1;
        }
    }
}
