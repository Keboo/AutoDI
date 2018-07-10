﻿using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Mono.Collections.Generic;

namespace AutoDI.Fody.CodeGen
{
    internal class CSharpCodeGenerator : ICodeGenerator
    {
        private readonly Dictionary<MethodDefinition, CSharpMethodGenerator>
            _methodGenerators = new Dictionary<MethodDefinition, CSharpMethodGenerator>();

        private readonly string _outputDirectory;

        public CSharpCodeGenerator(string outputDirectory)
        {
            _outputDirectory = outputDirectory;
        }

        public IMethodGenerator Method(MethodDefinition method)
        {
            if (!_methodGenerators.TryGetValue(method, out CSharpMethodGenerator methodGenerator))
            {
                _methodGenerators.Add(method, methodGenerator = new CSharpMethodGenerator(method));
            }
            return methodGenerator;
        }

        public void Save()
        {
            if (_methodGenerators.Values.All(x => x.IsEmpty)) return;
            try
            {
                Directory.Delete(_outputDirectory, true);
            }
            catch (IOException)
            { }
            Directory.CreateDirectory(_outputDirectory);
            
            foreach (CSharpMethodGenerator classGenerator in _methodGenerators.Values)
            {
                classGenerator.Save(_outputDirectory);
            }
        }

        private class CSharpMethodGenerator : IMethodGenerator
        {
            private static readonly Regex _newLinePattern = new Regex("(\r?\n)");
            private readonly List<KeyValuePair<string, Instruction>> _codeBlocks
                = new List<KeyValuePair<string, Instruction>>();
            private readonly MethodDefinition _method;
            private readonly Document _document;
            
            public bool IsEmpty => !_codeBlocks.Any();

            public CSharpMethodGenerator(MethodDefinition method)
            {
                _method = method;
                _document = new Document("") { Language = DocumentLanguage.CSharp };
            }

            public void Append(string code, Instruction instruction)
            {
                _codeBlocks.Add(new KeyValuePair<string, Instruction>(code, instruction));
            }

            public void Save(string outputDirectory)
            {
                string filePath = GetFilePath(outputDirectory, _method.DeclaringType.FullNameCSharp());
                _document.Url = filePath;


                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    int indentLevel = 0;
                    writer.WriteLine(indentLevel, $"namespace {_method.DeclaringType.Namespace}");
                    writer.WriteLine(indentLevel++, "{");

                    
                    writer.WriteLine(indentLevel, $"{_method.DeclaringType.Attributes.ProtectionModifierCSharp()} class {_method.DeclaringType.NameCSharp(true)}");
                    writer.WriteLine(indentLevel++, "{");

                    writer.WriteLine(indentLevel, "//Generated by AutoDI");
                    writer.WriteLine(indentLevel, GetMethodDeclaration());
                    writer.WriteLine(indentLevel++, "{");

                    int lineNumber = 8;
                    const int startingIndent = 12;
                    foreach (var pair in _codeBlocks)
                    {
                        int numLines = 0;
                        int indent = 0;
                        int lastLineLength = 0;
                        foreach (string line in _newLinePattern.Split(pair.Key))
                        {
                            if (line == "") continue;
                            if (numLines == 0)
                            {
                                indent = pair.Key.TakeWhile(char.IsWhiteSpace).Count();
                            }
                            lastLineLength = line.Length - indent;
                            if (_newLinePattern.IsMatch(line))
                            {
                                writer.WriteLine();
                            }
                            else
                            {
                                numLines++;
                                writer.Write(indentLevel, line);
                            }
                        }

                        Instruction instruction = pair.Value;

                        if (instruction != null)
                        {
                            var sequencePoint = new SequencePoint(instruction, _document)
                            {
                                StartLine = lineNumber,
                                EndLine = lineNumber + numLines - 1,
                                StartColumn = startingIndent + indent + 1
                            };
                            sequencePoint.EndColumn = sequencePoint.StartColumn + lastLineLength;

                            _method.DebugInformation.SequencePoints.Add(sequencePoint);
                        }

                        lineNumber += numLines;
                    }
                    
                    writer.WriteLine(indentLevel--, "//We now return you to your regularly scheduled method");
                    writer.WriteLine(indentLevel--, "}");
                    writer.WriteLine(indentLevel--, "}");


                    writer.WriteLine(indentLevel, "}");
                }
            }

            private string GetMethodDeclaration()
            {
                var sb = new StringBuilder();
                sb.Append(_method.Attributes.ProtectionModifierCSharp());
                sb.Append(' ');
                if (!_method.IsConstructor)
                {
                    sb.Append(_method.ReturnType.FullNameCSharp());
                    sb.Append(' ');
                    sb.Append(_method.Name);
                }
                else
                {
                    sb.Append(_method.DeclaringType.NameCSharp());
                }
                sb.Append('(');
                sb.Append(GetParameters(_method.Parameters));
                sb.Append(')');
                return sb.ToString();
            }

            private static string GetParameters(Collection<ParameterDefinition> methodParameters)
            {
                if (methodParameters?.Any() != true) return "";

                var sb = new StringBuilder();
                bool isFirst = true;
                foreach (ParameterDefinition parameters in methodParameters)
                {
                    if (!isFirst)
                    {
                        sb.Append(", ");
                    }
                    sb.Append(parameters.ParameterType.FullNameCSharp());
                    sb.Append(' ');
                    sb.Append(parameters.Name);
                    isFirst = false;
                }
                return sb.ToString();
            }

            private static string GetFilePath(string outputDirectory, string name)
            {
                foreach (var @char in Path.GetInvalidFileNameChars())
                {
                    name = name.Replace(@char, '_');
                }
                string filePath;
                int? index = null;
                do
                {
                    filePath = Path.Combine(outputDirectory, name);
                    filePath += $"{index}.g.cs";
                    index = index.GetValueOrDefault() + 1;
                } while (File.Exists(filePath));

                return filePath;
            }
        }
    }
}