using ProceduralLandscapeGeneration.Common;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Simulation.CPU
{
    internal class GridBasedErosion
    {
        //Fast Hydraulic Erosion Simulation and Visualization on GPU, Xing Mei, Philippe Decaudin, Bao-Gang Hu
        public struct GridPoint
        {
            public float TerrainHeight;
            public float WaterHeight;
            public float SuspendedSediment;
            public float PreviouslySuspendedSediment;

            public float FlowLeft;
            public float FlowRight;
            public float FlowTop;
            public float FlowBottom;

            public Vector2 Velocity;
        }

        public GridPoint[,] GridPoints;

        private bool myIsUpdateRequired;

        public GridBasedErosion(HeightMap heightMap)
        {
            GridPoints = new GridPoint[heightMap.Width, heightMap.Depth];

            for (int y = 0; y < heightMap.Depth; y++)
            {
                for (int x = 0; x < heightMap.Width; x++)
                {
                    GridPoints[x, y].TerrainHeight = heightMap.Height[x, y];
                }
            }
        }

        public void WaterIncrease(IVector2 position, float value)
        {
            GridPoints[position.X, position.Y].WaterHeight += value;
        }

        public void Simulate()
        {
            myIsUpdateRequired = true;
            int iteration = 0;
            while (myIsUpdateRequired && iteration < 100)
            {
                FlowSimulation();
                ErosionAndDeposition();
                SedimentTransportation();
                WaterDecrease();
                iteration++;
            }
        }

        private void FlowSimulation()
        {
            float A = 1.0f;
            float g = 1.0f;
            float l = 1.0f;

            for (int y = 1; y < GridPoints.GetLength(1) - 1; y++)
            {
                for (int x = 1; x < GridPoints.GetLength(0) - 1; x++)
                {
                    GridPoints[x, y].FlowLeft = MathF.Max(0, GridPoints[x, y].FlowLeft + A * g * (GridPoints[x, y].TerrainHeight + GridPoints[x, y].WaterHeight - GridPoints[x - 1, y].TerrainHeight + GridPoints[x - 1, y].WaterHeight) / l);
                    GridPoints[x, y].FlowRight = MathF.Max(0, GridPoints[x, y].FlowRight + A * g * (GridPoints[x, y].TerrainHeight + GridPoints[x, y].WaterHeight - GridPoints[x + 1, y].TerrainHeight + GridPoints[x + 1, y].WaterHeight) / l);
                    GridPoints[x, y].FlowTop = MathF.Max(0, GridPoints[x, y].FlowTop + A * g * (GridPoints[x, y].TerrainHeight + GridPoints[x, y].WaterHeight - GridPoints[x, y + 1].TerrainHeight + GridPoints[x, y + 1].WaterHeight) / l);
                    GridPoints[x, y].FlowBottom = MathF.Max(0, GridPoints[x, y].FlowBottom + A * g * (GridPoints[x, y].TerrainHeight + GridPoints[x, y].WaterHeight - GridPoints[x, y - 1].TerrainHeight + GridPoints[x, y - 1].WaterHeight) / l);

                    float tmp = (GridPoints[x, y].FlowLeft + GridPoints[x, y].FlowRight + GridPoints[x, y].FlowTop + GridPoints[x, y].FlowBottom);
                    if (tmp > 0)
                    {
                        float scaling = MathF.Min(1, GridPoints[x, y].WaterHeight / tmp);

                        GridPoints[x, y].FlowLeft = scaling * GridPoints[x, y].FlowLeft;
                        GridPoints[x, y].FlowRight = scaling * GridPoints[x, y].FlowRight;
                        GridPoints[x, y].FlowTop = scaling * GridPoints[x, y].FlowTop;
                        GridPoints[x, y].FlowBottom = scaling * GridPoints[x, y].FlowBottom;
                    }
                }
            }

            for (int y = 1; y < GridPoints.GetLength(1) - 1; y++)
            {
                for (int x = 1; x < GridPoints.GetLength(0) - 1; x++)
                {
                    GridPoints[x, y].WaterHeight += GridPoints[x - 1, y].FlowRight + GridPoints[x + 1, y].FlowLeft + GridPoints[x, y + 1].FlowBottom + GridPoints[x, y - 1].FlowTop - (GridPoints[x, y].FlowLeft + GridPoints[x, y].FlowRight + GridPoints[x, y].FlowTop + GridPoints[x, y].FlowBottom);

                    GridPoints[x, y].Velocity = new Vector2((GridPoints[x - 1, y].FlowRight - GridPoints[x, y].FlowLeft + GridPoints[x, y].FlowRight - GridPoints[x + 1, y].FlowLeft) / 2,
                        (GridPoints[x, y - 1].FlowTop - GridPoints[x, y].FlowBottom + GridPoints[x, y].FlowTop - GridPoints[x, y + 1].FlowBottom) / 2);
                }
            }
        }

        private void ErosionAndDeposition()
        {
            float Kc = 1.0f;
            float Ks = 1.0f;
            float Kd = 1.0f;

            myIsUpdateRequired = false;

            for (int y = 1; y < GridPoints.GetLength(1) - 1; y++)
            {
                for (int x = 1; x < GridPoints.GetLength(0) - 1; x++)
                {
                    float C = Kc * MathF.Abs(GridPoints[x, y].Velocity.Length());

                    if (C > GridPoints[x, y].SuspendedSediment)
                    {
                        GridPoints[x, y].TerrainHeight -= Ks * (C - GridPoints[x, y].SuspendedSediment);
                        GridPoints[x, y].SuspendedSediment += Ks * (C - GridPoints[x, y].SuspendedSediment);
                        myIsUpdateRequired = true;
                    }
                    else
                    {
                        GridPoints[x, y].TerrainHeight += Kd * (GridPoints[x, y].SuspendedSediment - C);
                        GridPoints[x, y].SuspendedSediment -= Kd * (GridPoints[x, y].SuspendedSediment - C);
                        myIsUpdateRequired = true;
                    }
                }
            }
        }

        private void SedimentTransportation()
        {
            float Kp = 1.0f;
            float Uc = 0.0001f;

            for (int y = 1; y < GridPoints.GetLength(1) - 1; y++)
            {
                for (int x = 1; x < GridPoints.GetLength(0) - 1; x++)
                {
                    GridPoints[x, y].SuspendedSediment = GridPoints[x, y].PreviouslySuspendedSediment;
                }
            }
            for (int y = 1; y < GridPoints.GetLength(1) - 1; y++)
            {
                for (int x = 1; x < GridPoints.GetLength(0) - 1; x++)
                {
                    Vector2 v = GridPoints[x, y].Velocity;
                    GridPoints[x, y].SuspendedSediment = GridPoints[x - (int)MathF.Ceiling(v.X), y - (int)MathF.Ceiling(v.Y)].PreviouslySuspendedSediment;
                }
            }
        }

        private void WaterDecrease()
        {
            float Ke = 0.001f;

            for (int y = 1; y < GridPoints.GetLength(1) - 1; y++)
            {
                for (int x = 1; x < GridPoints.GetLength(0) - 1; x++)
                {
                    GridPoints[x, y].WaterHeight *= (1 - Ke);
                }
            }
        }
    }
}
