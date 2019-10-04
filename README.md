# AutoDI
Have a question? [![Join the chat at https://gitter.im/AutoDIContainer/Lobby](https://badges.gitter.im/AutoDIContainer/Lobby.svg)](https://gitter.im/AutoDIContainer/Lobby?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

[![NuGet Status](http://img.shields.io/nuget/v/AutoDI.svg?style=flat&label=AutoDI)](https://www.nuget.org/packages/AutoDI/)
[![NuGet Status](http://img.shields.io/nuget/v/AutoDI.Build.svg?style=flat&label=AutoDI.Build)](https://www.nuget.org/packages/AutoDI.Build/)
[![NuGet Status](http://img.shields.io/nuget/v/AutoDI.AspNetCore.svg?style=flat&label=AutoDI.AspNetCore)](https://www.nuget.org/packages/AutoDI.AspNetCore/)

[![Build Status](https://kitokeboo.visualstudio.com/AutoDI/_apis/build/status/Keboo.AutoDI?branchName=master)](https://kitokeboo.visualstudio.com/AutoDI/_build/latest?definitionId=4&branchName=master)


AutoDI is both a dependency injection container and a framework to simplify working with dependency injection (DI). It is built on top of the [Microsoft.Extensions.DependencyInjection.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions/) library, and it works very similar to [the way ASP.NET Core handles dependency injection](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection).

AutoDI delivers delivers two excellent features:
1. It is built on [Microsoft.Extensions.DependencyInjection.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions/) library to bring the same flavor of dependency injection enjoyed on ASP.NET Core to other platforms (.NET Framework, WPF, Xamarin, UWP, etc). 
2. It generates container registrations _at compile time_ using conventions (of course you can also add more at run-time as well).

See the [wiki](https://github.com/Keboo/AutoDI/wiki) for more details or check out the [Quick Start](https://github.com/Keboo/AutoDI/wiki/Quick-Start) page to get up and running fast.


## Why do I need this?

The goal of this library is to make dependency injection as simple as adding a NuGet package. 

In addition to standard constructor dependency injection, it also allows optional constructor parameters that will be resolved when the constructor is invoked.

In a typical DI setup, you can often find objects needing to take in dependencies that the object itself is not using, but needs them to pass into some other object. The bigger and deeper the object model gets, the worse this becomes. 
As a specific example, imagine you are inside of class `Foo` and wish to create an instance of `Bar`, but `Bar` has a dependency on `IService`. 
In many cases, one of several options can be used:
* Pass a facade to either the DI container or a factory that can create a `Bar`
* Add a dependency on `Foo` to take in an `IService` so that when it needs to create a `Bar` it can simply pass the instance to `Bar`'s constructor.

Though both these options work, they add additional effort. After all, in many cases, when we want a `Bar` we simply call `new Bar();`. AutoDI lets you write exactly that, and moves the dependency resolution inside of `Bar`'s constructor.
This dependency resolution is then forwarded to the DI container to resolve it.


## Does it work with <my favorite DI container / framework>?
Probably. 
Take a look at the [AutoDI.Examples](https://github.com/Keboo/AutoDI.Examples) repository for samples. There are examples of adding support for other DI containers as well as simple examples for most frameworks.

*Don't see your favorite listed? I accept PRs.*

### Icon
<img src="https://raw.github.com/Keboo/AutoDI/master/Icons/needle.png" width="64">

[Needle](https://materialdesignicons.com/icon/needle) designed by [Austin Andrews](https://thenounproject.com/prosymbols/) from [Material Design Icons](https://materialdesignicons.com/)

