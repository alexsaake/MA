using Raylib_cs;
using System.Text;

namespace ProceduralLandscapeGeneration
{
    internal class ComputeShader : IComputeShader
    {
        public uint Id { get; private set; }

        public unsafe uint CreateComputeShaderProgram(string fileName)
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

        public unsafe uint CreateMeshShaderProgram(string meshShaderFileName, string fragmentShaderFileName)
        {
            byte[] bytes2 = Encoding.ASCII.GetBytes(meshShaderFileName);
            sbyte[] sbytes2 = Array.ConvertAll(bytes2, Convert.ToSByte);
            fixed (sbyte* ptr2 = sbytes2)
            {
                sbyte* golLogicCode2 = Raylib.LoadFileText(ptr2);
                uint golLogicShader2 = Rlgl.CompileShader(golLogicCode2, (int)ShaderType.Mesh);
                byte[] bytes3 = Encoding.ASCII.GetBytes(fragmentShaderFileName);
                sbyte[] sbytes3 = Array.ConvertAll(bytes3, Convert.ToSByte);
                fixed (sbyte* ptr3 = sbytes3)
                {
                    sbyte* golLogicCode3 = Raylib.LoadFileText(ptr3);
                    uint golLogicShader3 = Rlgl.CompileShader(golLogicCode3, (int)ShaderType.Fragment);
                    Id = Rlgl.LoadMeshShaderProgram(golLogicShader2, golLogicShader3);
                }
            }

            return Id;
        }

        public unsafe uint CreateMeshShaderProgram(string taskShaderFileName, string meshShaderFileName, string fragmentShaderFileName)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(taskShaderFileName);
            sbyte[] sbytes = Array.ConvertAll(bytes, Convert.ToSByte);
            fixed (sbyte* ptr = sbytes)
            {
                sbyte* golLogicCode = Raylib.LoadFileText(ptr);
                uint golLogicShader = Rlgl.CompileShader(golLogicCode, (int)ShaderType.Task);
                byte[] bytes2 = Encoding.ASCII.GetBytes(meshShaderFileName);
                sbyte[] sbytes2 = Array.ConvertAll(bytes2, Convert.ToSByte);
                fixed (sbyte* ptr2 = sbytes2)
                {
                    sbyte* golLogicCode2 = Raylib.LoadFileText(ptr2);
                    uint golLogicShader2 = Rlgl.CompileShader(golLogicCode2, (int)ShaderType.Mesh);
                    byte[] bytes3 = Encoding.ASCII.GetBytes(fragmentShaderFileName);
                    sbyte[] sbytes3 = Array.ConvertAll(bytes3, Convert.ToSByte);
                    fixed (sbyte* ptr3 = sbytes3)
                    {
                        sbyte* golLogicCode3 = Raylib.LoadFileText(ptr3);
                        uint golLogicShader3 = Rlgl.CompileShader(golLogicCode3, (int)ShaderType.Fragment);
                        Id = Rlgl.LoadMeshShaderProgram(golLogicShader, golLogicShader2, golLogicShader3);
                    }
                }
            }

            return Id;
        }

        public void Dispose()
        {
            Rlgl.UnloadShaderProgram(Id);
        }
    }
}
