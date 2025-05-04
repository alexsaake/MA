using ProceduralLandscapeGeneration.Common;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Simulation.CPU.PlateTectonics;

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
            myInertia += MathF.Pow((Position - segment.Position).Length(), 2) * segment.Mass;
            myHeight += segment.Height;
        }

        Position /= Segments.Count;
        myHeight /= Segments.Count;
    }

    public void Update(HeightMap heatMap, List<Segment> allSegments, uint size)
    {
        Vector2 acc = Vector2.Zero;
        float torque = 0.0f;

        //Collide

        for (int segment = 0; segment < Segments.Count; segment++)
        {

            IVector2 ipos = new IVector2(Segments[segment].Position);

            if (ipos.X >= size || ipos.X < 0 ||
                ipos.Y >= size || ipos.Y < 0)
            {
                Segments[segment].IsAlive = false;
                continue;
            }

            const float collisionRadius = 1f;
            const float subductionHeating = 0.1f;
            int n = 12;
            for (int j = 0; j < n; j++)
            {
                IVector2 scanIntegerPosition = new IVector2(Segments[segment].Position);
                scanIntegerPosition += size * collisionRadius * new IVector2(MathF.Cos(j / n * 2.0f * MathF.PI), MathF.Sin(j / n * 2.0f * MathF.PI));

                if (scanIntegerPosition.X >= size || scanIntegerPosition.X < 0 ||
                    scanIntegerPosition.Y >= size || scanIntegerPosition.Y < 0) continue;

                Segment? collidingSegment = allSegments.SingleOrDefault(segment => new IVector2(segment.Position).Equals(scanIntegerPosition));
                if (collidingSegment is null)
                {
                    continue;
                }
                if (Segments[segment].Parent! == collidingSegment.Parent)
                {
                    continue;
                }

                //Two Segments are Colliding, Subduce the Denser One
                if (Segments[segment].Density > collidingSegment.Density && !collidingSegment.IsColliding)
                {

                    float mdiff = Segments[segment].Height * Segments[segment].Density * Segments[segment].Area;
                    float hdiff = Segments[segment].Height;

                    collidingSegment.Thickness += hdiff;  //Move Mass
                    collidingSegment.Mass += mdiff;
                    collidingSegment.Buoyancy();

                    collidingSegment.IsColliding = true;
                    Segments[segment].IsAlive = false;
                    heatMap.Height[scanIntegerPosition.X, scanIntegerPosition.Y] += subductionHeating;
                }
            }
        }

        //Cascade
        //for (auto & s: cluster.segs)
        //{
        //    if (!s->colliding) continue;
        //    int n = 24;
        //    for (int j = 0; j < n; j++)
        //    {

        //        vec2 scan = *(s->pos);
        //        scan += SIZE * R * vec2(cos((float)j / (float)n * 2.0f * PI), sin((float)j / (float)n * 2.0f * PI));

        //        if (scan.x >= SIZE || scan.x < 0 ||
        //            scan.y >= SIZE || scan.y < 0) continue;

        //        int segind = cluster.sample(scan);
        //        if (segind < 0) continue;        //Non-Index (Blank Space)

        //        Litho* n = cluster.segs[segind];
        //        if (s == n) continue;   //Same Segment

        //        //    if(n->colliding) continue;


        //        float hdiff = s->height - n->height;

        //        hdiff -= 0.01;
        //        if (hdiff < 0) continue;

        //        float mdiff = hdiff * s->density * s->area;

        //        float trate = 0.2;

        //        n->thickness += 0.5 * trate * hdiff;  //Move Mass
        //        s->thickness -= 0.5 * trate * hdiff;
        //        n->mass += 0.5 * trate * mdiff;  //Move Mass
        //        s->mass -= 0.5 * trate * mdiff;

        //        n->buoyancy();
        //        s->buoyancy();

        //    }

        //    s->colliding = false;

        //}

        //Grow

        for (int segment = 0; segment < Segments.Count; segment++)
        {

            if (!Segments[segment].IsAlive) continue;

            IVector2 ip = new IVector2(Segments[segment].Position);
            float nd = heatMap.Height[ip.X, ip.Y];              //Heat Value!


            //LINEAR GROWTH RATE [m / s]
            float G = Growth * (1.0f - nd) * (1.0f - nd - Segments[segment].Density * Segments[segment].Thickness);
            if (G < 0.0f) G *= 0.05f;  //Dissolution Rate

            //COMPUTE EQUILIBRIUM DENSITY (PER-VOLUME)
            float D = Langmuir(3.0f, 1.0f - nd);

            Segments[segment].Mass += Segments[segment].Area * G * D; //m^2 * m / s * kg / m^3 = kg
            Segments[segment].Thickness += G; //New Thickness

            Segments[segment].Density = Segments[segment].Mass / (Segments[segment].Area * Segments[segment].Thickness);
            Segments[segment].Height = Segments[segment].Thickness * (1.0f - Segments[segment].Density);

        }

        //Convect

        foreach (Segment segment in Segments)
        {

            Vector2 f = Force(new IVector2(segment.Position), heatMap, size);
            Vector2 dir = segment.Position - Position;

            acc -= Convection * f;
            torque -= Convection * dir.Length() * f.Length() * MathF.Sin(Angle(f) - Angle(dir));

        }

        const float DT = 0.025f;

        mySpeed += DT * acc / myMass;
        if(myInertia == 0)
        {
            return;
        }
        myAngularVelocity += 100000f * DT * torque / myInertia;
        Position += DT * mySpeed;
        myRotation += DT * myAngularVelocity;

        if (myRotation > 2 * MathF.PI) myRotation -= 2 * MathF.PI;
        if (myRotation < 0) myRotation += 2 * MathF.PI;

        for (int segment = 0; segment < Segments.Count; segment++)
        {

            Vector2 dir = Segments[segment].Position - (Position - DT * mySpeed);
            float _angle = Angle(dir) - (myRotation - DT * myAngularVelocity);

            Vector2 newPosition = Position + dir.Length() * new Vector2(MathF.Cos(myRotation + _angle), MathF.Sin(myRotation + _angle));
            Segments[segment].Speed = newPosition - Segments[segment].Position;
            Segments[segment].Position = newPosition;

        }
    }

    private static float Langmuir(float k, float x)
    {
        return k * x / (1.0f + k * x);
    }

    private Vector2 Force(IVector2 i, HeightMap heatMap, uint size)
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
