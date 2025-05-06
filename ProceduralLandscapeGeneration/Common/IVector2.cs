using System.Numerics;
using System.Runtime.InteropServices;

namespace ProceduralLandscapeGeneration.Common;

[StructLayout(LayoutKind.Sequential)]
internal struct IVector2
{
    public int X { get; set; }
    public int Y { get; set; }

    public IVector2(Vector2 value) : this(value.X, value.Y) { }
    public IVector2(float i) : this(i, i) { }
    public IVector2(float x, float y) : this((int)x, (int)y) { }

    public IVector2(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static float Dot(IVector2 value1, IVector2 value2)
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

    public static IVector2 operator +(IVector2 left, IVector2 right)
    {
        return new IVector2(left.X + right.X, left.Y + right.Y);
    }

    public static IVector2 operator *(IVector2 left, float right)
    {
        return left * new IVector2(right);
    }

    public static IVector2 operator *(float left, IVector2 right)
    {
        return right * left;
    }

    public static IVector2 operator *(IVector2 left, IVector2 right)
    {
        return new IVector2(
            left.X * right.X,
            left.Y * right.Y
        );
    }
}
