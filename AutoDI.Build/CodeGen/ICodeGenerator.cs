using Mono.Cecil;

namespace AutoDI.Build.CodeGen;

internal interface ICodeGenerator
{
    IMethodGenerator Method(MethodDefinition method);

    void Save();
}