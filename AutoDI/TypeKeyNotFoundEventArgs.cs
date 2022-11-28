namespace AutoDI;

public class TypeKeyNotFoundEventArgs : EventArgs
{
    public Type ServiceType { get; }

    public object? Instance { get; set; }

    public TypeKeyNotFoundEventArgs(Type serviceType)
    {
        ServiceType = serviceType;
    }
}