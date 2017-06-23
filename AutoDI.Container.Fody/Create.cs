namespace AutoDI.Container.Fody
{
    public enum Create
    {
        None,
        LazySingleton,
        Singleton,
        WeakTransient,
        Transient
    }
}