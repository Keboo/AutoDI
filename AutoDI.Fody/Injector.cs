using Mono.Cecil;
using Mono.Cecil.Cil;
using System;

namespace AutoDI.Fody
{
    internal class Injector
    {
        private readonly MethodDefinition _constructor;
        private int _insertionPoint;

        public Injector(MethodDefinition constructor)
        {
            _constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
        }

        public void Insert(OpCode code, TypeReference type)
        {
            Insert(Instruction.Create(code, type));
        }

        public void Insert(OpCode code, MethodReference method)
        {
            Insert(Instruction.Create(code, method));
        }

        public void Insert(OpCode code, FieldReference field)
        {
            Insert(Instruction.Create(code, field));
        }

        public void Insert(OpCode code, int value)
        {
            Insert(Instruction.Create(code, value));
        }

        public void Insert(OpCode code)
        {
            Insert(Instruction.Create(code));
        }

        public void Insert(OpCode code, VariableDefinition variable)
        {
            Insert(Instruction.Create(code, variable));
        }

        public void Insert(OpCode code, Instruction target)
        {
            Insert(Instruction.Create(code, target));
        }

        public void Insert(OpCode code, ParameterDefinition parameter)
        {
            Insert(Instruction.Create(code, parameter));
        }

        public void Insert(OpCode code, string value)
        {
            Insert(Instruction.Create(code, value));
        }

        public void Insert(OpCode code, long value)
        {
            Insert(Instruction.Create(code, value));
        }

        public void Insert(OpCode code, double value)
        {
            Insert(Instruction.Create(code, value));
        }

        public void Insert(OpCode code, float value)
        {
            Insert(Instruction.Create(code, value));
        }

        public void Insert(Instruction instruction)
        {
            _constructor.Body.Instructions.Insert(_insertionPoint++, instruction);
        }

    }
}