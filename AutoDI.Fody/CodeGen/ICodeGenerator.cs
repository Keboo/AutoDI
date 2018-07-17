using Mono.Cecil;

namespace AutoDI.Fody.CodeGen
{
    internal interface ICodeGenerator
    {
        IMethodGenerator Method(MethodDefinition method);

        void Save();
    }
}