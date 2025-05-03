using ProceduralLandscapeGeneration.Common;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Simulation.CPU.ClusterConvection;

//https://nickmcd.me/2020/12/03/clustered-convection-for-simulating-plate-tectonics/
internal class Plate
{

    private const float Convection = 10.0f;
    private const float Growth = 0.05f;

    public Vector2 Position { get; private set; }
    private Vector2 mySpeed = Vector2.Zero;
    private float myRotation = 0.0f;
    private float myAngularVelocity = 0.0f;
    private float myMass = 0.0f;
    private float myArea = 0.0f;
    private float myInertia = 0.0f;
    private float myHeight = 0.0f;

    public List<Segment> Segments { get; private set; }

    public Plate(Vector2 position)
    {
        Position = position;

        Segments = new List<Segment>();
    }

    public void Recenter()
    {
        Position = Vector2.Zero;
        myHeight = 0.0f;
        myInertia = 0.0f;
        myMass = 0.0f;
        myArea = 0.0f;

        foreach (Segment segment in Segments)
        {
            Position += segment.Position;
            myMass += segment.Mass;
            myArea += segment.Area;
            myInertia += MathF.Pow((Position - segment.Position).Length(), 2) * (segment.Mass);
            myHeight += segment.Height;
        }

        Position /= Segments.Count;
        myHeight /= Segments.Count;
    }

    public void Update(HeightMap heatMap, int size)
    {
        //Convect Segments

        //Reset Plate Acceleration and Torque
        Vector2 acc = Vector2.Zero;
        float torque = 0.0f;

        //Compute Individual Segment Forces
        foreach (Segment segment in Segments)
        {
            Vector2 f = Force(new IVector2(segment.Position), heatMap, size);   //Force applied at Segment
            Vector2 dir = segment.Position - Position;        //Vector from Plate Center to Segment

            //Compute Acceleration and Torque
            //"convection" is a scaling parameter
            acc -= Convection * f;
            torque -= Convection * dir.Length() * f.Length() * MathF.Sin(Angle(f) - Angle(dir));

        }

        const float DT = 0.025f;

        //Apply Laws of Motion to Plate
        mySpeed += DT * acc / myMass;
        myAngularVelocity += DT * torque / myInertia;
        Position += DT * mySpeed;
        myRotation += DT * myAngularVelocity;

        //Clamp Angle
        if (myRotation > 2 * MathF.PI) myRotation -= 2 * MathF.PI;
        if (myRotation < 0) myRotation += 2 * MathF.PI;

        //Re-Apply Resulting Motion to Segments
        for (int segment = 0; segment < Segments.Count; segment++)
        {

            //Translation
            Vector2 dir = Segments[segment].Position - (Position - DT * mySpeed);

            //Rotation Angle
            float _angle = Angle(dir) - (myRotation - DT * myAngularVelocity);

            Vector2 effvec = dir.Length() * new Vector2(MathF.Cos(myRotation + _angle), MathF.Sin(myRotation + _angle));

            //Apply Motion
            Segments[segment].Speed = Position + effvec - Segments[segment].Position;
            Segments[segment].Position = Position + effvec;
        }
    }

    private Vector2 Force(IVector2 i, HeightMap heatMap, int size)
    {
        float fx = 0.0f;
        float fy = 0.0f;

        if (i.X > 0 && i.X < size - 2 && i.Y > 0 && i.Y < size - 2)
        {
            fx = (heatMap.Height[i.X + 1, i.Y] - heatMap.Height[i.X - 1, i.Y]) / 2.0f;
            fy = -(heatMap.Height[i.X, i.Y + 1] - heatMap.Height[i.X, i.Y - 1]) / 2.0f;
        }

        //Out-of-Bounds
        if (i.X <= 0) fx = 0.0f;
        else if (i.X >= size - 1) fx = -0.0f;
        if (i.Y <= 0) fy = 0.0f;
        else if (i.Y >= size - 1) fy = -0.0f;

        return new Vector2(fx, fy);
    }

    float Angle(Vector2 d)
    {
        if (d.X == 0 && d.Y == 0) return 0.0f;
        if (d.X == 0 && d.Y > 0) return MathF.PI / 2.0f;
        if (d.X == 0 && d.Y < 0) return 3.0f * MathF.PI / 2.0f;

        float a = 2.0f * MathF.PI + MathF.Atan(d.Y / d.X);

        if (d.X < 0) a += MathF.PI;

        return a;
    }
}
