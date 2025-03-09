using ProceduralLandscapeGeneration.Common;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Simulation.CPU.Grid;

internal class GridBasedErosion
{
    private const float myDt = 1;

    //Fast Hydraulic Erosion Simulation and Visualization on GPU, Xing Mei, Philippe Decaudin, Bao-Gang Hu
    //https://github.com/Huw-man/Interactive-Erosion-Simulator-on-GPU
    //https://github.com/karhu/terrain-erosion/blob/master/Simulation/FluidSimulation.cpp
    public GridPoint[,] GridPoints;

    public GridBasedErosion(HeightMap heightMap)
    {
        GridPoints = new GridPoint[heightMap.Width, heightMap.Depth];
    }

    public void WaterIncrease(IVector2 position, float value)
    {
        GridPoints[position.X, position.Y].WaterHeight += myDt * value;
    }

    public void Simulate(HeightMap heightMap)
    {
        FlowErosionSedimentTransportation(heightMap);
        WaterDecrease();
    }

    private void FlowErosionSedimentTransportation(HeightMap heightMap)
    {
        float A = 20.0f;
        float g = 9.81f;
        float l = 1.0f / 256;

        float dx = 1.0f / 256;
        float dy = 1.0f / 256;

        float Kc = 10.0f;
        float Ks = 0.1f;
        float Kd = 0.1f;

        float fluxFactor = myDt * A / l * g;

        for (int y = 1; y < GridPoints.GetLength(1) - 1; y++)
        {
            for (int x = 1; x < GridPoints.GetLength(0) - 1; x++)
            {
                if(GridPoints[x, y].WaterHeight == 0.0f)
                {
                    continue;
                }

                float h0 = heightMap.Height[x, y] + GridPoints[x, y].WaterHeight;

                if (x > 0)
                {
                    var dhL = h0 - heightMap.Height[x - 1, y] + GridPoints[x - 1, y].WaterHeight;
                    var newFluxL = GridPoints[x, y].FlowLeft + dhL * fluxFactor;
                    GridPoints[x, y].FlowLeft = MathF.Max(0.0f, newFluxL);
                }
                else
                {
                    GridPoints[x, y].FlowLeft = 0.0f;
                }

                if (x < GridPoints.GetLength(0) - 1)
                {
                    var dhR = h0 - heightMap.Height[x + 1, y] + GridPoints[x + 1, y].WaterHeight;
                    var newFluxR = GridPoints[x, y].FlowRight + dhR * fluxFactor;
                    GridPoints[x, y].FlowRight = MathF.Max(0.0f, newFluxR);
                }
                else
                {
                    GridPoints[x, y].FlowRight = 0.0f;
                }

                if (y > 0)
                {
                    var dhB = h0 - heightMap.Height[x, y - 1] + GridPoints[x, y - 1].WaterHeight;
                    var newFluxB = GridPoints[x, y].FlowBottom + dhB * fluxFactor;
                    GridPoints[x, y].FlowBottom = MathF.Max(0.0f, newFluxB);
                }
                else
                {
                    GridPoints[x, y].FlowBottom = 0.0f;
                }

                if (y < GridPoints.GetLength(1) - 1)
                {
                    var dhT = h0 - heightMap.Height[x, y + 1] + GridPoints[x, y + 1].WaterHeight;
                    var newFluxT = GridPoints[x, y].FlowTop + dhT * fluxFactor;
                    GridPoints[x, y].FlowTop = MathF.Max(0.0f, newFluxT);
                }
                else
                {
                    GridPoints[x, y].FlowTop = 0.0f;
                }

                float sumFlux = GridPoints[x, y].FlowLeft + GridPoints[x, y].FlowRight + GridPoints[x, y].FlowBottom + GridPoints[x, y].FlowTop;
                if (sumFlux > 0.0f)
                {
                    float K = MathF.Min(1.0f, GridPoints[x, y].WaterHeight * dx * dy / (sumFlux * myDt));

                    GridPoints[x, y].FlowLeft *= K;
                    GridPoints[x, y].FlowRight *= K;
                    GridPoints[x, y].FlowBottom *= K;
                    GridPoints[x, y].FlowTop *= K;
                }

                float inFlow = GetFlowRight(x - 1, y) + GetFlowLeft(x + 1, y) + GetFlowTop(x, y - 1) + GetFlowBottom(x, y + 1);
                float outFlow = GetFlowRight(x, y) + GetFlowLeft(x, y) + GetFlowTop(x, y) + GetFlowBottom(x, y);
                float dV = myDt * (inFlow - outFlow);
                float oldWater = GridPoints[x, y].WaterHeight;
                GridPoints[x, y].WaterHeight += dV / (dx + dy);
                GridPoints[x, y].WaterHeight = MathF.Max(0.0f, GridPoints[x, y].WaterHeight);

                float meanWater = GridPoints[x, y].WaterHeight + 0.5f * oldWater;

                float velocityX;
                float velocityY;

                if (meanWater == 0.0f)
                {
                    velocityX = 0.0f;
                    velocityY = 0.0f;
                }
                else
                {
                    velocityX = (GetFlowRight(x - 1, y) - GetFlowLeft(x, y) + GetFlowRight(x, y) - GetFlowLeft(x + 1, y)) / dy / meanWater;
                    velocityY = (GetFlowTop(x, y - 1) - GetFlowBottom(x, y) + GetFlowTop(x, y) - GetFlowBottom(x, y + 1)) / dx / meanWater;
                }

                Vector3 normal = new Vector3(heightMap.Height[x + 1, y] - heightMap.Height[x - 1, y], heightMap.Height[x, y + 1] - heightMap.Height[x, y - 1], 2);
                normal = Vector3.Normalize(normal);
                float cosa = Vector3.Dot(normal, Vector3.UnitZ);
                float sinAlpha = MathF.Sin(MathF.Acos(cosa));
                sinAlpha = MathF.Max(sinAlpha, 0.1f);

                float capacity = Kc * MathF.Sqrt(velocityX * velocityX + velocityY * velocityY) * sinAlpha * MathF.Min(GridPoints[x, y].WaterHeight, 0.01f) / 0.01f;
                float delta = capacity - GridPoints[x, y].SuspendedSediment;

                if (delta > 0.0f)
                {
                    float d = Ks * delta;
                    heightMap.Height[x, y] -= d;
                    GridPoints[x, y].SuspendedSediment = MathF.Max(0.0f, GridPoints[x, y].SuspendedSediment + d);
                }
                else if (delta < 0.0f)
                {
                    float d = Kd * delta;
                    heightMap.Height[x, y] -= d;
                    GridPoints[x, y].SuspendedSediment = MathF.Max(0.0f, GridPoints[x, y].SuspendedSediment + d);
                }

                float fromPosX = x - velocityX * myDt;
                float fromPosY = y - velocityY * myDt;

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

    private void WaterDecrease()
    {
        float Ke = 0.003f;

        for (int y = 0; y < GridPoints.GetLength(1); y++)
        {
            for (int x = 0; x < GridPoints.GetLength(0); x++)
            {
                GridPoints[x, y].SuspendedSediment = GridPoints[x, y].TempSediment;

                GridPoints[x, y].WaterHeight = MathF.Max(0, GridPoints[x, y].WaterHeight - Ke * myDt);
            }
        }
    }
}
