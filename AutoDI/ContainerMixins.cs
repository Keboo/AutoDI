namespace AutoDI;

public static class ContainerMixins
{
    public static IEnumerable<Map> Singletons(this IEnumerable<Map> registrations)
    {
        return registrations.Where(x => x.Lifetime == Lifetime.Singleton);
    }

    public static IEnumerable<Map> LazySingletons(this IEnumerable<Map> registrations)
    {
        return registrations.Where(x => x.Lifetime == Lifetime.LazySingleton);
    }

    public static IEnumerable<Map> WeakSingletons(this IEnumerable<Map> registrations)
    {
        return registrations.Where(x => x.Lifetime == Lifetime.WeakSingleton);
    }

    public static IEnumerable<Map> Scoped(this IEnumerable<Map> registrations)
    {
        return registrations.Where(x => x.Lifetime == Lifetime.Scoped);
    }

    public static IEnumerable<Map> Transient(this IEnumerable<Map> registrations)
    {
        return registrations.Where(x => x.Lifetime == Lifetime.Transient);
    }
}