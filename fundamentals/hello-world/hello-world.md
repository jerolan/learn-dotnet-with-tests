# Hello, World

[You can find all the code for this chapter here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/hello-world)

It is traditional for your first program in a new language to be [Hello, World](https://en.wikipedia.org/wiki/%22Hello,_World!%22_program).

Create a folder for your work, then ask the .NET CLI for a console project:

```sh
mkdir hello-world
cd hello-world
dotnet new console -n HelloWorld -o src/HelloWorld
```

Open `src/HelloWorld/Program.cs` and replace what the template wrote with:

```csharp
Console.WriteLine("Hello, world");
```

Run it:

```sh
dotnet run --project src/HelloWorld
```

You should see:

```text
Hello, world
```

## How it works

`Program.cs` is a top-level program. You write statements at the top of the file and the compiler wraps them in a generated `Main` method. Execution starts at the first statement and runs down the file.

`Console.WriteLine` writes one line to standard output. `Console` lives in the `System` namespace, and the console template enables implicit usings, so you do not need `using System;` at the top.

Change the text inside the quotes, run the command again, and watch the output change. So far the program is a list of statements.

## How to test

How do you test this? `Console.WriteLine` is a side effect: it reaches outside the program to print on your terminal. The greeting is our domain, and we can test it without a console.

Move the greeting into a class. Create `src/HelloWorld/Greeter.cs`:

```csharp
namespace HelloWorld;

public class Greeter
{
    public string SayHello()
    {
        return "Hello, world";
    }
}
```

Now `Program.cs` becomes:

```csharp
using HelloWorld;

var greeter = new Greeter();
Console.WriteLine(greeter.SayHello());
```

Run it again. Same output. Nothing changed for the person using the program, but the interesting part is now a plain method call.

## Projects, references and a solution

.NET will not discover a test file sitting next to your code. Tests live in their own project, and that project has to reference the project it tests. This is the part where the tooling asks for a little ceremony before you can see a red test.

From the chapter folder, create the test project, a solution to group both projects, and the reference:

```sh
dotnet new mstest -n HelloWorld.Tests -o tests/HelloWorld.Tests
dotnet new sln -n HelloWorld
dotnet sln add src/HelloWorld/HelloWorld.csproj tests/HelloWorld.Tests/HelloWorld.Tests.csproj
dotnet add tests/HelloWorld.Tests/HelloWorld.Tests.csproj reference src/HelloWorld/HelloWorld.csproj
```

`dotnet new mstest` writes a project that already references the test SDK, MSTest and its adapter. The reference line is the one that matters: without it the test project cannot see `Greeter`, and the compiler will tell you the type does not exist even though the file is right there.

Delete the generated `UnitTest1.cs`. It exists only to prove that the template ran.

The repository ships the finished solution for this chapter. If you created your own project, keep the same names; otherwise open `fundamentals/hello-world` and run the commands from there.

## Back to testing

Create `tests/HelloWorld.Tests/GreeterTests.cs`:

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HelloWorld.Tests;

[TestClass]
public sealed class GreeterTests
{
    [TestMethod]
    public void TestHello()
    {
        var greeter = new Greeter();

        var got = greeter.SayHello();
        var want = "Hello, world";

        Assert.AreEqual(want, got);
    }
}
```

Run the tests from the chapter folder:

```sh
dotnet test
```

It should pass. To check that the test is testing something, change `want` to `"Hello, world!"` and run it again. You should see a failure that shows both values:

```text
  Failed TestHello [13 ms]
  Error Message:
   Assert.AreEqual failed. Expected:<Hello, world!>. Actual:<Hello, world>.
```

Put the string back. You have now seen the two states that matter: green when the code matches the specification, red when it does not.

### Writing tests

A test in MSTest follows a few rules:

- It lives in a test project.
- The class has a `[TestClass]` attribute.
- Each test method is `public void`, takes no parameters, and has a `[TestMethod]` attribute.
- Assertions come from `Assert`. `Assert.AreEqual(expected, actual)` is the one you will use most.

The method name is the description. When a test fails, MSTest prints the method name and the assertion message, so `TestHello` is enough while the class has one test. Later chapters use longer names when a class has several.

## Hello, YOU

In the last example we wrote the test after the code so you could see how a test is put together. From here on we write the test first.

Our next requirement is to let the caller choose who we greet.

Start with the test:

```csharp
[TestMethod]
public void SayHelloToChris()
{
    var greeter = new Greeter();

    var got = greeter.SayHello("Chris");
    var want = "Hello, Chris";

    Assert.AreEqual(want, got);
}
```

Run `dotnet test` and you get a compilation error:

```text
error CS1501: No overload for method 'SayHello' takes 1 arguments
```

Listen to the compiler. It is telling us the shape the code needs: `SayHello` has to accept an argument.

Change the method to take a `string`:

```csharp
public string SayHello(string name)
{
    return "Hello, world";
}
```

The test project compiles now, but `Program.cs` does not: it still calls `SayHello()` with no argument. Pass `"world"` in `Program.cs`:

```csharp
Console.WriteLine(greeter.SayHello("world"));
```

Run the tests again. The code compiles, and the test fails for the right reason:

```text
  Failed SayHelloToChris [13 ms]
  Error Message:
   Assert.AreEqual failed. Expected:<Hello, Chris>. Actual:<Hello, world>.
```

Make it pass by using the argument:

```csharp
public string SayHello(string name)
{
    return "Hello, " + name;
}
```

Run the tests. Green.

## A note on source control

If you are using source control, commit the code as it is. You have working software backed by a test. Do not push to main yet if you plan to refactor in the next section; the commit is a point you can come back to if the refactor goes wrong.

## Constants

There is not much to refactor, but we can give the greeting prefix a name:

```csharp
private const string EnglishHelloPrefix = "Hello, ";

public string SayHello(string name)
{
    return EnglishHelloPrefix + name;
}
```

Run the tests after the change. A constant captures the meaning of the value and keeps the same string from being repeated in several places.

## Hello, world... again

The next requirement: when the caller passes an empty string, greet the world instead of returning `Hello, `.

Write the new test first:

```csharp
[TestMethod]
public void SayHelloToWorldWhenEmptyStringIsSupplied()
{
    var greeter = new Greeter();

    var got = greeter.SayHello(string.Empty);
    var want = "Hello, World";

    Assert.AreEqual(want, got);
}
```

Run the tests. The new one fails with `Expected:<Hello, World>. Actual:<Hello, >`.

Fix the code with an `if`:

```csharp
public string SayHello(string name)
{
    if (string.IsNullOrEmpty(name))
    {
        name = "World";
    }

    return EnglishHelloPrefix + name;
}
```

Run the tests again. Both pass, and the old behavior still works.

The tests compare messages in the same way, and that repetition is in the tests. Refactoring is not just for production code:

```csharp
[TestClass]
public sealed class GreeterTests
{
    [TestMethod]
    public void SayHelloToPeople()
    {
        AssertCorrectMessage("Hello, Chris", new Greeter().SayHello("Chris"));
    }

    [TestMethod]
    public void SayHelloToWorldWhenEmptyStringIsSupplied()
    {
        AssertCorrectMessage("Hello, World", new Greeter().SayHello(string.Empty));
    }

    private static void AssertCorrectMessage(string want, string got)
    {
        Assert.AreEqual(want, got);
    }
}
```

The assertion now lives in one helper, and the tests read as a list of examples. `TestHello` and `SayHelloToChris` are gone because the method no longer has a parameterless shape; the two remaining tests describe the requirement as it stands.

Run the tests once more. Still green.

## Wrapping up

What we have covered:

- Creating a console project and running it with `dotnet run`.
- Separating the greeting, our domain, from `Console.WriteLine`, a side effect.
- Creating a test project, a solution and a project reference.
- The MSTest attributes and `Assert.AreEqual`.
- Red, green, refactor on two new requirements: a name argument and an empty string.
- A constant and a refactor in the tests.

The progression follows the [Hello, World chapter of Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/hello-world): start with a program that prints, move the logic out of the entry point, write a test, grow the function with a name, then handle the empty string.

Next: [Types and Variables](../types/types.md).

