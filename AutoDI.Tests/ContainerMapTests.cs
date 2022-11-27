using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutoDI.Tests;

[TestClass]
public class ContainerMapTests
{
    [TestMethod]
    public void TestTypeKeyNotFoundEventIsRaised()
    {
        var map = new ContainerMap();
        var services = new AutoDIServiceCollection();
        bool eventRaised = false;

        map.TypeKeyNotFound += (_, __) => eventRaised = true;

        services.AddAutoDISingleton<IInterface, Class>();
        map.Add(services);

        map.Get<string>(null);
        Assert.IsTrue(eventRaised);
    }

    [TestMethod]
    public void TestTypeKeyNotFoundEventIsNotRaised()
    {
        var map = new ContainerMap();
        var services = new AutoDIServiceCollection();
        bool eventRaised = false;

        map.TypeKeyNotFound += (_, __) => eventRaised = true;

        services.AddAutoDISingleton<IInterface, Class>();
        map.Add(services);

        map.Get<IInterface>(null);
        Assert.IsFalse(eventRaised);
    }

    [TestMethod]
    public void TestTypeKeyNotFoundEventCanInsertType()
    {
        var map = new ContainerMap();
        var services = new AutoDIServiceCollection();
        var @class = new Class();

        map.TypeKeyNotFound += (_, e) => e.Instance = @class;

        map.Add(services);

        IInterface retrievedService = map.Get<IInterface>(null);
        Assert.AreEqual(@class, retrievedService);
    }

    [TestMethod]
    public void TestGetSingleton()
    {
        var map = new ContainerMap();
        var services = new AutoDIServiceCollection();
        services.AddAutoDISingleton<IInterface, Class>();
        map.Add(services);

        IInterface c = map.Get<IInterface>(null);
        Assert.IsTrue(c is Class);
    }

    [TestMethod]
    public void TestGetLazySingleton()
    {
        var map = new ContainerMap();
        var services = new AutoDIServiceCollection();
        services.AddAutoDILazySingleton<IInterface, Class>();
        map.Add(services);

        IInterface c = map.Get<IInterface>(null);
        Assert.IsTrue(c is Class);
    }

    [TestMethod]
    public void TestGetWeakSingleton()
    {
        var map = new ContainerMap();
        var services = new AutoDIServiceCollection();
        services.AddAutoDIWeakSingleton<IInterface, Class>();
        map.Add(services);

        IInterface c = map.Get<IInterface>(null);
        Assert.IsTrue(c is Class);
    }

    [TestMethod]
    public void TestGetTransient()
    {
        var map = new ContainerMap();
        var services = new AutoDIServiceCollection();
        services.AddAutoDITransient<IInterface, Class>();
        map.Add(services);

        IInterface c = map.Get<IInterface>(null);
        Assert.IsTrue(c is Class);
    }

    [TestMethod]
    public void GetSingletonAlwaysReturnsTheSameInstance()
    {
        var map = new ContainerMap();
        var instance = new Class();
        var services = new AutoDIServiceCollection();
        services.AddAutoDISingleton<IInterface>(instance);
        map.Add(services);

        IInterface c1 = map.Get<IInterface>(null);
        IInterface c2 = map.Get<IInterface>(null);
        Assert.IsTrue(ReferenceEquals(c1, c2));
        Assert.IsTrue(ReferenceEquals(c1, instance));
        Assert.IsTrue(ReferenceEquals(c2, instance));
    }

    [TestMethod]
    public void GetLazySingletonDoesNotCreateObjectUntilRequested()
    {
        var map = new ContainerMap();
        var services = new AutoDIServiceCollection();
        services.AddAutoDILazySingleton<IInterface, Class>(provider => throw new Exception());
        map.Add(services);

        try
        {
            map.Get<IInterface>(null);
        }
        catch (Exception)
        {
            return;
        }
        Assert.Fail("Exception should have been thrown");
    }

