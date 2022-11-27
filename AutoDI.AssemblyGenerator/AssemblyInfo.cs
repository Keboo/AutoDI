using System.Reflection;

using Microsoft.CodeAnalysis;

namespace AutoDI.AssemblyGenerator;

public sealed class AssemblyInfo
{
    private readonly StringBuilder _contents = new();
    private readonly List<Weaver> _weavers = new();
    private readonly List<MetadataReference> _references = new()
    {
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
    };

    public string Name { get; }

    public Assembly Assembly { get; internal set; }

    public string FilePath { get; internal set; }

    internal IReadOnlyList<MetadataReference> References => _references;

    public IReadOnlyList<Weaver> Weavers => _weavers;

    internal OutputKind OutputKind { get; set; } = OutputKind.DynamicallyLinkedLibrary;

    public AssemblyInfo(string name)
    {
        Name = name;
    }

    internal void AppendLine(string line) => _contents.AppendLine(line);

    internal void AddReference(MetadataReference reference) => _references.Add(reference);

    internal void AddWeaver(Weaver weaver) => _weavers.Add(weaver);

    internal string GetContents() => _contents.ToString();
}