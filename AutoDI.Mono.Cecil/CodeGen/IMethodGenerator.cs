using Mono.Cecil.Cil;

namespace AutoDI.Mono.Cecil.CodeGen
{
    internal interface IMethodGenerator
    {
        void Append(string code, Instruction instruction = null);
    }
}