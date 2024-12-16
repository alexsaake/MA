using System.Numerics;

namespace ProceduralLandscapeGeneration
{
    internal class HeightMap
    {
        public Soil[,] Value { get; }

        public int Width => Value.GetLength(0);
        public int Height => Value.GetLength(1);

        public HeightMap(Soil[,] noiseMap)
        {
            Value = noiseMap;
        }

        public Vector3 GetNormal(Vector2Int position)
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
                || y < 1 || y > Height - 2)
            {
                return new Vector3(0, 0, 1);
            }

            Vector3 normal = new(
            scale * -(Value[x + 1, y - 1].Height - Value[x - 1, y - 1].Height + 2 * (Value[x + 1, y].Height - Value[x - 1, y].Height) + Value[x + 1, y + 1].Height - Value[x - 1, y + 1].Height),
            scale * -(Value[x - 1, y + 1].Height - Value[x - 1, y - 1].Height + 2 * (Value[x, y + 1].Height - Value[x, y - 1].Height) + Value[x + 1, y + 1].Height - Value[x + 1, y - 1].Height),
            1.0f);
            normal = Vector3.Normalize(normal);

            return normal;
        }

        public HeightMap GetHeightMapPart(int xFrom, int xTo, int yFrom, int yTo)
        {
            Soil[,] dataPart = new Soil[xTo - xFrom, yTo - yFrom];
            int xSize = xTo - xFrom;
            int ySize = yTo - yFrom;

            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    dataPart[x, y] = Value[x + xFrom, y + yFrom];
                }
            }

            return new HeightMap(dataPart);
        }

        public bool IsOutOfBounds(Vector2 position)
        {
            return IsOutOfBounds((int)position.X, (int)position.Y);
        }

        public bool IsOutOfBounds(Vector2Int position)
        {
            return IsOutOfBounds(position.X, position.Y);
        }

        public bool IsOutOfBounds(int x, int y)
        {
            return x < 0
                    || x > Width - 1
                    || y < 0
                    || y > Height - 1;
        }
    }
}
