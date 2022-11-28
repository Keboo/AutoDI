using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoDI.Build;

internal class Injector
{
    private readonly MethodDefinition _method;
    private int _insertionPoint;

    public Injector(MethodDefinition method)
    {
        _method = method ?? throw new ArgumentNullException(nameof(method));
    }

    public Instruction Insert(OpCode code, TypeReference type)
    {
        return Insert(Instruction.Create(code, type));
    }

    public Instruction Insert(OpCode code, MethodReference method)
    {
        return Insert(Instruction.Create(code, method));
    }

    public Instruction Insert(OpCode code, FieldReference field)
    {
        return Insert(Instruction.Create(code, field));
    }

    public Instruction Insert(OpCode code, int value)
    {
        return Insert(Instruction.Create(code, value));
    }

    public Instruction Insert(OpCode code)
    {
        return Insert(Instruction.Create(code));
    }

    public Instruction Insert(OpCode code, VariableDefinition variable)
    {
        return Insert(Instruction.Create(code, variable));
    }

    public Instruction Insert(OpCode code, Instruction target)
    {
        return Insert(Instruction.Create(code, target));
    }

    public Instruction Insert(OpCode code, ParameterDefinition parameter)
    {
        return Insert(Instruction.Create(code, parameter));
    }

    public Instruction Insert(OpCode code, string value)
    {
        return Insert(Instruction.Create(code, value));
    }

    public Instruction Insert(OpCode code, long value)
    {
        return Insert(Instruction.Create(code, value));
    }

    public Instruction Insert(OpCode code, double value)
    {
        return Insert(Instruction.Create(code, value));
    }

    public Instruction Insert(OpCode code, float value)
    {
        return Insert(Instruction.Create(code, value));
    }

    public Instruction Insert(Instruction instruction)
    {
        _method.Body.Instructions.Insert(_insertionPoint++, instruction);
        return instruction;
    }

    public void Insert(IEnumerable<Instruction> instructions)
    {
        foreach (Instruction instruction in instructions)
        {
            Insert(instruction);
        }
    }

}