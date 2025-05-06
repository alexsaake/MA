using System.Numerics;

namespace ProceduralLandscapeGeneration.Config
{
    internal interface IParticleWindErosionConfiguration : IDisposable
    {
        uint MaxAge { get; set; }
        float Suspension { get; set; }
        float Gravity { get; set; }
        float MaxDiff { get; set; }
        float Settling { get; set; }
        Vector3 PersistentSpeed { get; set; }

        void Initialize();
    }
}