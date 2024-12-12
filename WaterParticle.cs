using System.Numerics;

namespace ProceduralLandscapeGeneration
{
    internal class WaterParticle
    {
        private const float TimeStep = 1.2f;
        private const float Friction = 0.05f;
        private const float Density = 1.0f;
        private const float EvaporationRate = 0.001f;
        private const float DepositionRate = 0.1f;
        private const float MinimumVolume = 0.01f;

        private Vector2 myPosition;
        private Vector2 mySpeed;
        private float myVolume = 1.0f;
        private float mySediment = 0.0f;

        public WaterParticle(Vector2 position)
        {
            myPosition = position;
            mySpeed = Vector2.Zero;
        }

        public void Erode(HeightMap heightMap)
        {
            while (myVolume > MinimumVolume)
            {
                Vector2 initialPosition = myPosition;
                Vector3 normal = heightMap.GetNormal((int)myPosition.X, (int)myPosition.Y);
                mySpeed += TimeStep * new Vector2(normal.X, normal.Y) / (myVolume * Density);
                myPosition += TimeStep * mySpeed;
                mySpeed *= (1.0f - TimeStep * Friction);

                if (heightMap.IsOutOfBounds(myPosition))
                {
                    break;
                }

                float maximumSediment = myVolume * mySpeed.Length() * (heightMap.Data[(int)initialPosition.X, (int)initialPosition.Y] - heightMap.Data[(int)myPosition.X, (int)myPosition.Y]);

                if (maximumSediment < 0.0f)
                {
                    maximumSediment = 0.0f;
                }

                float sedimentDifference = maximumSediment - mySediment;

                mySediment += TimeStep * DepositionRate * sedimentDifference;
                heightMap.Data[(int)initialPosition.X, (int)initialPosition.Y] -= TimeStep * myVolume * DepositionRate * sedimentDifference;

                myVolume *= (1.0f - TimeStep * EvaporationRate);
            }
        }
    }
}
