using System.Numerics;

namespace ProceduralLandscapeGeneration
{
    internal class HeightMap
    {
        private const float Scale = 60.0f;

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

            Vector3 normal = new Vector3(0.15f) * Vector3.Normalize(new Vector3(Scale * (Data[x, y] - Data[x + 1, y]), 0.0f, 1.0f));
            normal += new Vector3(0.15f) * Vector3.Normalize(new Vector3(Scale * (Data[x - 1, y] - Data[x, y]), 0.0f, 1.0f));
            normal += new Vector3(0.15f) * Vector3.Normalize(new Vector3(0.0f, Scale * (Data[x, y] - Data[x, y + 1]), 1.0f));
            normal += new Vector3(0.15f) * Vector3.Normalize(new Vector3(0.0f, Scale * (Data[x, y - 1] - Data[x, y]), 1.0f));

            var squareRootOf2 = MathF.Sqrt(2);
            normal += new Vector3(0.1f) * Vector3.Normalize(new Vector3(Scale * (Data[x, y] - Data[x + 1, y + 1]) / squareRootOf2, Scale * (Data[x, y] - Data[x + 1, y + 1]) / squareRootOf2, squareRootOf2));
            normal += new Vector3(0.1f) * Vector3.Normalize(new Vector3(Scale * (Data[x, y] - Data[x + 1, y - 1]) / squareRootOf2, Scale * (Data[x, y] - Data[x + 1, y - 1]) / squareRootOf2, squareRootOf2));
            normal += new Vector3(0.1f) * Vector3.Normalize(new Vector3(Scale * (Data[x, y] - Data[x - 1, y + 1]) / squareRootOf2, Scale * (Data[x, y] - Data[x - 1, y + 1]) / squareRootOf2, squareRootOf2));
            normal += new Vector3(0.1f) * Vector3.Normalize(new Vector3(Scale * (Data[x, y] - Data[x - 1, y - 1]) / squareRootOf2, Scale * (Data[x, y] - Data[x - 1, y - 1]) / squareRootOf2, squareRootOf2));

            return normal;
        }
    }
}
