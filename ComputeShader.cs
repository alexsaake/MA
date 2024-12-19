using Raylib_cs;
using System.Text;

namespace ProceduralLandscapeGeneration
{
    internal class ComputeShader : IComputeShader
    {
        public uint Id { get; private set; }

        public unsafe uint CreateShaderProgram(string fileName)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(fileName);
            sbyte[] sbytes = Array.ConvertAll(bytes, Convert.ToSByte);
            fixed (sbyte* ptr = sbytes)
            {
                sbyte* golLogicCode = Raylib.LoadFileText(ptr);
                uint golLogicShader = Rlgl.CompileShader(golLogicCode, (int)ShaderType.Compute);
                Id = Rlgl.LoadComputeShaderProgram(golLogicShader);
            }

            return Id;
        }

        public void Dispose()
        {
            Rlgl.UnloadShaderProgram(Id);
        }
    }
}
