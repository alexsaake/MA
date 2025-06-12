using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Renderers
{
    internal interface ICamera
    {
        Camera3D Instance { get; }
        Vector3 Position { get; }

        void Initialize();
        void Update();
    }
}