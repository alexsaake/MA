using System.Numerics;

namespace ProceduralLandscapeGeneration
{
    internal class WaterParticle
    {
        private const int MaxAge = 1024;
        private const float EvaporationRate = 0.001f;
        private const float DepositionRate = 0.05f;
        private const float MinimumVolume = 0.001f;
        private const float Entrainment = 10.0f;
        private const float Gravity = 2.0f;
        private const float MomentumTransfer = 1.0f;

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
            Vector2Int position = new Vector2Int(myPosition);

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
                heightMap.Value[position.X, position.Y].Height += mySediment;
                //Cascade(heightMap, position);
                return false;
            }

            Vector3 normal = heightMap.GetNormal(position);

            //Vector2 fSpeed = heightMap.Value[position.X, position.Y].Momentum;
            Vector2 fSpeed = Vector2.Zero;
            //float discharge = 0.4f * heightMap.Value[position.X, position.Y].Discharge;//erf c++
            float discharge = 0;

            mySpeed += Gravity * new Vector2(normal.X, normal.Y) / myVolume;

            if (fSpeed.Length() > 0
                && mySpeed.Length() > 0)
            {
                mySpeed += MomentumTransfer * Vector2.Dot(Vector2.Normalize(fSpeed), Vector2.Normalize(mySpeed)) / (myVolume + discharge) * fSpeed;
            }

            if (mySpeed.Length() > 0)
            {
                mySpeed = MathF.Sqrt(2.0f) * Vector2.Normalize(mySpeed);
            }

            myOriginalPosition = myPosition;
            myPosition += mySpeed;

            return true;
        }

        public void Track(HeightMap heightMap)
        {
            Vector2Int position = new Vector2Int(myPosition);

            if (heightMap.IsOutOfBounds(position))
            {
                return;
            }

            heightMap.Value[position.X, position.Y].DischargeTrack += myVolume;
            heightMap.Value[position.X, position.Y].MomentumTrack += myVolume * mySpeed;
        }

        public bool Interact(HeightMap heightMap)
        {
            Vector2Int position = new Vector2Int(myOriginalPosition);

            if (heightMap.IsOutOfBounds(position))
            {
                return false;
            }

            //float discharge = 0.4f * heightMap.Value[position.X, position.Y].Discharge;//erf c++
            float discharge = 0.4f * heightMap.Value[position.X, position.Y].Discharge;//erf c++
            //float resistance = heightMap.Value[position.X, position.Y].Resistance;
            float resistance = 0;

            float h2;
            if (heightMap.IsOutOfBounds(myPosition))
            {
                h2 = 0.99f * heightMap.Value[position.X, position.Y].Height;
            }
            else
            {
                Vector2Int currentPosition = new Vector2Int(myPosition);
                h2 = heightMap.Value[currentPosition.X, currentPosition.Y].Height;
            }

            float cEq = (1.0f + Entrainment * discharge) * heightMap.Value[position.X, position.Y].Height - h2;
            if (cEq < 0)
            {
                cEq = 0;
            }

            float cDiff = (cEq * myVolume - mySediment);

            float effD = DepositionRate * (1.0f - resistance);
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
            else if (effD * cDiff > 0)
            {

                //https://github.com/erosiv/soillib/blob/main/source/particle/water.hpp
                //matrix = (matrix * sediment + wmatrix * (effD * cdiff)) / (sediment + effD * cdiff);
            }

            mySediment += effD * cDiff;
            heightMap.Value[position.X, position.Y].Height -= effD * cDiff;

            myVolume *= (1.0f - EvaporationRate);

            //Cascade(heightMap, position);

            myAge++;

            return true;
        }
    }
}
