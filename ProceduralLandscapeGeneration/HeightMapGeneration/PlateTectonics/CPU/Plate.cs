using ProceduralLandscapeGeneration.Common;
using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations.HeightMapGeneration;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.HeightMapGeneration.PlateTectonics.CPU;

//https://nickmcd.me/2020/12/03/clustered-convection-for-simulating-plate-tectonics/
internal class Plate
{
    private const float Convection = 10.0f;
    private const float Growth = 0.05f;

    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IShaderBuffers myShaderBuffers;

    public Vector2 Position { get; private set; }

    private Vector2 mySpeed = Vector2.Zero;
    private float myRotation = 0.0f;
    private float myAngularVelocity = 0.0f;
    private float myMass = 0.0f;
    private float myInertia = 0.0f;

    public List<Segment> Segments { get; private set; }

    public Plate(Vector2 position, IMapGenerationConfiguration mapGenerationConfiguration, IShaderBuffers shaderBuffers)
    {
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myShaderBuffers = shaderBuffers;

        Position = position;

        Segments = new List<Segment>();
    }

    public void Recenter()
    {
        Position = Vector2.Zero;
        myInertia = 0.0f;
        myMass = 0.0f;

        foreach (Segment segment in Segments)
        {
            Position += segment.Position;
            myMass += segment.Mass;
            myInertia += MathF.Pow((Position - segment.Position).Length(), 2) * segment.Mass;
        }

        Position /= Segments.Count;
    }

    public void Move(Segment[] allSegments)
    {
        float[] heatMap = ReadHeatMap();

        CollideSegments(heatMap, allSegments);

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

        GrowSegments(heatMap);
        MoveSegments(heatMap);

        WriteHeatMap(heatMap);
    }

    private unsafe float[] ReadHeatMap()
    {
        uint heatMapBufferSize = myMapGenerationConfiguration.MapSize * sizeof(float);
        float[] heatMap = new float[myMapGenerationConfiguration.MapSize];
        Rlgl.MemoryBarrier();
        fixed (float* heatMapPointer = heatMap)
        {
            Rlgl.ReadShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeatMap], heatMapPointer, heatMapBufferSize, 0);
        }

        return heatMap;
    }

    private void CollideSegments(float[] heatMap, Segment[] allSegments)
    {
        for (int segment = 0; segment < Segments.Count; segment++)
        {
            IntVector2 segmentPosition = new IntVector2(Segments[segment].Position);

            if (segmentPosition.X >= myMapGenerationConfiguration.HeightMapSideLength || segmentPosition.X < 0 ||
                segmentPosition.Y >= myMapGenerationConfiguration.HeightMapSideLength || segmentPosition.Y < 0)
            {
                Segments[segment].IsAlive = false;
                continue;
            }

            const float subductionHeating = 0.1f;

            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    IntVector2 scanPosition = new IntVector2(x, y) + segmentPosition;
                    if (scanPosition.X >= myMapGenerationConfiguration.HeightMapSideLength || scanPosition.X < 0 ||
                        scanPosition.Y >= myMapGenerationConfiguration.HeightMapSideLength || scanPosition.Y < 0)
                    {
                        continue;
                    }

                    Segment? collidingSegment = allSegments[scanPosition.X + scanPosition.Y * myMapGenerationConfiguration.HeightMapSideLength];
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
                        heatMap[scanPosition.X + scanPosition.Y * myMapGenerationConfiguration.HeightMapSideLength] += subductionHeating;
                    }
                }
            }
        }
    }

    private void GrowSegments(float[] heatMap)
    {
        for (int segment = 0; segment < Segments.Count; segment++)
        {
            if (!Segments[segment].IsAlive)
            {
                continue;
            }

            IntVector2 position = new IntVector2(Segments[segment].Position);
            float nd = heatMap[position.X + position.Y * myMapGenerationConfiguration.HeightMapSideLength];              //Heat Value!


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
    }

    private static float Langmuir(float k, float x)
    {
        return k * x / (1.0f + k * x);
    }

    private void MoveSegments(float[] heatMap)
    {
        Vector2 acc = Vector2.Zero;
        float torque = 0.0f;
        foreach (Segment segment in Segments)
        {
            Vector2 force = Force(new IntVector2(segment.Position), heatMap);
            Vector2 dir = segment.Position - Position;

            acc -= Convection * force;
            torque -= Convection * dir.Length() * force.Length() * MathF.Sin(Angle(force) - Angle(dir));

        }

        const float DT = 0.025f;

        mySpeed += DT * acc / myMass;
        if (myInertia == 0)
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

    private Vector2 Force(IntVector2 position, float[] heatMap)
    {
        float fx = 0.0f;
        float fy = 0.0f;

        if (position.X > 0 && position.X < myMapGenerationConfiguration.HeightMapSideLength - 2 && position.Y > 0 && position.Y < myMapGenerationConfiguration.HeightMapSideLength - 2)
        {
            fx = (heatMap[position.X + 1 + position.Y * myMapGenerationConfiguration.HeightMapSideLength] - heatMap[position.X - 1 + position.Y * myMapGenerationConfiguration.HeightMapSideLength]) / 2.0f;
            fy = -(heatMap[position.X + (position.Y + 1) * myMapGenerationConfiguration.HeightMapSideLength] - heatMap[position.X + (position.Y - 1) * myMapGenerationConfiguration.HeightMapSideLength]) / 2.0f;
        }

        //Out-of-Bounds
        if (position.X <= 0) fx = 0.0f;
        else if (position.X >= myMapGenerationConfiguration.HeightMapSideLength - 1) fx = -0.0f;
        if (position.Y <= 0) fy = 0.0f;
        else if (position.Y >= myMapGenerationConfiguration.HeightMapSideLength - 1) fy = -0.0f;

        return new Vector2(fx, fy);
    }

    private static float Angle(Vector2 d)
    {
        if (d.X == 0 && d.Y == 0) return 0.0f;
        if (d.X == 0 && d.Y > 0) return MathF.PI / 2.0f;
        if (d.X == 0 && d.Y < 0) return 3.0f * MathF.PI / 2.0f;

        float a = 2.0f * MathF.PI + MathF.Atan(d.Y / d.X);

        if (d.X < 0) a += MathF.PI;

        return a;
    }

    private unsafe void WriteHeatMap(float[] heatMap)
    {
        uint heatMapBufferSize = myMapGenerationConfiguration.MapSize * sizeof(float);
        fixed (float* heatMapPointer = heatMap)
        {
            Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeatMap], heatMapPointer, heatMapBufferSize, 0);
        }
        Rlgl.MemoryBarrier();
    }
}
