# AutoDI
Have a question? [![Join the chat at https://gitter.im/AutoDIContainer/Lobby](https://badges.gitter.im/AutoDIContainer/Lobby.svg)](https://gitter.im/AutoDIContainer/Lobby?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

AutoDI is framework to simplify working with dependency injection (DI). It can integreate with your favorite DI container, or it can generate a container for you.

See the [wiki](https://github.com/Keboo/AutoDI/wiki) for more details or check out the [Quick Start](https://github.com/Keboo/AutoDI/wiki/Quick-Start) page to get up and running fast.

## Nugets
The entire framework is made up of several nuget packages.

[![NuGet Status](http://img.shields.io/nuget/v/AutoDI.svg?style=flat&label=AutoDI)](https://www.nuget.org/packages/AutoDI/)
[![NuGet Status](http://img.shields.io/nuget/v/AutoDI.Fody.svg?style=flat&label=AutoDI.Fody)](https://www.nuget.org/packages/AutoDI.Fody/)
[![NuGet Status](http://img.shields.io/nuget/v/AutoDI.Container.svg?style=flat&label=AutoDI.Container)](https://www.nuget.org/packages/AutoDI.Container/)
[![NuGet Status](http://img.shields.io/nuget/v/AutoDI.Container.Fody.svg?style=flat&label=AutoDI.Container.Fody)](https://www.nuget.org/packages/AutoDI.Container.Fody/)



A framework for simplifying the creation of objects that only partially depend on resources in your favorite DI container.

[![NuGet Status](http://img.shields.io/nuget/v/AutoDI.Fody.svg?style=flat)](https://www.nuget.org/packages/AutoDI.Fody/)
[![Build status](https://ci.appveyor.com/api/projects/status/ybmv50xxi3lb086o?svg=true)](https://ci.appveyor.com/project/Keboo/autodi)

Just the AutoDI assembly without the weaver. [![NuGet Status](http://img.shields.io/nuget/v/AutoDI.svg?style=flat)](https://www.nuget.org/packages/AutoDI/)

This is not another [DI](https://en.wikipedia.org/wiki/Dependency_injection) container, rather its intent is to make interacting with your favorite DI container easier. It allows you to decorate constructor arguments so they are automatically resolved by your DI container. This also enables constructors that take in some initialization data, in addition to the service dependencies that are provided by the DI contianer.

### Configuring AutoDI.Container

//Manually injecting the container

## Examples

There are examples using many of the popular DI containers inside of the [examples directory](https://github.com/Keboo/AutoDI/tree/master/Examples).

You can also see an example in a small WPF project [here](https://github.com/Keboo/YoutubeDownloader).

To use AutoDI in your classes simply declare an optional constructor parameter and decorate it with the DependencyAttribute. This parameter will be resolved when the constructor is invoked.

```C#
public class MyClass()
{
  public MyClass([Dependency] IService service = null)
  {
    if (service == null) throw new ArgumentNullException(nameof(service));
  }
}
```

## Customization and features
AutoDI has several extension points that allow for a great deal of customization and control.
- DependencyResolver.Set can also take in an IGetResolver behavior. This interface allows you to return a different IDependencyResolver based on the requested type and services.
- DependencyAttribute's constructor takes in a params array of values. These values are passed the the IDependencyResolver.Resolve method allowing you to pass configuration keys or additional values to help resolve the instance.



### Icon
![needle](https://raw.github.com/Keboo/AutoDI/master/Icons/needle.png)

[Needle](https://materialdesignicons.com/icon/needle) designed by [Austin Andrews](https://thenounproject.com/prosymbols/) from [Material Design Icons](https://materialdesignicons.com/)