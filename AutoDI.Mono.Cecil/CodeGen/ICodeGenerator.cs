using Mono.Cecil;

namespace AutoDI.Mono.Cecil.CodeGen
{
    internal interface ICodeGenerator
    {
        IMethodGenerator Method(MethodDefinition method);

        void Save();
    }
}