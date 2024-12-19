using System.Numerics;

namespace ProceduralLandscapeGeneration
{
    internal class HeightMap
    {
        public float[,] Height { get; }

        public int Width => Height.GetLength(0);
        public int Depth => Height.GetLength(1);

        public HeightMap(float[,] noiseMap)
        {
            Height = noiseMap;
        }

        public HeightMap(float[] noiseMap)
        {
            int size = (int)MathF.Sqrt(noiseMap.Length);
            Height = new float[size, size];
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    Height[x, y] = noiseMap[x + y];
                }
            }
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
