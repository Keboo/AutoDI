using Mono.Cecil.Cil;

namespace AutoDI.Build.CodeGen
{
    internal interface IMethodGenerator
    {
        void Append(string code, Instruction instruction = null);
    }
}