namespace AutoDI.AssemblyGenerator;

public class WeaverAddedEventArgs : EventArgs
{
    public Weaver Weaver { get; }

    public WeaverAddedEventArgs(Weaver weaver)
    {
        Weaver = weaver;
    }
}