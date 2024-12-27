using System.Numerics;

namespace ProceduralLandscapeGeneration
{
    internal class WaterParticle
    {
        private const int MaxAge = 1024;
        private const float EvaporationRate = 0.001f;
        private const float DepositionRate = 0.05f;
        private const float MinimumVolume = 0.001f;
        private const float Entrainment = 0.0f;
        private const float Gravity = 2.0f;

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

            if (myAge > MaxAge)
            {
                return false;
            }

            if (myVolume < MinimumVolume)
            {
                heightMap.Height[position.X, position.Y] += mySediment;
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

            float cEq = (1.0f + Entrainment) * heightMap.Height[position.X, position.Y] - h2;
            if (cEq < 0)
            {
                cEq = 0;
            }

            float cDiff = (cEq * myVolume - mySediment);

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

            myVolume *= (1.0f - EvaporationRate);

            myAge++;

            return true;
        }
    }
}
