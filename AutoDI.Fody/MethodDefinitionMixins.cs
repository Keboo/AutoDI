using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace AutoDI.Fody
{
    internal static class MethodDefinitionMixins
    {
        //Based on example from here: https://stackoverflow.com/questions/16430947/emit-call-to-system-lazyt-constructor-with-mono-cecil
        public static MethodReference MakeGenericTypeConstructor(this MethodReference self, params TypeReference[] args)
        {
            var reference = new MethodReference(
                self.Name,
                self.ReturnType,
                self.DeclaringType.MakeGenericInstanceType(args))
            {
                HasThis = self.HasThis,
                ExplicitThis = self.ExplicitThis,
                CallingConvention = self.CallingConvention
            };

            foreach (var parameter in self.Parameters)
            {
                reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
            }

            foreach (var genericParam in self.GenericParameters)
            {
                reference.GenericParameters.Add(new GenericParameter(genericParam.Name, reference));
            }

            return reference;
        }
    }
}