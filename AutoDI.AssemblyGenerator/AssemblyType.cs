using Microsoft.CodeAnalysis;

namespace AutoDI.AssemblyGenerator
{
    public enum AssemblyType
    {
        ConsoleApplication = OutputKind.ConsoleApplication,
        WindowsApplication = OutputKind.WindowsApplication,
        DynamicallyLinkedLibrary = OutputKind.DynamicallyLinkedLibrary,
        NetModule = OutputKind.NetModule
    }
}