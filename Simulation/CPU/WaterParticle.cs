using ProceduralLandscapeGeneration.Common;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Simulation.CPU
{
    internal class WaterParticle
    {
        //https://github.com/erosiv/soillib/blob/main/source/particle/water.hpp
        private const int MaxAge = 1024;
        private const float EvaporationRate = 0.001f;
        private const float DepositionRate = 0.05f;
        private const float MinimumVolume = 0.001f;
        private const float Gravity = 2.0f;
        private const float MaxDiff = 0.8f;
        private const float Settling = 1.0f;

        private Vector2 myPosition;
        private Vector2 myOriginalPosition;
        private Vector2 mySpeed;
        private int myAge = 0;
        private float myVolume = 1.0f;
        private float mySediment = 0.0f;

        public WaterParticle(Vector2 position)
        {
            myPosition = position;
            mySpeed = Vector2.Zero;
        }

        public bool Move(HeightMap heightMap)
        {
            IVector2 position = new IVector2(myPosition);

            if (heightMap.IsOutOfBounds(position))
            {
                return false;
            }

            if (myAge > MaxAge
                || myVolume < MinimumVolume)
            {
                heightMap.Height[position.X, position.Y] += mySediment;
                Cascade(heightMap, position);
                return false;
            }

            Vector3 normal = heightMap.GetNormal(position);

            mySpeed += Gravity * new Vector2(normal.X, normal.Y) / myVolume;

            if (mySpeed.Length() > 0)
            {
                mySpeed = MathF.Sqrt(2.0f) * Vector2.Normalize(mySpeed);
            }

            myOriginalPosition = myPosition;
            myPosition += mySpeed;

            return true;
        }

        public bool Interact(HeightMap heightMap)
        {
            IVector2 position = new IVector2(myOriginalPosition);

            if (heightMap.IsOutOfBounds(position))
            {
                return false;
            }

            float h2;
            if (heightMap.IsOutOfBounds(myPosition))
            {
                h2 = 0.99f * heightMap.Height[position.X, position.Y];
            }
            else
            {
                IVector2 currentPosition = new IVector2(myPosition);
                h2 = heightMap.Height[currentPosition.X, currentPosition.Y];
            }

            float cEq = heightMap.Height[position.X, position.Y] - h2;
            if (cEq < 0)
            {
                cEq = 0;
            }

            float cDiff = cEq * myVolume - mySediment;

            float effD = DepositionRate;
            if (effD < 0)
            {
                effD = 0;
            }

            if (effD * cDiff < 0)
            {
                if (effD * cDiff < -mySediment)
                {
                    cDiff = -mySediment / effD;
                }
            }

            mySediment += effD * cDiff;
            heightMap.Height[position.X, position.Y] -= effD * cDiff;

            myVolume *= 1.0f - EvaporationRate;

            Cascade(heightMap, position);

            myAge++;

            return true;
        }
        internal struct Point
        {
            public IVector2 pos;
            public float h;
            public float d;
        }

        //https://github.com/erosiv/soillib/blob/main/source/particle/cascade.hpp
        void Cascade(HeightMap heightMap, IVector2 ipos)
        {
            if (heightMap.IsOutOfBounds(ipos))
            {
                return;
            }

            // Get Non-Out-of-Bounds Neighbors

            IVector2[] n = {
                new IVector2(-1, -1),
                new IVector2(-1, 0),
                new IVector2(-1, 1),
                new IVector2(0, -1),
                new IVector2(0, 1),
                new IVector2(1, -1),
                new IVector2(1, 0),
                new IVector2(1, 1)
            };

            Point[] sn = new Point[8];

            int num = 0;

            for (int i = 0; i < n.Length; i++)
            {
                IVector2 nn = n[i];
                IVector2 npos = ipos + nn;

                if (heightMap.IsOutOfBounds(npos))
                {
                    continue;
                }

                sn[num].pos = npos;
                sn[num].h = heightMap.Height[npos.X, npos.Y];
                sn[num].d = nn.Length();
                num++;
            }

            // Local Matrix, Target Height

            float height = heightMap.Height[ipos.X, ipos.Y];
            float h_ave = height;
            for (int i = 0; i < num; ++i)
            {
                h_ave += sn[i].h;
            }
            h_ave /= (float)num + 1;

            for (int i = 0; i < num; ++i)
            {
                // Full Height-Different Between Positions!
                float diff = h_ave - sn[i].h;
                if (diff == 0)
                {
                    continue;
                }

                IVector2 tpos = (diff > 0) ? ipos : sn[i].pos;
                IVector2 bpos = (diff > 0) ? sn[i].pos : ipos;

                // The Amount of Excess Difference!
                float excess = 0.0f;
                excess = MathF.Abs(diff) - sn[i].d * MaxDiff;
                if (excess <= 0)
                {
                    continue;
                }

                // Actual Amount Transferred
                float transfer = Settling * excess / 2.0f;
                heightMap.Height[tpos.X, tpos.Y] -= transfer;
                heightMap.Height[bpos.X, bpos.Y] += transfer;
            }
        }
    }
}
