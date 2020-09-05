# dotnet-windows-registry

[![Build status](https://ci.appveyor.com/api/projects/status/tq6brn2mgvaj95jh?svg=true)](https://ci.appveyor.com/project/dwmkerr/dotnet-windows-registry) [![codecov](https://codecov.io/gh/dwmkerr/dotnet-windows-registry/branch/master/graph/badge.svg)](https://codecov.io/gh/dwmkerr/dotnet-windows-registry) [![NuGet Package](https://img.shields.io/nuget/v/DotNetWindowsRegsitry)](https://www.nuget.org/packages/DotNetWindowsRegsitry)

The `DotNetWindowsRegistry` package provides a simple, unit and integration test friendly wrapper around the Windows Registry, which is 100% compliant with the existing `Microsoft.Win32.Registry` package.

This module is particularly useful if you want to be able to test code which works on the registry.

<!-- vim-markdown-toc GFM -->

* [Quick Start](#quick-start)
    * [Using the IRegistry Classes](#using-the-iregistry-classes)
    * [Quick Start - Castle Windsor](#quick-start---castle-windsor)
* [Developer Guide](#developer-guide)
* [Creating a Release](#creating-a-release)
* [Essential Reading](#essential-reading)
* [Compatibility, Format and Potential Changes](#compatibility-format-and-potential-changes)

<!-- vim-markdown-toc -->

## Quick Start

Install the package with:

```sh
dotnet add package DotNetWindowsRegistry
```

Modifying the registry directly would be problematic when running tests, as all changes would need to be cleaned up afterwards. Doing this is brittle and generally non-portable (i.e. the tests would only be able to be run on a Windows machine).

To support testing, this project supports a 'Testable Registry'. Instead of accessing the registry directly, _all_ APIs and classes use the `IRegistry` interface.

In most cases, you will use the `WindowsRegistry` class, which is nothing more than a wrapper around the standard registry. In testing scenarios, this implementation can be swapped out for the `InMemoryRegistry` class.

The `InMemoryRegistry` class is a lightweight implementation of the registry, which essentially is just an in memory structure. This allows for quick assertions on the shape of the changes to the registry, allowing you to test the side-effects of your functions.

The code snippet below shows how you can use the `InMemoryRegistry` to test a scenario which is exercised by the `MyClass` class:

```cs
public class MyClassTests
{
    private MyClass _myClass;
    private InMemoryRegistry _registry;

    [SetUp]
    public void SetUp()
    {
        //  Create the class under test - but use an InMemoryRegistry.
        _registry = new InMemoryRegistry();
        _myClass = new MyClass(_registry);
    }

    [Test]
    public void MyClass_Register_Correctly_Creates_A_Class_Entry()
    {
        //  Call a made-up function which creates a COM server entry.
        _myClass.Register("00000000-1111-2222-3333-444444444444",
             "SomeComServerName.MyServer", @"c:\Some Folder\SomeServer.comhost.dll");

        //  Assert we have the expected structure
        var print = _registry.Print(RegistryView.Registry64);
        Assert.That(print, Is.EqualTo(string.Join(Environment.NewLine,
            @"HKEY_CLASSES_ROOT",
            @"  CLSID",
            @"     {00000000-1111-2222-3333-444444444444} = CoreCLR COMHost Server",
            @"       InProcServer32 = c:\Some Folder\SomeServer.comhost.dll",
            @"       ThreadinModel = Both",
            @"     ProgId = SomeComServerName.MyServer")
        ));
    }
}
```

You can pre-populate the registry with structure if needed. In this example, the test validates the behaviour of server registration when a class is already registered with the same class identifier:

```cs
[Test]
public void MyClass_Register_Throws_If_A_Class_Is_Already_Registered_With_The_Same_Clsid()
{
    //  Pre-popoluate the registry with a server which clashes with the one we will register.
    _registry.AddStructure(RegistryView.Registry64, string.Join(Environment.NewLine,
        @"HKEY_CLASSES_ROOT",
        @"  CLSID",
        @"     {00000000-1111-2222-3333-444444444444} = Some Existing Server")
    );

    //  Assert that we throw in this case.
    Assert.Throws<ClassAlreadyRegisteredException>(() =>
        _myClass.Register("00000000-1111-2222-3333-444444444444",
             "SomeComServerName.MyServer", @"c:\Some Folder\SomeServer.comhost.dll"));
}
```

### Using the IRegistry Classes

The only way to be able to test changes to the registry with this library is to make sure that your code uses the `IRegistry` and `IRegistryKey` interfaces - *not* the concrete `Windows.WindowsRegistry` class or `Microsoft.Win32.Registry` class. This is because during testing the `InMemoryRegistry` implementation must be used.

In practice this means you will most likely need to adopt a [Dependency Injection](https://en.wikipedia.org/wiki/Dependency_injection) pattern for your code.

Note that this does not mean you *have* to implement any kind of [Inversion of Control](https://en.wikipedia.org/wiki/Inversion_of_control) container. You can if you want to, and if you already are using one in your project it makes sense. If you want to avoid this complexity, simply pass the registry implementation in the constructor of your class.

For example:

```cs
public class MyClass
{
    public MyClass(IRegistry _registry)
    {
        _registry = registry;
    }

    public void Function()
    {
        //  e.g.
        _regsitry.CreateKey();
    }

    private IRegsitry _registry;
}
```

Now create the class like this:

```cs
var myClass = new MyClass(new WindowsRegistry());
```

Or in test scenarios, like this:

```cs
var inMemoryRegistry = new InMemoryRegistry();
var myClass = new MyClass(inMemoryRegistry);
```

A full discussion of the pros and cons of Dependency Inject, Inversion of Control and specific implementations is beyond the scope of this guide (and there are many online resources for this). However, to help out, I will add examples for specific implementations if this is useful, or add links to good resources explaining the concepts if they are suggested.

### Quick Start - Castle Windsor

Create your container, register the implementation for the `IRegistry` service, resolve. Easy!

```cs
var container = new WindsorContainer();
container.Register(Component.For<IRegistry>()
      .ImplementedBy<WindowsRegistry>()
);
var registry = container.Resolve<IRegistry>();
```

Note that the `WindowsRegistry` and `InMemoryRegistry` services can be singletons. It is the _callers_ responsibility to think about thread-safety if needed. The registry is essentially a database which you must put your own logic around to deal with race conditions etc.

## Developer Guide

This section covers all of the material you should need to be able to build the code locally, customise it to your needs, or contribute to the project.

All source code is in the `src` directory. You can open the `./src/DotNetWindowsRegistry.sln` solution in Visual Studio or Code.

To build, test and package the project, just run:

| Command        | Usage                                                                                    |
|----------------|------------------------------------------------------------------------------------------|
| `init.ps1`     | Ensure your machine can run builds by installing necessary components such as `codecov`. |
| `dotnet build` | Build the library and CLI.                                                               |
| `dotnet test`  | Run the tests.                                                                           |
| `coverage.ps1` | Run the tests, generating a coverage report in `./artifacts`.                            |
| `dotnet pack`  | Build the NuGet packages.                                                                |

## Creating a Release

To create a release, update the version number in the project files and create version tag. Then push - the build pipeline with publish the release

## Essential Reading

The `*.reg` file format is used as a 'spec' of sorts for the string representations of the in-memory registry. The closest I can find to a specification is at:

- [How to add, modify, or delete registry subkeys and values by using a .reg file](https://support.microsoft.com/en-us/help/310516/how-to-add-modify-or-delete-registry-subkeys-and-values-by-using-a-reg)

## Compatibility, Format and Potential Changes

This library treats all objects written to the registry for testing purposes as strings. I *might* instead adopt the `*.reg` file structure documented above, to allow for more precision when specifying `DWORD` values, expandable strings and so on.

Until the project reaches a `v1.x` release please make no assumptions that the structure of the testing registry in-memory representation will stay the same.
