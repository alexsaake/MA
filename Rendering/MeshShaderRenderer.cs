using ProceduralLandscapeGeneration.Common;
using ProceduralLandscapeGeneration.Simulation;
using ProceduralLandscapeGeneration.Simulation.CPU;
using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Rendering
{
    internal class MeshShaderRenderer : IRenderer
    {
        private readonly IErosionSimulator myErosionSimulator;

        private Shader myMeshShader;
        private int myViewPositionLocation;
        private Camera3D myCamera;

        private uint myHeightMapShaderBufferId;
        private uint myMeshletCount;
        private bool myIsUpdateAvailable;
        private bool myIsDisposed;

        public MeshShaderRenderer(IErosionSimulator erosionSimulator)
        {
            myErosionSimulator = erosionSimulator;
        }

        public void Initialize()
        {
            myMeshShader = Raylib.LoadMeshShader("Rendering/Shaders/MeshShader.glsl", "Rendering/Shaders/FragmentShader.glsl");

            Vector3 heightMapCenter = new Vector3(Configuration.HeightMapSideLength / 2, Configuration.HeightMapSideLength / 2, 0);
            Vector3 lightDirection = new Vector3(0, Configuration.HeightMapSideLength, -Configuration.HeightMapSideLength / 2);
            int lightDirectionLocation = Raylib.GetShaderLocation(myMeshShader, "lightDirection");
            unsafe
            {
                Raylib.SetShaderValue(myMeshShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);
            }
            myViewPositionLocation = Raylib.GetShaderLocation(myMeshShader, "viewPosition");

            Vector3 cameraPosition = heightMapCenter + new Vector3(Configuration.HeightMapSideLength / 2, -Configuration.HeightMapSideLength / 2, Configuration.HeightMapSideLength / 2);
            myCamera = new(cameraPosition, heightMapCenter, Vector3.UnitZ, 45.0f, CameraProjection.Perspective);
            Raylib.UpdateCamera(ref myCamera, CameraMode.Custom);

            myMeshletCount = CalculateMeshletCount();

            if (myErosionSimulator is ErosionSimulatorCPU)
            {
                myErosionSimulator.ErosionIterationFinished += OnErosionIterationFinished;
                CreateShaderBuffer();
            }
            else
            {
                myHeightMapShaderBufferId = myErosionSimulator.HeightMapShaderBufferId;
            }
        }

        private unsafe void CreateShaderBuffer()
        {
            HeightMap heightMap = myErosionSimulator.HeightMap;
            float[] heightMapValues = heightMap.Get1DHeightMapValues();

            uint heightMapShaderBufferSize = (uint)heightMapValues.Length * sizeof(float);
            fixed (float* heightMapValuesPointer = heightMapValues)
            {
                myHeightMapShaderBufferId = Rlgl.LoadShaderBuffer(heightMapShaderBufferSize, heightMapValuesPointer, Rlgl.DYNAMIC_COPY);
            }
        }

        private void OnErosionIterationFinished(object? sender, EventArgs e)
        {
            myIsUpdateAvailable = true;
        }

        private static uint CalculateMeshletCount()
        {
            const float chunkSize = 7.0f;
            float meshletSideLength = MathF.Ceiling(Configuration.HeightMapSideLength / chunkSize);
            return (uint)(meshletSideLength * meshletSideLength);
        }

        public unsafe void Update()
        {
            if (myIsUpdateAvailable)
            {
                UpdateShaderBuffer();

                myIsUpdateAvailable = false;
            }

            Raylib.UpdateCamera(ref myCamera, CameraMode.Custom);
            Vector3 viewPosition = myCamera.Position;
            Raylib.SetShaderValue(myMeshShader, myViewPositionLocation, &viewPosition, ShaderUniformDataType.Vec3);
        }

        private unsafe void UpdateShaderBuffer()
        {
            HeightMap heightMap = myErosionSimulator.HeightMap;
            float[] heightMapValues = heightMap.Get1DHeightMapValues();

            uint heightMapShaderBufferSize = (uint)heightMapValues.Length * sizeof(float);
            fixed (float* heightMapValuesPointer = heightMapValues)
            {
                Rlgl.UpdateShaderBuffer(myHeightMapShaderBufferId, heightMapValuesPointer, heightMapShaderBufferSize, 0);
            }
        }

        public void Draw()
        {
            Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.SkyBlue);
                Raylib.BeginShaderMode(myMeshShader);
                    Raylib.BeginMode3D(myCamera);
                        Rlgl.EnableShader(myMeshShader.Id);
                            Rlgl.BindShaderBuffer(myHeightMapShaderBufferId, 1);
                            Raylib.DrawMeshTasks(0, myMeshletCount);
                        Rlgl.DisableShader();
                    Raylib.EndMode3D();
                Raylib.EndShaderMode();
            Raylib.EndDrawing();
        }

        public void Dispose()
        {
            if (myIsDisposed)
            {
                return;
            }

            if (myErosionSimulator is ErosionSimulatorCPU)
            {
                Rlgl.UnloadShaderBuffer(myHeightMapShaderBufferId);
            }
            Raylib.UnloadShader(myMeshShader);

            myIsDisposed = true;
        }
    }
}
