﻿namespace ProceduralLandscapeGeneration.Rendering;

internal interface IRenderer : IDisposable
{
    void Initialize();
    void Update();
    void Draw();
}