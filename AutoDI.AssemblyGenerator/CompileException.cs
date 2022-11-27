using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

namespace AutoDI.AssemblyGenerator;

public class CompileException : Exception
{
    internal CompileException(ImmutableArray<Diagnostic> diagnostics)
    {
        Errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.GetMessage()).ToArray();
        Warnings = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning)
            .Select(d => d.GetMessage()).ToArray();
    }

    public string[] Errors { get; }

    public string[] Warnings { get; }
}