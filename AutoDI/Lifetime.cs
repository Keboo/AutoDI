namespace AutoDI
{
    public enum Lifetime
    {
        None,
        LazySingleton,
        Singleton,
        Scoped,
        WeakTransient,
        Transient
    }
}