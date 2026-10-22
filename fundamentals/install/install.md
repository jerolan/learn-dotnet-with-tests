# Install .NET and set up your environment

The official installation instructions for .NET are available [here](https://dotnet.microsoft.com/download).

## The SDK and the runtime

The .NET SDK contains the compilers, the CLI and the runtime you need to build and run code. The runtime alone can run an application that somebody else built, but it cannot compile one. Install the SDK.

The projects in this book target `net6.0` and CI builds them with the `6.0.x` SDK. If you install only a newer SDK, you can still build a `net6.0` project, but running its tests needs the 6.0 runtime. Keeping the .NET 6 SDK installed alongside newer ones avoids that problem.

Check what is on your machine:

```sh
dotnet --info
```

The output lists the installed SDKs and runtimes. You want at least one SDK and a `Microsoft.NETCore.App 6.0.x` runtime:

```sh
dotnet --list-sdks
dotnet --list-runtimes
```

If `dotnet` is not recognized as a command, the installer did not put it on your `PATH`. Close and reopen the terminal first; on macOS and Linux the installer adds the path to your shell profile, and the change only applies to new sessions.

## Projects and solutions

A .NET project is a `.csproj` file: it names the target framework, the package references and, for a console program, the output type. A solution is a file that groups projects so one command can build or test all of them.

You will use four commands all the time:

```sh
dotnet new console -n HelloWorld -o src/HelloWorld
dotnet new classlib -n Calculator -o src/Calculator
dotnet new mstest -n Calculator.Tests -o tests/Calculator.Tests
dotnet new sln -n Calculator
```

Adding things to the solution and wiring the reference is the part people forget:

```sh
dotnet sln add src/Calculator/Calculator.csproj tests/Calculator.Tests/Calculator.Tests.csproj
dotnet add tests/Calculator.Tests/Calculator.Tests.csproj reference src/Calculator/Calculator.csproj
```

The reference is what lets the test project see the code it tests. Without it, the compiler reports that your types do not exist even when the files are sitting in the same folder.

The day-to-day commands are short:

```sh
dotnet build
dotnet test
dotnet run --project src/HelloWorld
```

`dotnet test` restores packages, builds the solution and runs every test project it finds. In this book, every chapter folder is a solution on its own, so you can open `fundamentals/types` and run `dotnet test` without building anything else.

## Verify the installation

The `fundamentals/install` folder contains a small solution with one test. Run it:

```sh
cd fundamentals/install
dotnet test
```

You should see one passing test:

```text
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 19 ms - InstallCheck.Tests.dll (net6.0)
```

The duration depends on your machine.

The test calls `SdkInfo.GetRuntimeVersion()` and checks that it returns something:

```csharp
[TestClass]
public sealed class SdkInfoTests
{
    [TestMethod]
    public void GetRuntimeVersion_ReturnsNonEmpty()
    {
        var version = InstallCheck.SdkInfo.GetRuntimeVersion();

        Assert.IsFalse(string.IsNullOrWhiteSpace(version));
    }
}
```

The assertion is deliberately weak. It does not pin a version, because that would break on every patch release. What it proves is that the SDK compiled C#, resolved the project reference, the test runner discovered the MSTest class, and the runtime executed the method. Those are the four things that have to work before any later chapter can.

If the command fails, read the error before changing anything:

- `dotnet: command not found` means the SDK is missing or not on `PATH`.
- `You must install .NET to run this application` means the build worked but the 6.0 runtime is missing.
- A `CS0246` about `InstallCheck` means the project reference is missing.

## Your editor

You can follow the whole book in VS Code with the C# extension, or in Visual Studio/Rider. The editor matters less than what you can do with it. You should be able to do these without leaving the keyboard:

- Rename a symbol across the solution.
- Extract a variable or a method from a selection.
- Go to a definition and find all usages.
- Format the file. `dotnet format` does this from the terminal.
- Run a single test, or the whole suite, and see the result.

The refactoring moves are how the "refactor" step stays cheap. If renaming a class takes ten manual edits, you will skip the refactor and the design will drift.

## Wrapping up

At this point you should have:

- A .NET 6 SDK that `dotnet --info` can see.
- A terminal where `dotnet build` and `dotnet test` work.
- An editor that can run the tests and perform the refactorings this book depends on.
- The `fundamentals/install` test passing.

Next: [Hello, World](../hello-world/hello-world.md), where you write your first program and your first test.

