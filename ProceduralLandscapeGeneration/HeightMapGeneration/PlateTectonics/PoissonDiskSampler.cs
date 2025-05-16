using ProceduralLandscapeGeneration.Common;
using System.Numerics;

namespace ProceduralLandscapeGeneration.HeightMapGeneration.PlateTectonics;

//https://www.youtube.com/watch?v=7WcmyxyFO7o
internal class PoissonDiskSampler : IPoissonDiskSampler
{
    private readonly IRandom myRandom;

    public PoissonDiskSampler(IRandom random)
    {
        myRandom = random;
    }

    public List<Vector2> GeneratePoints(float radius, uint heightMapSideLength, int samplesBeforeRejection = 30)
    {
        float cellSize = radius / MathF.Sqrt(2);

        int[,] grid = new int[(int)MathF.Ceiling(heightMapSideLength / cellSize), (int)MathF.Ceiling(heightMapSideLength / cellSize)];
        List<Vector2> points = new List<Vector2>();
        List<Vector2> spawnPoints = new List<Vector2>();

        spawnPoints.Add(new Vector2(heightMapSideLength) / 2);
        while (spawnPoints.Count > 0)
        {
            int spawnIndex = myRandom.Next(0, spawnPoints.Count);
            Vector2 spawnCenter = spawnPoints[spawnIndex];

            bool candidateAccepted = false;

            for (int i = 0; i < samplesBeforeRejection; i++)
            {
                float angle = myRandom.NextFloat() * MathF.PI * 2;
                Vector2 direction = new Vector2(MathF.Sin(angle), MathF.Cos(angle));
                Vector2 candidate = spawnCenter + direction * myRandom.NextFloat(radius, radius * 2);
                if (IsValid(candidate, heightMapSideLength, cellSize, radius, points, grid))
                {
                    points.Add(candidate);
                    spawnPoints.Add(candidate);
                    grid[(int)(candidate.X / cellSize), (int)(candidate.Y / cellSize)] = points.Count;
                    candidateAccepted = true;
                    break;
                }
            }

            if (!candidateAccepted)
            {
                spawnPoints.RemoveAt(spawnIndex);
            }

        }

        return points;
    }

    private bool IsValid(Vector2 candidate, uint heightMapSideLength, float cellSize, float radius, List<Vector2> points, int[,] grid)
    {
        if (candidate.X >= 0 && candidate.X < heightMapSideLength
        && candidate.Y >= 0 && candidate.Y < heightMapSideLength)
        {
            int cellX = (int)(candidate.X / cellSize);
            int cellY = (int)(candidate.Y / cellSize);
            int searchhStartX = (int)MathF.Max(0, cellX - 2);
            int searchhEndX = (int)MathF.Min(cellX + 2, grid.GetLength(0) - 1);
            int searchhStartY = (int)MathF.Max(0, cellY - 2);
            int searchhEndY = (int)MathF.Min(cellY + 2, grid.GetLength(1) - 1);

            for (int x = searchhStartX; x <= searchhEndX; x++)
            {
                for (int y = searchhStartY; y <= searchhEndY; y++)
                {
                    int pointIndex = grid[x, y] - 1;
                    if (pointIndex != -1)
                    {
                        float squaredDistance = (candidate - points[pointIndex]).LengthSquared();
                        if (squaredDistance < radius * radius)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }
        return false;
    }
}
