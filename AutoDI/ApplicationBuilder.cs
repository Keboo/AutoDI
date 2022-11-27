using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

namespace AutoDI;

public class ApplicationBuilder : IApplicationBuilder
{
    private readonly List<Action<IServiceCollection>> _configureServicesDelegates = new();
    private readonly List<Delegate> _configureContainerDelegates = new();

    private Type _specifiedContainerType;

    public IApplicationBuilder ConfigureContainer<TContainerType>(Action<TContainerType> configureContianer)
    {
        if (configureContianer is null) throw new ArgumentNullException(nameof(configureContianer));
        if (_specifiedContainerType != null) throw new InvalidOperationException($"A container type of '{_specifiedContainerType.FullName}' was already specified");
        _specifiedContainerType = typeof(TContainerType);
        _configureContainerDelegates.Add(configureContianer);
        return this;
    }

    public IApplicationBuilder ConfigureServices(Action<IServiceCollection> configureServices)
    {
        if (configureServices is null) throw new ArgumentNullException(nameof(configureServices));
        _configureServicesDelegates.Add(configureServices);
        return this;
    }

    public IServiceProvider Build()
    {
        IServiceCollection collection = BuildCommonServices();
        IServiceProvider applicationProvider = BuildApplicationServiceProvider(collection);
        IServiceProvider rootProvider = GetProvider(applicationProvider, collection);
        if (rootProvider is IInitializeServiceProvider initResolver)
        {
            initResolver.Initialize();
        }
        return rootProvider;
    }

    private Type GetContainerType(IServiceCollection serviceCollection)
    {
        if (_specifiedContainerType != null)
        {
            return _specifiedContainerType;
        }
        var containerTypes = from ServiceDescriptor serviceDescriptor in serviceCollection
                             let typeInfo = serviceDescriptor.ServiceType.GetTypeInfo()
                             where typeInfo.IsGenericType
                             let genericType = typeInfo.GetGenericTypeDefinition()
                             where genericType == typeof(IServiceProviderFactory<>)
                             let containerType = serviceDescriptor.ServiceType.GenericTypeArguments[0]
                             orderby containerType == typeof(IContainer) ? 1 : 0 descending
                             select containerType;
        return containerTypes.LastOrDefault();
    }

    private IServiceProvider GetProvider(IServiceProvider applicationProvider,
        IServiceCollection serviceCollection)
    {
        Type containerType = GetContainerType(serviceCollection) ??
                             throw new NoRegisteredContainerException($"Could not determine container type. Is there an {typeof(IServiceProviderFactory<>).FullName} registered?");
        return (IServiceProvider)GetType().GetTypeInfo().DeclaredMethods
            .Single(m => m.IsGenericMethodDefinition && m.Name == nameof(GetProvider))
            .MakeGenericMethod(containerType)
            .Invoke(this, new object[] { applicationProvider, serviceCollection });
    }

    private IServiceProvider GetProvider<TContainerType>(IServiceProvider applicationProvider, IServiceCollection serviceCollection)
    {
        IServiceProviderFactory<TContainerType> providerFactory =
            applicationProvider.GetService<IServiceProviderFactory<TContainerType>>()
            ?? throw new NoServiceProviderFactoryException("Failed to resolve service provider factory");

        TContainerType container = providerFactory.CreateBuilder(serviceCollection);

        foreach (Action<TContainerType> configureMethods in _configureContainerDelegates.OfType<Action<TContainerType>>())
        {
            configureMethods(container);
        }

        return providerFactory.CreateServiceProvider(container);
    }

    private IServiceCollection BuildCommonServices()
    {
        var collection = new AutoDIServiceCollection();

        collection.AddAutoDITransient<IServiceProviderFactory<IContainer>>(sp => new AutoDIServiceProviderFactory());

        foreach (var @delegate in _configureServicesDelegates)
        {
            @delegate(collection);
        }

        return collection;
    }

    private static IServiceProvider BuildApplicationServiceProvider(IServiceCollection collection)
    {
        var factory = new AutoDIServiceProviderFactory();
        return factory.CreateServiceProvider(factory.CreateBuilder(collection));
    }
}