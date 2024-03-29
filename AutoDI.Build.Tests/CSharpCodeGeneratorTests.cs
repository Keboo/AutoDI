﻿using System.Threading.Tasks;

using AutoDI.AssemblyGenerator;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace AutoDI.Build.Tests
{
    extern alias AutoDIBuild;

    [TestClass]
    public class CSharpCodeGeneratorTests
    {
        private static ModuleDefinition _testModule = null!;
        private string _outputDirectory = null!;

        [TestInitialize]
        public async Task TestSetup()
        {
            Generator gen = new();

            _testModule = (await gen.Execute()).SingleModule();

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
                $"{nameof(CSharpCodeGenerationTestsNamespace)}.{nameof(CSharpCodeGenerationTestsNamespace.Class1)}");
            MethodDefinition ctor = type.GetConstructors().Single();

            AutoDIBuild::AutoDI.Build.CodeGen.CSharpCodeGenerator generator = new(_outputDirectory);
            var ctorGenerator = generator.Method(ctor);

            ctorGenerator.Append("if (ReferenceEquals(foo, null))", Instruction.Create(OpCodes.Nop));
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
            Assert.AreEqual(44, first.EndColumn);
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
                $"{nameof(CSharpCodeGenerationTestsNamespace)}.{nameof(CSharpCodeGenerationTestsNamespace.Outer)}/{nameof(CSharpCodeGenerationTestsNamespace.Outer.Nested)}");
            MethodDefinition ctor = type.GetConstructors().Single();

            AutoDIBuild::AutoDI.Build.CodeGen.CSharpCodeGenerator generator = new(_outputDirectory);
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
                $"{nameof(CSharpCodeGenerationTestsNamespace)}.{nameof(CSharpCodeGenerationTestsNamespace.Class1)}");
            MethodDefinition ctor = type.GetConstructors().Single();

            AutoDIBuild::AutoDI.Build.CodeGen.CSharpCodeGenerator generator = new(_outputDirectory);
            var ctorGenerator = generator.Method(ctor);

            ctorGenerator.Append("if (ReferenceEquals(foo, null))", Instruction.Create(OpCodes.Nop));
            ctorGenerator.Append(Environment.NewLine + "{" + Environment.NewLine);
            ctorGenerator.Append("    foo = GlobalDI.GetService<IService>();", Instruction.Create(OpCodes.Nop));
            ctorGenerator.Append(Environment.NewLine);
            ctorGenerator.Append("}", Instruction.Create(OpCodes.Nop));
            ctorGenerator.Append(Environment.NewLine);

            generator.Save();

            string result = File.ReadAllText(Directory.EnumerateFiles(_outputDirectory).Single());

            string expected =
                "namespace " + nameof(CSharpCodeGenerationTestsNamespace) + Environment.NewLine +
                "{" + Environment.NewLine +
                "    public class " + nameof(CSharpCodeGenerationTestsNamespace.Class1) + Environment.NewLine +
                "    {" + Environment.NewLine +
                "        //Generated by AutoDI" + Environment.NewLine +
                $"        public {nameof(CSharpCodeGenerationTestsNamespace.Class1)}([AutoDI.DependencyAttribute]{nameof(CSharpCodeGenerationTestsNamespace)}.{nameof(CSharpCodeGenerationTestsNamespace.IService)} foo = null)" + Environment.NewLine +
                "        {" + Environment.NewLine +
                "            if (ReferenceEquals(foo, null))" + Environment.NewLine +
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
                $"{nameof(CSharpCodeGenerationTestsNamespace)}.{nameof(CSharpCodeGenerationTestsNamespace.Class3)}");
            MethodDefinition ctor = type.GetConstructors().Single();

            AutoDIBuild::AutoDI.Build.CodeGen.CSharpCodeGenerator generator = new(_outputDirectory);
            var ctorGenerator = generator.Method(ctor);

            ctorGenerator.Append("if (ReferenceEquals(Service, null))", Instruction.Create(OpCodes.Nop));
            ctorGenerator.Append(Environment.NewLine + "{" + Environment.NewLine);
            ctorGenerator.Append("    Service = GlobalDI.GetService<IService>();", Instruction.Create(OpCodes.Nop));
            ctorGenerator.Append(Environment.NewLine);
            ctorGenerator.Append("}", Instruction.Create(OpCodes.Nop));
            ctorGenerator.Append(Environment.NewLine);

            generator.Save();

            string result = File.ReadAllText(Directory.EnumerateFiles(_outputDirectory).Single());

            string expected =
                "namespace " + nameof(CSharpCodeGenerationTestsNamespace) + Environment.NewLine +
                "{" + Environment.NewLine +
                "    public class " + nameof(CSharpCodeGenerationTestsNamespace.Class3) + Environment.NewLine +
                "    {" + Environment.NewLine +
                "        //Generated by AutoDI" + Environment.NewLine +
                $"        public {nameof(CSharpCodeGenerationTestsNamespace.Class3)}()" + Environment.NewLine +
                "        {" + Environment.NewLine +
                "            if (ReferenceEquals(Service, null))" + Environment.NewLine +
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
                $"{nameof(CSharpCodeGenerationTestsNamespace)}.{nameof(CSharpCodeGenerationTestsNamespace.Class4)}");
            MethodDefinition method = type.GetMethods().Single();

            AutoDIBuild::AutoDI.Build.CodeGen.CSharpCodeGenerator generator = new(_outputDirectory);
            var ctorGenerator = generator.Method(method);

            ctorGenerator.Append("if (ReferenceEquals(foo, null))", Instruction.Create(OpCodes.Nop));
            ctorGenerator.Append(Environment.NewLine + "{" + Environment.NewLine);
            ctorGenerator.Append("    foo = GlobalDI.GetService<IService>();", Instruction.Create(OpCodes.Nop));
            ctorGenerator.Append(Environment.NewLine);
            ctorGenerator.Append("}", Instruction.Create(OpCodes.Nop));
            ctorGenerator.Append(Environment.NewLine);

            generator.Save();

            string result = File.ReadAllText(Directory.EnumerateFiles(_outputDirectory).Single());

            string expected =
                "namespace " + nameof(CSharpCodeGenerationTestsNamespace) + Environment.NewLine +
                "{" + Environment.NewLine +
                "    public class " + nameof(CSharpCodeGenerationTestsNamespace.Class4) + Environment.NewLine +
                "    {" + Environment.NewLine +
                "        //Generated by AutoDI" + Environment.NewLine +
                $"        public System.Int32 {nameof(CSharpCodeGenerationTestsNamespace.Class4.DoStuff)}([AutoDI.DependencyAttribute]{nameof(CSharpCodeGenerationTestsNamespace)}.{nameof(CSharpCodeGenerationTestsNamespace.IService)} foo = null)" + Environment.NewLine +
                "        {" + Environment.NewLine +
                "            if (ReferenceEquals(foo, null))" + Environment.NewLine +
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
                $"{nameof(CSharpCodeGenerationTestsNamespace)}.{nameof(CSharpCodeGenerationTestsNamespace.Class2<object>)}`1");
            MethodDefinition ctor = type.GetConstructors().Single();

            AutoDIBuild::AutoDI.Build.CodeGen.CSharpCodeGenerator generator = new(_outputDirectory);
            var ctorGenerator = generator.Method(ctor);

            ctorGenerator.Append("");

            generator.Save();

            string result = File.ReadAllText(Directory.EnumerateFiles(_outputDirectory).Single());

            string expected =
                "namespace " + nameof(CSharpCodeGenerationTestsNamespace) + Environment.NewLine +
                "{" + Environment.NewLine +
                "    public class " + nameof(CSharpCodeGenerationTestsNamespace.Class2<object>) + "<TGeneric>" + Environment.NewLine +
                "    {" + Environment.NewLine +
                "        //Generated by AutoDI" + Environment.NewLine +
                $"        public {nameof(CSharpCodeGenerationTestsNamespace.Class2<object>)}([AutoDI.DependencyAttribute]{nameof(CSharpCodeGenerationTestsNamespace)}.{nameof(CSharpCodeGenerationTestsNamespace.IService)} foo = null, [AutoDI.DependencyAttribute]{nameof(CSharpCodeGenerationTestsNamespace)}.{nameof(CSharpCodeGenerationTestsNamespace.IService2)} bar = null)" + Environment.NewLine +
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
                $"{nameof(CSharpCodeGenerationTestsNamespace)}.{nameof(CSharpCodeGenerationTestsNamespace.Outer)}/{nameof(CSharpCodeGenerationTestsNamespace.Outer.Nested)}");
            MethodDefinition ctor = type.GetConstructors().Single();

            AutoDIBuild::AutoDI.Build.CodeGen.CSharpCodeGenerator generator = new(_outputDirectory);
            var ctorGenerator = generator.Method(ctor);

            ctorGenerator.Append("");

            generator.Save();

            string result = File.ReadAllText(Directory.EnumerateFiles(_outputDirectory).Single());

            string expected =
                "namespace " + nameof(CSharpCodeGenerationTestsNamespace) + Environment.NewLine +
                "{" + Environment.NewLine +
                "    public class " + nameof(CSharpCodeGenerationTestsNamespace.Outer) + Environment.NewLine +
                "    {" + Environment.NewLine +
                "        public class " + nameof(CSharpCodeGenerationTestsNamespace.Outer.Nested) + Environment.NewLine +
                "        {" + Environment.NewLine +
                "            //Generated by AutoDI" + Environment.NewLine +
                $"            public {nameof(CSharpCodeGenerationTestsNamespace.Outer.Nested)}([AutoDI.DependencyAttribute]{nameof(CSharpCodeGenerationTestsNamespace)}.{nameof(CSharpCodeGenerationTestsNamespace.IService)} foo = null)" + Environment.NewLine +
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
                $"{nameof(CSharpCodeGenerationTestsNamespace)}.{nameof(CSharpCodeGenerationTestsNamespace.MethodProtectionModifiers)}");

            MethodDefinition method = type.Methods.Single(x => x.Name == methodName);

            Assert.AreEqual(modifier, AutoDIBuild::AutoDI.Build.TypeReferenceMixins.ProtectionModifierCSharp(method.Attributes));
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
                $"{nameof(CSharpCodeGenerationTestsNamespace)}.{className}");

            TypeDefinition nestedType = _testModule.GetType(
                $"{nameof(CSharpCodeGenerationTestsNamespace)}.{nameof(CSharpCodeGenerationTestsNamespace.ClassProtectedModifiers)}/{className}");

            Assert.AreEqual(modifier, AutoDIBuild::AutoDI.Build.TypeReferenceMixins.ProtectionModifierCSharp(nestedType.Attributes));
            if (type != null)
            {
                Assert.AreEqual(modifier, AutoDIBuild::AutoDI.Build.TypeReferenceMixins.ProtectionModifierCSharp(type.Attributes));
            }
        }

        [TestMethod]
        [Description("Issue 150")]
        public void GeneratedSequencePointReferenceAbsoluteFilePath()
        {
            TypeDefinition type = _testModule.GetType(
                $"{nameof(CSharpCodeGenerationTestsNamespace)}.{nameof(CSharpCodeGenerationTestsNamespace.Class5)}");
            MethodDefinition ctor = type.GetConstructors().Single();

            AutoDIBuild::AutoDI.Build.CodeGen.CSharpCodeGenerator generator = new(_outputDirectory);
            var ctorGenerator = generator.Method(ctor);

            ctorGenerator.Append("object foo = null;", Instruction.Create(OpCodes.Nop));

            generator.Save();

            Assert.AreEqual(1, ctor.DebugInformation.SequencePoints.Count);

            SequencePoint first = ctor.DebugInformation.SequencePoints[0];
            string expectedFilePath = Path.Combine(Path.GetFullPath(_outputDirectory), $"{nameof(CSharpCodeGenerationTestsNamespace)}.{nameof(CSharpCodeGenerationTestsNamespace.Class5)}.g.cs");
            Assert.AreEqual(expectedFilePath, first.Document.Url);
        }

    }

    //<assembly>
    //<ref: AutoDI />
    namespace CSharpCodeGenerationTestsNamespace
    {
        using AutoDI;

        public interface IService
        { }

        public interface IService2
        { }

        public class Class1
        {
            public Class1([Dependency] IService foo = null!)
            {

            }
        }

        public class Class2<TGeneric>
        {
            public Class2([Dependency] IService foo = null!, [Dependency] IService2 bar = null!)
            { }
        }

        public class Class3
        {
            [Dependency]
            public IService Service { get; } = null!;
        }

        public class Class4
        {
            public static int DoStuff([Dependency] IService foo = null!)
            {
                return 0;
            }
        }

        public class Class5
        {

        }

        public class Outer
        {
            public class Nested
            {
                public Nested([Dependency] IService foo = null!)
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