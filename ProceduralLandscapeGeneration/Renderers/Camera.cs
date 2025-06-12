using ProceduralLandscapeGeneration.Configurations.MapGeneration;
using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Renderers;

internal class Camera : ICamera
{
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;

    private Camera3D myCamera;

    public Camera3D Instance => myCamera;
    public Vector3 Position => myCamera.Position;

    public Camera(IMapGenerationConfiguration mapGenerationConfiguration)
    {
        myMapGenerationConfiguration = mapGenerationConfiguration;
    }

    public void Initialize()
    {
        Vector3 heightMapCenter = new Vector3(myMapGenerationConfiguration.HeightMapSideLength / 2, myMapGenerationConfiguration.HeightMapSideLength / 2, 0);
        Vector3 cameraPosition = heightMapCenter + new Vector3(myMapGenerationConfiguration.HeightMapSideLength / 2, -myMapGenerationConfiguration.HeightMapSideLength / 2, myMapGenerationConfiguration.HeightMapSideLength / 2);
        myCamera = new(cameraPosition, heightMapCenter, Vector3.UnitZ, 45.0f, CameraProjection.Perspective);
        Raylib.UpdateCamera(ref myCamera, myMapGenerationConfiguration.CameraMode);
    }

    public void Update()
    {
        if (myMapGenerationConfiguration.CameraMode == CameraMode.Custom)
        {
            float cameraMoveSpeed = 50.0f * Raylib.GetFrameTime();
            float cameraRotationSpeed = 0.5f * Raylib.GetFrameTime();

            if (Raylib.IsKeyDown(KeyboardKey.W))
            {
                Raylib.CameraMoveToTarget(ref myCamera, -cameraMoveSpeed);
            }
            else if (Raylib.IsKeyDown(KeyboardKey.S))
            {
                Raylib.CameraMoveToTarget(ref myCamera, cameraMoveSpeed);
            }
            Raylib.CameraMoveToTarget(ref myCamera, -Raylib.GetMouseWheelMove() * cameraMoveSpeed);
            if (Raylib.IsKeyDown(KeyboardKey.A))
            {
                Raylib.CameraYaw(ref myCamera, -cameraRotationSpeed, true);
            }
            else if (Raylib.IsKeyDown(KeyboardKey.D))
            {
                Raylib.CameraYaw(ref myCamera, cameraRotationSpeed, true);
            }
            if (Raylib.IsKeyDown(KeyboardKey.LeftShift))
            {
                Raylib.CameraPitch(ref myCamera, -cameraRotationSpeed, true, true, false);
            }
            else if (Raylib.IsKeyDown(KeyboardKey.LeftControl))
            {
                Raylib.CameraPitch(ref myCamera, cameraRotationSpeed, true, true, false);
            }

            if (myCamera.Position.Z < 0)
            {
                myCamera.Position.Z = 0;
            }
        }
        Raylib.UpdateCamera(ref myCamera, myMapGenerationConfiguration.CameraMode);
    }
}
