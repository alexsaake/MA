using System.Numerics;
using System.Runtime.InteropServices;

namespace ProceduralLandscapeGeneration
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct IVector2
    {
        public int X { get; set; }
        public int Y { get; set; }

        public IVector2(Vector2 value) : this(value.X, value.Y) { }
        public IVector2(float x, float y) : this((int)x, (int)y) { }

        public IVector2(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}
