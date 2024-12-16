using System.Numerics;

namespace ProceduralLandscapeGeneration
{
    internal struct Vector2Int
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Vector2Int(Vector2 value) : this(value.X, value.Y) { }
        public Vector2Int(float x, float y) : this((int)x, (int)y) { }

        public Vector2Int(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}
