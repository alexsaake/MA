using System.Numerics;

namespace ProceduralLandscapeGeneration
{
    internal class HeightMap
    {
        public float[,] Data { get; }

        public int Width => Data.GetLength(0);
        public int Height => Data.GetLength(1);

        public HeightMap(float[,] noiseMap)
        {
            Data = noiseMap;
        }

        public Vector3 GetNormal(int x, int y)
        {
            if (x < 1 || x > Width - 2
                || y < 1 || y > Height - 2)
            {
                return new Vector3(0, 0, 1);
            }

            Vector3 normal = new Vector3(0.15f) * Vector3.Normalize(new Vector3(Configuration.HeightMultiplier * (Data[x, y] - Data[x + 1, y]), 0.0f, 1.0f));
            normal += new Vector3(0.15f) * Vector3.Normalize(new Vector3(Configuration.HeightMultiplier * (Data[x - 1, y] - Data[x, y]), 0.0f, 1.0f));
            normal += new Vector3(0.15f) * Vector3.Normalize(new Vector3(0.0f, Configuration.HeightMultiplier * (Data[x, y] - Data[x, y + 1]), 1.0f));
            normal += new Vector3(0.15f) * Vector3.Normalize(new Vector3(0.0f, Configuration.HeightMultiplier * (Data[x, y - 1] - Data[x, y]), 1.0f));

            var squareRootOf2 = MathF.Sqrt(2);
            normal += new Vector3(0.1f) * Vector3.Normalize(new Vector3(Configuration.HeightMultiplier * (Data[x, y] - Data[x + 1, y + 1]) / squareRootOf2, Configuration.HeightMultiplier * (Data[x, y] - Data[x + 1, y + 1]) / squareRootOf2, squareRootOf2));
            normal += new Vector3(0.1f) * Vector3.Normalize(new Vector3(Configuration.HeightMultiplier * (Data[x, y] - Data[x + 1, y - 1]) / squareRootOf2, Configuration.HeightMultiplier * (Data[x, y] - Data[x + 1, y - 1]) / squareRootOf2, squareRootOf2));
            normal += new Vector3(0.1f) * Vector3.Normalize(new Vector3(Configuration.HeightMultiplier * (Data[x, y] - Data[x - 1, y + 1]) / squareRootOf2, Configuration.HeightMultiplier * (Data[x, y] - Data[x - 1, y + 1]) / squareRootOf2, squareRootOf2));
            normal += new Vector3(0.1f) * Vector3.Normalize(new Vector3(Configuration.HeightMultiplier * (Data[x, y] - Data[x - 1, y - 1]) / squareRootOf2, Configuration.HeightMultiplier * (Data[x, y] - Data[x - 1, y - 1]) / squareRootOf2, squareRootOf2));

            return normal;
        }

        public HeightMap GetPart(int xFrom, int xTo, int yFrom, int yTo)
        {
            float[,] dataPart = new float[xTo - xFrom, yTo - yFrom];
            int xSize = xTo - xFrom;
            int ySize = yTo - yFrom;

            for (int x = 0; x < xSize; x++)
            {
                for (int y = 0; y < ySize; y++)
                {
                    dataPart[x, y] = Data[x + xFrom, y + yFrom];
                }
            }

            return new HeightMap(dataPart);
        }

        public bool IsOutOfBounds(Vector2 position)
        {
            return (int)position.X < 0
                    || (int)position.X > Width - 1
                    || (int)position.Y < 0
                    || (int)position.Y > Height - 1;
        }
    }
}
