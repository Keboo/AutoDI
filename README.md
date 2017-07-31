# AutoDI
Have a question? [![Join the chat at https://gitter.im/AutoDIContainer/Lobby](https://badges.gitter.im/AutoDIContainer/Lobby.svg)](https://gitter.im/AutoDIContainer/Lobby?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

[![NuGet Status](http://img.shields.io/nuget/v/AutoDI.svg?style=flat&label=AutoDI)](https://www.nuget.org/packages/AutoDI/)
[![NuGet Status](http://img.shields.io/nuget/v/AutoDI.Fody.svg?style=flat&label=AutoDI.Fody)](https://www.nuget.org/packages/AutoDI.Fody/)

[![Build status](https://ci.appveyor.com/api/projects/status/ybmv50xxi3lb086o?svg=true)](https://ci.appveyor.com/project/Keboo/autodi)


AutoDI is framework to simplify working with dependency injection (DI). It can integreate with your favorite DI container, or it can generate a container for you.

See the [wiki](https://github.com/Keboo/AutoDI/wiki) for more details or check out the [Quick Start](https://github.com/Keboo/AutoDI/wiki/Quick-Start) page to get up and running fast.


## Why do I need this?

In a typical DI setup you can often find your objects needing to take in dependencies that the object is not actually using, but needs the dependencies to pass into some other object. The bigger and deeper the object model gets, the worse this becomes. 
As a specific example, imagine you are inside of class `Foo` and wish to create an instance of `Bar`, but `Bar` has a dependency on `IService`. 
In many cases one of several options is used:
* Pass a facade to either the DI container or a factory that can create a `Bar`
* Add a dependency on `Foo` to take in an `IService` so that when it needs to create a `Bar` it can simply pass the instance in.

Though these options work, they add additional work. After all, in most cases when we want a `Bar` we simply call `new Bar();`. AutoDI lets you write exactly that, and moves the dependency resolution inside of Bar's constructor.
This dependency resolution is then forwarded to your favorite DI container to resolve it.

AutoDI.Container aims to take this one step further. Rather than needing to rely on a third party DI container, it will generate one at compile-time. In addition to being fast, it also saves you the haslte of needing to setup and maintain configuration code for your DI container.


## Does it work with <insert favorite DI container / framework here>?
Probably. 
Take a look in the [Examples](https://github.com/Keboo/AutoDI/tree/master/Examples). 
There are examples using AutoDI with many popular DI containers, as well as examples using AutoDI.Container on most popular C# platforms.

*Don't see your favorite listed? I accept PRs.*

### Icon
![needle](https://raw.github.com/Keboo/AutoDI/master/Icons/needle.png)

[Needle](https://materialdesignicons.com/icon/needle) designed by [Austin Andrews](https://thenounproject.com/prosymbols/) from [Material Design Icons](https://materialdesignicons.com/)
