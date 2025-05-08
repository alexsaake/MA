using System.Numerics;
using System.Runtime.InteropServices;

namespace ProceduralLandscapeGeneration.Common;

[StructLayout(LayoutKind.Sequential)]
internal struct IntVector2
{
    public int X { get; set; }
    public int Y { get; set; }

    public IntVector2(Vector2 value) : this(value.X, value.Y) { }
    public IntVector2(float i) : this(i, i) { }
    public IntVector2(float x, float y) : this((int)x, (int)y) { }

    public IntVector2(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static float Dot(IntVector2 value1, IntVector2 value2)
    {
        return (value1.X * value2.X)
             + (value1.Y * value2.Y);
    }

    public readonly float Length()
    {
        float lengthSquared = LengthSquared();
        return MathF.Sqrt(lengthSquared);
    }

    public readonly float LengthSquared()
    {
        return Dot(this, this);
    }

    public static IntVector2 operator +(IntVector2 left, IntVector2 right)
    {
        return new IntVector2(left.X + right.X, left.Y + right.Y);
    }

    public static IntVector2 operator *(IntVector2 left, float right)
    {
        return left * new IntVector2(right);
    }

    public static IntVector2 operator *(float left, IntVector2 right)
    {
        return right * left;
    }

    public static IntVector2 operator *(IntVector2 left, IntVector2 right)
    {
        return new IntVector2(
            left.X * right.X,
            left.Y * right.Y
        );
    }
}
