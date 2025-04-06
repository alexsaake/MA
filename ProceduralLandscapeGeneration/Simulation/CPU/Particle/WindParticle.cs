using ProceduralLandscapeGeneration.Common;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Simulation.CPU.Particle;

internal class WindParticle
{
    private const int MaximumAge = 1024;
    private const float BoundaryLayer = 2.0f;
    private const float Suspension = 0.0001f;
    private const float Gravity = 0.1f;

    private Vector3 myPosition;
    private Vector3 mySpeed;
    private int myAge;
    private float mySediment = 0.0f;

    public WindParticle(Vector2 position)
    {
        myPosition = new Vector3(position.X, 0, position.Y);
    }

    public void Fly(HeightMap heightMap, Vector3 prevailingWindSpeed)
    {
        prevailingWindSpeed = Vector3.Normalize(prevailingWindSpeed);

        while (myAge++ < MaximumAge)
        {
            IVector2 initialPosition = new IVector2(myPosition.X, myPosition.Y);
            Vector3 normal = heightMap.GetScaledNormal(initialPosition);

            if (heightMap.IsOutOfBounds(initialPosition))
            {
                break;
            }

            if (myAge == 1
                || myPosition.Z < heightMap.Height[initialPosition.X, initialPosition.Y])
            {
                myPosition.Z = heightMap.Height[initialPosition.X, initialPosition.Y];
            }

            float hfac = MathF.Exp(-(myPosition.Y - heightMap.Height[initialPosition.X, initialPosition.Y]) / BoundaryLayer);
            if (hfac < 0)
            {
                hfac = 0;
            }

            //// Apply Base Prevailign Wind-Speed w. Shadowing

            float shadow = Vector3.Dot(prevailingWindSpeed, normal);
            if (shadow < 0)
            {
                shadow = 0;
            }
            shadow = 1.0f - shadow;

            mySpeed += 0.05f * ((0.1f + 0.9f * shadow) * prevailingWindSpeed - mySpeed);

            //// Apply Gravity

            if (myPosition.Y > heightMap.Height[initialPosition.X, initialPosition.Y])
            {
                mySpeed.Y -= Gravity * mySediment;
            }

            //// Compute Collision Factor

            float collision = -Vector3.Dot(Vector3.Normalize(mySpeed), normal);
            if (collision < 0)
            {
                collision = 0;
            }

            //// Compute Redirect Velocity

            Vector3 redirectionSpeed = Vector3.Cross(normal, Vector3.Cross((1.0f - collision) * mySpeed, normal));

            //// Speed is accelerated by terrain features

            mySpeed += 0.9f * (shadow * Vector3.Lerp(prevailingWindSpeed, redirectionSpeed, shadow * hfac) - mySpeed);

            //// Turbulence

            var random = new System.Random();
            mySpeed += 0.1f * hfac * collision * new Vector3(random.Next(1001) - 500.0f, random.Next(1001) - 500.0f, random.Next(1001) - 500.0f) / 500.0f;

            //// Speed is damped by drag

            mySpeed *= 1.0f - 0.3f * mySediment;

            //// Move

            myPosition += mySpeed;

            //// Compute Mass Transport

            float force = -Vector3.Dot(Vector3.Normalize(mySpeed), normal) * mySpeed.Length();
            if (force < 0)
            {
                force = 0;
            }

            float lift = (1.0f - collision) * mySpeed.Length();

            float capacity = force * hfac + 0.02f * lift * hfac;

            //// Mass Transfer to Equilibrium

            float diff = capacity - mySediment;
            heightMap.Height[initialPosition.X, initialPosition.Y] -= Suspension * diff;
            mySediment += Suspension * diff;

            Cascade(heightMap, initialPosition);
        }
    }

    static IVector2[] n = {
        new IVector2(-1, -1),
        new IVector2(-1,  0),
        new IVector2(-1,  1),
        new IVector2( 0, -1),
        new IVector2( 0,  1),
        new IVector2( 1, -1),
        new IVector2( 1,  0),
        new IVector2( 1,  1)
      };

    struct Point
    {
        public IVector2 pos;
        public float h;
        public float d;
    };

    private const float LevelOfDetail = 1.2f;
    private const float MaxDiff = 0.005f;
    private const float Settling = 0.01f;

    private static void Cascade(HeightMap heightMap, IVector2 position)
    {

        // Get Non-Out-of-Bounds Neighbors


        Point[] sn = new Point[8];
        int num = 0;

        IVector2 ipos = position;

        foreach (IVector2 nn in n)
        {

            IVector2 npos = ipos + LevelOfDetail * nn;

            if (heightMap.IsOutOfBounds(npos))
            {
                continue;
            }

            sn[num++] = new Point()
            {
                pos = npos,
                h = heightMap.Height[npos.X, npos.Y],
                d = nn.Length()
            };

        }

        sn = sn.OrderBy(point => point.h).ToArray();

        for (int i = 0; i < num; ++i)
        {

            var npos = sn[i].pos;

            //Full Height-Different Between Positions!
            float diff = heightMap.Height[ipos.X, ipos.Y] - sn[i].h;
            if (diff == 0)   //No Height Difference
            {
                continue;
            }

            //The Amount of Excess Difference!
            float excess = 0.0f;
            excess = MathF.Abs(diff) - sn[i].d * MaxDiff * LevelOfDetail;

            if (excess <= 0)  //No Excess
            {
                continue;
            }

            //Actual Amount Transferred
            float transfer = Settling * excess / 2.0f;

            //Cap by Maximum Transferrable Amount
            if (diff > 0)
            {
                heightMap.Height[ipos.X, ipos.Y] -= transfer;
                heightMap.Height[npos.X, npos.Y] += transfer;
            }
            else
            {
                heightMap.Height[ipos.X, ipos.Y] += transfer;
                heightMap.Height[npos.X, npos.Y] -= transfer;
            }
        }
    }
}
