using Mono.Cecil.Cil;

namespace AutoDI.Fody.CodeGen
{
    internal interface IMethodGenerator
    {
        void Append(string code, Instruction instruction);
    }
}