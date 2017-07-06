namespace AutoDI.Container
{
    public enum Lifetime
    {
        None,
        LazySingleton,
        Singleton,
        WeakTransient,
        Transient
    }
}