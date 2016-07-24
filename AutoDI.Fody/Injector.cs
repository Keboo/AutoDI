using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoDI.Fody
{
    internal class Injector
    {
        private readonly MethodDefinition _constructor;
        private int _insertionPoint;

        public Injector(MethodDefinition constructor)
        {
            if (constructor == null) throw new ArgumentNullException(nameof(constructor));
            _constructor = constructor;

            _insertionPoint = FindInsertionPoint(constructor.Body);
        }

        public void Insert(OpCode code, TypeReference type)
        {
            Insert(Instruction.Create(code, type));
        }

        public void Insert(OpCode code, MethodReference method)
        {
            Insert(Instruction.Create(code, method));
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

        private static int FindInsertionPoint(MethodBody body)
        {
            var instructions = body.Instructions;
            bool seenBaseCall = false;
            return instructions.IndexOf(instructions.SkipWhile(x =>
            {
                if (x.OpCode != OpCodes.Nop && seenBaseCall)
                    return false;
                if (x.OpCode == OpCodes.Call)
                    seenBaseCall = true;
                return true;
            }).First());
        }

    }
}