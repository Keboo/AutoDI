using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoDI.AssemblyGenerator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace AutoDI.Fody.Tests
{
    extern alias AutoDIFody;

    [TestClass]
    public class CSharpCodeGeneratorTests
    {
        private static ModuleDefinition _testModule;
        private string _outputDirectory;

        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            var gen = new Generator();

            _testModule = (await gen.Execute()).SingleModule();
        }
        
        [TestInitialize]
        public void TestSetup()
        {
            _outputDirectory = Path.Combine(".", Path.GetRandomFileName());
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (Directory.Exists(_outputDirectory))
            {
                Directory.Delete(_outputDirectory, true);
            }
        }

        [TestMethod]
        public void ConstructorWithSingleParameterGeneratesExpectedSequencePoints()
        {
            TypeDefinition type = _testModule.GetType(
                $"{nameof(CSharpCodeGenrationTestsNamespace)}.{nameof(CSharpCodeGenrationTestsNamespace.Class1)}");
            MethodDefinition ctor = type.GetConstructors().Single();

            var generator = new AutoDIFody::AutoDI.Fody.CodeGen.CSharpCodeGenerator(_outputDirectory);
            var ctorGenerator = generator.Method(ctor);

            ctorGenerator.Append("if (foo == null)", Instruction.Create(OpCodes.Nop));
            ctorGenerator.Append(Environment.NewLine + "{" + Environment.NewLine);
            ctorGenerator.Append("    foo = GlobalDI.GetService<IService>();", Instruction.Create(OpCodes.Nop));
            ctorGenerator.Append(Environment.NewLine);
            ctorGenerator.Append("}", Instruction.Create(OpCodes.Nop));
            ctorGenerator.Append(Environment.NewLine);

            generator.Save();

            Assert.AreEqual(3, ctor.DebugInformation.SequencePoints.Count);

            SequencePoint first = ctor.DebugInformation.SequencePoints[0];
            Assert.AreEqual(8, first.StartLine);
            Assert.AreEqual(8, first.EndLine);
            Assert.AreEqual(13, first.StartColumn);
            Assert.AreEqual(29, first.EndColumn);
            SequencePoint second = ctor.DebugInformation.SequencePoints[1];
            Assert.AreEqual(10, second.StartLine);
            Assert.AreEqual(10, second.EndLine);
            Assert.AreEqual(17, second.StartColumn);
            Assert.AreEqual(55, second.EndColumn);
            SequencePoint third = ctor.DebugInformation.SequencePoints[2];
            Assert.AreEqual(11, third.StartLine);
            Assert.AreEqual(11, third.EndLine);
            Assert.AreEqual(13, third.StartColumn);
            Assert.AreEqual(14, third.EndColumn);
        }

        [TestMethod]
        public void NestedClassGeneratesExpectedSequencePoints()
        {
            TypeDefinition type = _testModule.GetType(
                $"{nameof(CSharpCodeGenrationTestsNamespace)}.{nameof(CSharpCodeGenrationTestsNamespace.Outer)}/{nameof(CSharpCodeGenrationTestsNamespace.Outer.Nested)}");
            MethodDefinition ctor = type.GetConstructors().Single();

            var generator = new AutoDIFody::AutoDI.Fody.CodeGen.CSharpCodeGenerator(_outputDirectory);
            var ctorGenerator = generator.Method(ctor);

            ctorGenerator.Append("//Comment", Instruction.Create(OpCodes.Nop));

            generator.Save();

            Assert.AreEqual(1, ctor.DebugInformation.SequencePoints.Count);

            SequencePoint first = ctor.DebugInformation.SequencePoints[0];
            Assert.AreEqual(10, first.StartLine);
            Assert.AreEqual(10, first.EndLine);
            Assert.AreEqual(17, first.StartColumn);
            Assert.AreEqual(26, first.EndColumn);
        }

        [TestMethod]
        public void CanGenerateClassWithPublicConstructor()
        {
            TypeDefinition type = _testModule.GetType(
                $"{nameof(CSharpCodeGenrationTestsNamespace)}.{nameof(CSharpCodeGenrationTestsNamespace.Class1)}");
            MethodDefinition ctor = type.GetConstructors().Single();

            var generator = new AutoDIFody::AutoDI.Fody.CodeGen.CSharpCodeGenerator(_outputDirectory);
            var ctorGenerator = generator.Method(ctor);

            ctorGenerator.Append("if (foo == null)", Instruction.Create(OpCodes.Nop));
            ctorGenerator.Append(Environment.NewLine + "{" + Environment.NewLine);
            ctorGenerator.Append("    foo = GlobalDI.GetService<IService>();", Instruction.Create(OpCodes.Nop));
            ctorGenerator.Append(Environment.NewLine);
            ctorGenerator.Append("}", Instruction.Create(OpCodes.Nop));
            ctorGenerator.Append(Environment.NewLine);

            generator.Save();

            string result = File.ReadAllText(Directory.EnumerateFiles(_outputDirectory).Single());

            string expected =
                "namespace " + nameof(CSharpCodeGenrationTestsNamespace) + Environment.NewLine +
                "{" + Environment.NewLine +
                "    public class " + nameof(CSharpCodeGenrationTestsNamespace.Class1) + Environment.NewLine +
                "    {" + Environment.NewLine +
                "        //Generated by AutoDI" + Environment.NewLine +
                $"        public {nameof(CSharpCodeGenrationTestsNamespace.Class1)}([AutoDI.DependencyAttribute]{nameof(CSharpCodeGenrationTestsNamespace)}.{nameof(CSharpCodeGenrationTestsNamespace.IService)} foo = null)" + Environment.NewLine +
                "        {" + Environment.NewLine +
                "            if (foo == null)" + Environment.NewLine +
                "            {" + Environment.NewLine +
                "                foo = GlobalDI.GetService<IService>();" + Environment.NewLine +
                "            }" + Environment.NewLine +
                "        }" + Environment.NewLine +
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine;
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void CanGenerateClassWithPropertyInjection()
        {
            TypeDefinition type = _testModule.GetType(
                $"{nameof(CSharpCodeGenrationTestsNamespace)}.{nameof(CSharpCodeGenrationTestsNamespace.Class3)}");
            MethodDefinition ctor = type.GetConstructors().Single();

            var generator = new AutoDIFody::AutoDI.Fody.CodeGen.CSharpCodeGenerator(_outputDirectory);
            var ctorGenerator = generator.Method(ctor);

            ctorGenerator.Append("if (Service == null)", Instruction.Create(OpCodes.Nop));
            ctorGenerator.Append(Environment.NewLine + "{" + Environment.NewLine);
            ctorGenerator.Append("    Service = GlobalDI.GetService<IService>();", Instruction.Create(OpCodes.Nop));
            ctorGenerator.Append(Environment.NewLine);
            ctorGenerator.Append("}", Instruction.Create(OpCodes.Nop));
            ctorGenerator.Append(Environment.NewLine);

            generator.Save();

            string result = File.ReadAllText(Directory.EnumerateFiles(_outputDirectory).Single());

            string expected =
                "namespace " + nameof(CSharpCodeGenrationTestsNamespace) + Environment.NewLine +
                "{" + Environment.NewLine +
                "    public class " + nameof(CSharpCodeGenrationTestsNamespace.Class3) + Environment.NewLine +
                "    {" + Environment.NewLine +
                "        //Generated by AutoDI" + Environment.NewLine +
                $"        public {nameof(CSharpCodeGenrationTestsNamespace.Class3)}()" + Environment.NewLine +
                "        {" + Environment.NewLine +
                "            if (Service == null)" + Environment.NewLine +
                "            {" + Environment.NewLine +
                "                Service = GlobalDI.GetService<IService>();" + Environment.NewLine +
                "            }" + Environment.NewLine +
                "        }" + Environment.NewLine +
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine;
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void CanGenerateClassWithMethodInjection()
        {
            TypeDefinition type = _testModule.GetType(
                $"{nameof(CSharpCodeGenrationTestsNamespace)}.{nameof(CSharpCodeGenrationTestsNamespace.Class4)}");
            MethodDefinition method = type.GetMethods().Single();

            var generator = new AutoDIFody::AutoDI.Fody.CodeGen.CSharpCodeGenerator(_outputDirectory);
            var ctorGenerator = generator.Method(method);

            ctorGenerator.Append("if (foo == null)", Instruction.Create(OpCodes.Nop));
            ctorGenerator.Append(Environment.NewLine + "{" + Environment.NewLine);
            ctorGenerator.Append("    foo = GlobalDI.GetService<IService>();", Instruction.Create(OpCodes.Nop));
            ctorGenerator.Append(Environment.NewLine);
            ctorGenerator.Append("}", Instruction.Create(OpCodes.Nop));
            ctorGenerator.Append(Environment.NewLine);

            generator.Save();

            string result = File.ReadAllText(Directory.EnumerateFiles(_outputDirectory).Single());

            string expected =
                "namespace " + nameof(CSharpCodeGenrationTestsNamespace) + Environment.NewLine +
                "{" + Environment.NewLine +
                "    public class " + nameof(CSharpCodeGenrationTestsNamespace.Class4) + Environment.NewLine +
                "    {" + Environment.NewLine +
                "        //Generated by AutoDI" + Environment.NewLine +
                $"        public System.Int32 {nameof(CSharpCodeGenrationTestsNamespace.Class4.DoStuff)}([AutoDI.DependencyAttribute]{nameof(CSharpCodeGenrationTestsNamespace)}.{nameof(CSharpCodeGenrationTestsNamespace.IService)} foo = null)" + Environment.NewLine +
                "        {" + Environment.NewLine +
                "            if (foo == null)" + Environment.NewLine +
                "            {" + Environment.NewLine +
                "                foo = GlobalDI.GetService<IService>();" + Environment.NewLine +
                "            }" + Environment.NewLine +
                "        }" + Environment.NewLine +
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine;
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void CanGenerateGenericClass()
        {
            TypeDefinition type = _testModule.GetType(
                $"{nameof(CSharpCodeGenrationTestsNamespace)}.{nameof(CSharpCodeGenrationTestsNamespace.Class2<object>)}`1");
            MethodDefinition ctor = type.GetConstructors().Single();

            var generator = new AutoDIFody::AutoDI.Fody.CodeGen.CSharpCodeGenerator(_outputDirectory);
            var ctorGenerator = generator.Method(ctor);

            ctorGenerator.Append("");

            generator.Save();

            string result = File.ReadAllText(Directory.EnumerateFiles(_outputDirectory).Single());

            string expected =
                "namespace " + nameof(CSharpCodeGenrationTestsNamespace) + Environment.NewLine +
                "{" + Environment.NewLine +
                "    public class " + nameof(CSharpCodeGenrationTestsNamespace.Class2<object>) + "<TGeneric>" + Environment.NewLine +
                "    {" + Environment.NewLine +
                "        //Generated by AutoDI" + Environment.NewLine +
                $"        public {nameof(CSharpCodeGenrationTestsNamespace.Class2<object>)}([AutoDI.DependencyAttribute]{nameof(CSharpCodeGenrationTestsNamespace)}.{nameof(CSharpCodeGenrationTestsNamespace.IService)} foo = null, [AutoDI.DependencyAttribute]{nameof(CSharpCodeGenrationTestsNamespace)}.{nameof(CSharpCodeGenrationTestsNamespace.IService2)} bar = null)" + Environment.NewLine +
                "        {" + Environment.NewLine +
                "        }" + Environment.NewLine +
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine;
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void CanGenerateNestedClass()
        {
            TypeDefinition type = _testModule.GetType(
                $"{nameof(CSharpCodeGenrationTestsNamespace)}.{nameof(CSharpCodeGenrationTestsNamespace.Outer)}/{nameof(CSharpCodeGenrationTestsNamespace.Outer.Nested)}");
            MethodDefinition ctor = type.GetConstructors().Single();

            var generator = new AutoDIFody::AutoDI.Fody.CodeGen.CSharpCodeGenerator(_outputDirectory);
            var ctorGenerator = generator.Method(ctor);

            ctorGenerator.Append("");

            generator.Save();

            string result = File.ReadAllText(Directory.EnumerateFiles(_outputDirectory).Single());

            string expected =
                "namespace " + nameof(CSharpCodeGenrationTestsNamespace) + Environment.NewLine +
                "{" + Environment.NewLine +
                "    public class " + nameof(CSharpCodeGenrationTestsNamespace.Outer) + Environment.NewLine +
                "    {" + Environment.NewLine +
                "        public class " + nameof(CSharpCodeGenrationTestsNamespace.Outer.Nested) + Environment.NewLine +
                "        {" + Environment.NewLine +
                "            //Generated by AutoDI" + Environment.NewLine +
                $"            public {nameof(CSharpCodeGenrationTestsNamespace.Outer.Nested)}([AutoDI.DependencyAttribute]{nameof(CSharpCodeGenrationTestsNamespace)}.{nameof(CSharpCodeGenrationTestsNamespace.IService)} foo = null)" + Environment.NewLine +
                "            {" + Environment.NewLine +
                "            }" + Environment.NewLine +
                "        }" + Environment.NewLine +
                "    }" + Environment.NewLine +
                "}" + Environment.NewLine;
            Assert.AreEqual(expected, result);
        }

        [DataTestMethod]
        [DataRow("public", "Public")]
        [DataRow("internal", "Internal")]
        [DataRow("protected internal", "ProtectedInternal")]
        [DataRow("protected", "Protected")]
        [DataRow("private protected", "PrivateProtected")]
        [DataRow("private", "Private")]
        public void CanGetMethodProtectionModifier(string modifier, string methodName)
        {
            TypeDefinition type = _testModule.GetType(
                $"{nameof(CSharpCodeGenrationTestsNamespace)}.{nameof(CSharpCodeGenrationTestsNamespace.MethodProtectionModifiers)}");

            MethodDefinition method = type.Methods.Single(x => x.Name == methodName);

            Assert.AreEqual(modifier, AutoDIFody::AutoDI.Fody.TypeReferenceMixins.ProtectionModifierCSharp(method.Attributes));
        }

        [DataTestMethod]
        [DataRow("public", "Public")]
        [DataRow("internal", "Internal")]
        [DataRow("protected internal", "ProtectedInternal")]
        [DataRow("protected", "Protected")]
        [DataRow("private protected", "PrivateProtected")]
        [DataRow("private", "Private")]
        public void CanGetClassProtectionModifier(string modifier, string className)
        {
            TypeDefinition type = _testModule.GetType(
                $"{nameof(CSharpCodeGenrationTestsNamespace)}.{className}");

            TypeDefinition nestedType = _testModule.GetType(
                $"{nameof(CSharpCodeGenrationTestsNamespace)}.{nameof(CSharpCodeGenrationTestsNamespace.ClassProtectedModifiers)}/{className}");

            Assert.AreEqual(modifier, AutoDIFody::AutoDI.Fody.TypeReferenceMixins.ProtectionModifierCSharp(nestedType.Attributes));
            if (type != null)
            {
                Assert.AreEqual(modifier, AutoDIFody::AutoDI.Fody.TypeReferenceMixins.ProtectionModifierCSharp(type.Attributes));
            }
        }

    }

    //<assembly>
    //<ref: AutoDI />
    namespace CSharpCodeGenrationTestsNamespace
    {
        using AutoDI;

        public interface IService
        { }

        public interface IService2
        { }

        public class Class1
        {
            public Class1([Dependency]IService foo = null)
            {
                
            }
        }

        public class Class2<TGeneric>
        {
            public Class2([Dependency]IService foo = null, [Dependency]IService2 bar = null)
            { }
        }

        public class Class3
        {
            [Dependency]
            public IService Service { get; }
        }

        public class Class4
        {
            public int DoStuff([Dependency] IService foo = null)
            {
                return 0;
            }
        }

        public class Outer
        {
            public class Nested
            {
                public Nested([Dependency]IService foo =  null)
                {
                    
                }
            }
        }

        public class MethodProtectionModifiers
        {
            public void Public() { }
            internal void Internal() { }
            protected internal void ProtectedInternal() { }
            protected void Protected() { }
            private protected void PrivateProtected() { }
            private void Private() { }
        }
        
        public class ClassProtectedModifiers
        {
            public class Public
            { }

            internal class Internal
            { }

            protected internal class ProtectedInternal
            { }

            protected class Protected
            { }

            private protected class PrivateProtected
            { }

            private class Private
            { }
        }

        public class Public
        { }

        internal class Internal
        { }
    }
    //</assembly>
}