    [TestMethod]
    public void GetLazySingletonReturnsTheSameInstance()
    {
        var map = new ContainerMap();
        var services = new AutoDIServiceCollection();
        services.AddAutoDILazySingleton<IInterface, Class>(provider => new Class());
        services.AddAutoDILazySingleton<IInterface2, Class>(provider => new Class());
        map.Add(services);

        var instance1 = map.Get<IInterface>(null);
        var instance2 = map.Get<IInterface2>(null);
        Assert.IsNotNull(instance1);
        Assert.IsNotNull(instance2);
        Assert.IsTrue(ReferenceEquals(instance1, instance2));
    }

    [TestMethod]
    public void GetWeakSingletonOnlyCreatesOneInstanceAtATime()
    {
        var map = new ContainerMap();
        var services = new AutoDIServiceCollection();
        int instanceCount = 0;
        services.AddAutoDIWeakSingleton<IInterface, Class>(provider =>
        {
            instanceCount++;
            return new Class();
        });
        map.Add(services);

        AssertSingleInstance(map);

        Assert.AreEqual(1, instanceCount);

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);

        Assert.IsNotNull(map.Get<IInterface>(null));
        Assert.AreEqual(2, instanceCount);

        static void AssertSingleInstance(ContainerMap map)
        {
            var instance = map.Get<IInterface>(null);

            Assert.IsTrue(ReferenceEquals(instance, map.Get<IInterface>(null)));
            Assert.IsTrue(ReferenceEquals(instance, map.Get<IInterface>(null)));
        }
    }

    [TestMethod]
    public void GetTransientAlwaysCreatesNewInstances()
    {
        var map = new ContainerMap();
        var services = new AutoDIServiceCollection();
        int instanceCount = 0;
        services.AddAutoDITransient<IInterface, Class>(provider =>
        {
            instanceCount++;
            return new Class();
        });
        map.Add(services);

        var a = map.Get<IInterface>(null);
        var b = map.Get<IInterface>(null);
        var c = map.Get<IInterface>(null);
        Assert.IsFalse(ReferenceEquals(a, b));
        Assert.IsFalse(ReferenceEquals(b, c));
        Assert.IsFalse(ReferenceEquals(a, c));
        Assert.AreEqual(3, instanceCount);
    }

    [TestMethod]
    [Description("Issue 22")]
    public void ContainerMapCanGenerateLazyInstances()
    {
        var map = new ContainerMap();
        var @class = new Class();
        var services = new AutoDIServiceCollection();
        services.AddAutoDISingleton<IInterface>(@class);
        map.Add(services);

        Lazy<IInterface> lazy = map.Get<Lazy<IInterface>>(null);
        Assert.IsNotNull(lazy);
        Assert.IsTrue(ReferenceEquals(@class, lazy.Value));
    }

    [TestMethod]
    [Description("Issue 22")]
    public void ContainerMapCanGenerateFuncInstances()
    {
        var map = new ContainerMap();
        var @class = new Class();
        var services = new AutoDIServiceCollection();
        services.AddAutoDISingleton<IInterface>(@class);
        map.Add(services);

        Func<IInterface> func = map.Get<Func<IInterface>>(null);
        Assert.IsNotNull(func);
        Assert.IsTrue(ReferenceEquals(@class, func()));
    }

    [TestMethod]
    [Description("Issue 106")]
    public void ContainerMapCanConstructTypeWithDefaultConstructor()
    {
        var map = new ContainerMap();

        Class @class = map.Get<Class>(null);
        Assert.IsInstanceOfType(@class, typeof(Class));
    }

    [TestMethod]
    [Description("Issue 106")]
    public void ContainerMapCanConstructTypeWhoseDependenciesAreMapped()
    {
        var map = new ContainerMap();
        var services = new AutoDIServiceCollection();
        services.AddAutoDITransient<IInterface, Class>();
        map.Add(services);

        ClassWithParameters @class = map.Get<ClassWithParameters>(null);
        Assert.IsInstanceOfType(@class, typeof(ClassWithParameters));
        Assert.IsInstanceOfType(@class.Service, typeof(Class));
    }

    [TestMethod]
    [Description("Issue 106")]
    public void AutoConstructedTypesAlwaysReturnNewInstances()
    {
        var map = new ContainerMap();
        var services = new AutoDIServiceCollection();
        services.AddAutoDITransient<IInterface, Class>();
        map.Add(services);

        ClassWithParameters class1 = map.Get<ClassWithParameters>(null);
        Assert.IsInstanceOfType(class1, typeof(ClassWithParameters));

        ClassWithParameters class2 = map.Get<ClassWithParameters>(null);
        Assert.IsInstanceOfType(class2, typeof(ClassWithParameters));

        Assert.IsFalse(ReferenceEquals(class1, class2));
    }

    [TestMethod]
    [Description("Issue 124")]
    public void CanResolveClosedGenericFromOpenGenericRegistration()
    {
        var map = new ContainerMap();
        var services = new AutoDIServiceCollection
        {
            ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>))
        };

        /* Unmerged change from project 'AutoDI.Tests(net7.0)'
        Before:
                    services.Add(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));
                    map.Add(services);
        After:
                    map.Add(services);
        */
        map.Add(services);

        var logger1 = map.Get<ILogger<MyClass>>(null);
        var logger2 = map.Get<ILogger<MyOtherClass>>(null);

        Assert.IsNotNull(logger1);
        Assert.IsNotNull(logger2);
        Assert.IsTrue(ReferenceEquals(logger1, map.Get<ILogger<MyClass>>(null)));
        Assert.IsTrue(ReferenceEquals(logger2, map.Get<ILogger<MyOtherClass>>(null)));
    }

    [TestMethod]
    [Description("Issue 124")]
    public void CanResolveClosedGenericFromOpenGenericRegistrationWithParameter()
    {
        var map = new ContainerMap();
        var services = new AutoDIServiceCollection
        {
            ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(LoggerEx<>)),
            ServiceDescriptor.Singleton(typeof(ILoggerFactory), typeof(LoggerFactory))
        };

        /* Unmerged change from project 'AutoDI.Tests(net7.0)'
        Before:
                    services.Add(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(LoggerEx<>)));
                    services.Add(ServiceDescriptor.Singleton(typeof(ILoggerFactory), typeof(LoggerFactory)));
                    map.Add(services);
        After:
                    map.Add(services);
        */
        map.Add(services);

        var logger = map.Get<ILogger<MyClass>>(null) as LoggerEx<MyClass>;

        Assert.IsNotNull(logger);
        Assert.IsTrue(logger.Factory is LoggerFactory);
    }

    [TestMethod]
    [Description("Issue 127")]
    public void CanResolveSingleIEnumerableConstructorParameter()
    {
        var map = new ContainerMap();
        var services = new AutoDIServiceCollection();
        services.AddAutoDISingleton<IInterface, Derived1>();
        map.Add(services);

        var @class = map.Get<ClassWithParameter<IEnumerable<IInterface>>>(null);

        Assert.IsNotNull(@class);
        CollectionAssert.AreEquivalent(
            new[] { typeof(Derived1) },
            @class.Parameter.Select(x => x.GetType()).ToArray());
    }

    [TestMethod]
    [Description("Issue 127")]
    public void CanResolveMultipleIEnumerableConstructorParameter()
    {
        var map = new ContainerMap();
        var services = new AutoDIServiceCollection();
        services.AddAutoDISingleton<IInterface, Derived1>();
        services.AddAutoDISingleton<IInterface, Derived2>();
        map.Add(services);

        var @class = map.Get<ClassWithParameter<IEnumerable<IInterface>>>(null);

        Assert.IsNotNull(@class);
        CollectionAssert.AreEquivalent(
            new[] { typeof(Derived1), typeof(Derived2) },
            @class.Parameter.Select(x => x.GetType()).ToArray());
    }

    [TestMethod]
    [Description("Issue 127")]
    public void CanResolveSingleArrayConstructorParameter()
    {
        var map = new ContainerMap();
        var services = new AutoDIServiceCollection();
        services.AddAutoDISingleton<IInterface, Derived1>();
        services.AddAutoDISingleton<IInterface, Derived2>();
        map.Add(services);

        var @class = map.Get<ClassWithParameter<IInterface[]>>(null);

        Assert.IsNotNull(@class);
        CollectionAssert.AreEquivalent(
            new[] { typeof(Derived1), typeof(Derived2) },
            @class.Parameter.Select(x => x.GetType()).ToArray());
    }

    [TestMethod]
    [Description("Issue 127")]
    public void CanResolveMultipleArrayConstructorParameter()
    {
        var map = new ContainerMap();
        var services = new AutoDIServiceCollection();
        services.AddAutoDISingleton<IInterface, Derived1>();
        services.AddAutoDISingleton<IInterface, Derived2>();
        map.Add(services);

        var @class = map.Get<ClassWithParameter<IInterface[]>>(null);

        Assert.IsNotNull(@class);
        CollectionAssert.AreEquivalent(
            new[] { typeof(Derived1), typeof(Derived2) },
            @class.Parameter.Select(x => x.GetType()).ToArray());
    }

    [TestMethod]
    public void CanRemovedRegisteredMap()
    {
        var map = new ContainerMap();
        var services = new AutoDIServiceCollection();
        services.AddAutoDISingleton<IInterface, Derived1>();
        services.AddAutoDISingleton<IInterface, Derived2>();
        map.Add(services);

        bool wasRemoved = map.Remove<IInterface>();
        var interfaces = map.Get<IEnumerable<IInterface>>(null);

        Assert.IsTrue(wasRemoved);
        Assert.IsNull(interfaces);
    }

    [TestMethod]
    public void WhenMultipleRegistrationsExistItResolvesTheLastOne()
    {
        var map = new ContainerMap();
        var services = new AutoDIServiceCollection();
        services.AddAutoDISingleton<IInterface, Derived1>();
        services.AddAutoDISingleton<IInterface, Derived2>();
        map.Add(services);

        var @class = map.Get<IInterface>(null);

        Assert.IsTrue(@class is Derived2);
    }

    [TestMethod]
    [Description("Issue 156")]
    public void WhenConstructorHasOptionalStringParameterItResolves()
    {
        var map = new ContainerMap();
        var services = new AutoDIServiceCollection();
        services.AddAutoDISingleton<ClassWithOptionalStringParameter>();
        map.Add(services);

        var @class = map.Get<ClassWithOptionalStringParameter>(null);

        Assert.IsNotNull(@class);
        Assert.IsNull(@class.Foo);
    }

    private interface IInterface { }

    private interface IInterface2 { }

    private interface ILogger<T> { }

    private class Logger<T> : ILogger<T> { }

    private class LoggerEx<T> : ILogger<T>
    {
        public ILoggerFactory Factory { get; }

        public LoggerEx(ILoggerFactory factory)
        {
            Factory = factory;
        }
    }

    private interface ILoggerFactory { }

    private class LoggerFactory : ILoggerFactory { }

    private class MyClass { }
    private class MyOtherClass { }

    private class Class : IInterface, IInterface2 { }

    private class Derived : Class { }

    private class Derived1 : IInterface { }
    private class Derived2 : IInterface { }
    private class Derived3 : Derived2 { }

    private class ClassWithParameter<T>
    {
        public ClassWithParameter(T parameter)
        {
            Parameter = parameter;
        }

        public T Parameter { get; }
    }

    private class ClassWithParameters
    {
        public IInterface Service { get; }
        public ClassWithParameters(IInterface service)
        {
            Service = service ?? throw new ArgumentNullException(nameof(service));
        }
    }

    private class ClassWithOptionalStringParameter
    {
        public string Foo { get; }
        public ClassWithOptionalStringParameter(string foo = null)
        {
            Foo = foo;
        }
    }
}