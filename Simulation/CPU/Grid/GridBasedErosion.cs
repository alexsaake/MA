using ProceduralLandscapeGeneration.Common;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Simulation.CPU.Grid
{
    internal class GridBasedErosion
    {
        //Fast Hydraulic Erosion Simulation and Visualization on GPU, Xing Mei, Philippe Decaudin, Bao-Gang Hu
        //https://github.com/karhu/terrain-erosion/blob/master/Simulation/FluidSimulation.cpp
        public GridPoint[,] GridPoints;

        public GridBasedErosion(HeightMap heightMap)
        {
            GridPoints = new GridPoint[heightMap.Width, heightMap.Depth];
        }

        public void WaterIncrease(IVector2 position, float value)
        {
            GridPoints[position.X, position.Y].WaterHeight += value;
        }

        public void Simulate(HeightMap heightMap)
        {
            SimulateFlow(heightMap);
            SedimentTransportation();
        }

        private void SimulateFlow(HeightMap heightMap)
        {
            float A = 0.00005f;
            float g = 9.81f;
            float l = 1.0f;

            float Kc = 0.01f;
            float Ks = 0.1f;
            float Kd = 0.1f;

            float fluxFactor = A * g / l;

            for (int y = 1; y < GridPoints.GetLength(1) - 1; y++)
            {
                for (int x = 1; x < GridPoints.GetLength(0) - 1; x++)
                {
                    float dh;
                    float h0 = heightMap.Height[x, y] + GridPoints[x, y].WaterHeight;
                    float newFlux;

                    dh = h0 - heightMap.Height[x - 1, y] + GridPoints[x - 1, y].WaterHeight;
                    newFlux = GridPoints[x, y].FlowLeft + fluxFactor * dh;
                    GridPoints[x, y].FlowLeft = MathF.Max(0.0f, newFlux);

                    dh = h0 - heightMap.Height[x + 1, y] + GridPoints[x + 1, y].WaterHeight;
                    newFlux = GridPoints[x, y].FlowRight + fluxFactor * dh;
                    GridPoints[x, y].FlowRight = MathF.Max(0.0f, newFlux);

                    dh = h0 - heightMap.Height[x, y - 1] + GridPoints[x, y - 1].WaterHeight;
                    newFlux = GridPoints[x, y].FlowBottom + fluxFactor * dh;
                    GridPoints[x, y].FlowBottom = MathF.Max(0.0f, newFlux);

                    dh = h0 - heightMap.Height[x, y + 1] + GridPoints[x, y + 1].WaterHeight;
                    newFlux = GridPoints[x, y].FlowTop + fluxFactor * dh;
                    GridPoints[x, y].FlowTop = MathF.Max(0.0f, newFlux);

                    float sumFlux = GridPoints[x, y].FlowLeft + GridPoints[x, y].FlowRight + GridPoints[x, y].FlowBottom + GridPoints[x, y].FlowTop;
                    if (sumFlux > 0.0f)
                    {
                        float K = MathF.Min(1.0f, GridPoints[x, y].WaterHeight / sumFlux);

                        GridPoints[x, y].FlowLeft *= K;
                        GridPoints[x, y].FlowRight *= K;
                        GridPoints[x, y].FlowBottom *= K;
                        GridPoints[x, y].FlowTop *= K;
                    }
                }
            }

            for (int y = 1; y < GridPoints.GetLength(1) - 1; y++)
            {
                for (int x = 1; x < GridPoints.GetLength(0) - 1; x++)
                {
                    float inFlow = GetFlowRight(x - 1, y) + GetFlowLeft(x + 1, y) + GetFlowTop(x, y - 1) + GetFlowBottom(x, y + 1);
                    float outFlow = GetFlowRight(x, y) + GetFlowLeft(x, y) + GetFlowTop(x, y) + GetFlowBottom(x, y);
                    float dV = inFlow - outFlow;
                    float oldWater = GridPoints[x, y].WaterHeight;
                    GridPoints[x, y].WaterHeight += dV;
                    GridPoints[x, y].WaterHeight = MathF.Max(0.0f, GridPoints[x, y].WaterHeight);

                    float meanWater = 0.5f * (oldWater + GridPoints[x, y].WaterHeight);

                    if (meanWater == 0.0f)
                    {
                        GridPoints[x, y].VelocityX = 0.0f;
                        GridPoints[x, y].VelocityY = 0.0f;
                    }
                    else
                    {
                        GridPoints[x, y].VelocityX = 0.5f * (GetFlowRight(x - 1, y) - GetFlowLeft(x, y) - GetFlowLeft(x + 1, y) + GetFlowRight(x, y)) / meanWater;
                        GridPoints[x, y].VelocityY = 0.5f * (GetFlowTop(x, y - 1) - GetFlowBottom(x, y) - GetFlowBottom(x, y + 1) + GetFlowTop(x, y)) / meanWater;
                    }

                    //ErosionAndDeposition

                    Vector3 normal = new Vector3(heightMap.Height[x + 1, y] - heightMap.Height[x - 1, y], heightMap.Height[x, y + 1] - heightMap.Height[x, y - 1], 2);
                    normal = Vector3.Normalize(normal);
                    float cosa = Vector3.Dot(normal, Vector3.UnitZ);
                    float sinAlpha = MathF.Sin(MathF.Acos(cosa));
                    sinAlpha = MathF.Max(sinAlpha, 0.1f);

                    float capacity = Kc * MathF.Sqrt(GridPoints[x, y].VelocityX * GridPoints[x, y].VelocityX + GridPoints[x, y].VelocityY * GridPoints[x, y].VelocityY) * sinAlpha * MathF.Min(GridPoints[x, y].WaterHeight, 0.01f) / 0.01f;
                    float delta = capacity - GridPoints[x, y].SuspendedSediment;

                    if (delta > 0.0f)
                    {
                        float d = Ks * delta;
                        heightMap.Height[x, y] -= d;
                        GridPoints[x, y].SuspendedSediment += d;
                    }
                    else if (delta < 0.0f)
                    {
                        float d = Kd * delta;
                        heightMap.Height[x, y] -= d;
                        GridPoints[x, y].SuspendedSediment += d;
                    }
                }
            }
        }

        private float GetFlowRight(int x, int y)
        {
            if (x < 0 || x > GridPoints.GetLength(0) - 1)
            {
                return 0.0f;
            }
            return GridPoints[x, y].FlowRight;
        }

        private float GetFlowLeft(int x, int y)
        {
            if (x < 0 || x > GridPoints.GetLength(0) - 1)
            {
                return 0.0f;
            }
            return GridPoints[x, y].FlowLeft;
        }

        private float GetFlowBottom(int x, int y)
        {
            if (y < 0 || y > GridPoints.GetLength(1) - 1)
            {
                return 0.0f;
            }
            return GridPoints[x, y].FlowBottom;
        }

        private float GetFlowTop(int x, int y)
        {
            if (y < 0 || y > GridPoints.GetLength(1) - 1)
            {
                return 0.0f;
            }
            return GridPoints[x, y].FlowTop;
        }

        private void SedimentTransportation()
        {
            float Ke = 0.00001f;

            for (int y = 1; y < GridPoints.GetLength(1) - 1; y++)
            {
                for (int x = 1; x < GridPoints.GetLength(0) - 1; x++)
                {
                    float fromPosX = x - GridPoints[x, y].VelocityX;
                    float fromPosY = y - GridPoints[x, y].VelocityY;

                    // integer coordinates
                    int x0 = (int)fromPosX;
                    int y0 = (int)fromPosY;
                    int x1 = x0 + 1;
                    int y1 = y0 + 1;

                    // interpolation factors
                    float fX = fromPosX - x0;
                    float fY = fromPosY - y0;

                    // clamp to grid borders
                    x0 = (int)MathF.Min(GridPoints.GetLength(0) - 1, MathF.Max(0, x0));
                    x1 = (int)MathF.Min(GridPoints.GetLength(0) - 1, MathF.Max(0, x1));
                    y0 = (int)MathF.Min(GridPoints.GetLength(1) - 1, MathF.Max(0, y0));
                    y1 = (int)MathF.Min(GridPoints.GetLength(1) - 1, MathF.Max(0, y1));

                    float newVal = Math.Lerp(Math.Lerp(GridPoints[x0, y0].SuspendedSediment, GridPoints[x1, y0].SuspendedSediment, fX), Math.Lerp(GridPoints[x0, y1].SuspendedSediment, GridPoints[x1, y1].SuspendedSediment, fX), fY);
                    GridPoints[x, y].TempSediment = newVal;
                }
            }

            for (int y = 0; y < GridPoints.GetLength(1); y++)
            {
                for (int x = 0; x < GridPoints.GetLength(0); x++)
                {
                    GridPoints[x, y].SuspendedSediment = GridPoints[x, y].TempSediment;

                    //WaterDecrease
                    GridPoints[x, y].WaterHeight = MathF.Max(0, GridPoints[x, y].WaterHeight - Ke);
                }
            }
        }
    }
}
