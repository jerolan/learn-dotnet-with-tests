# Hello, World

Why it matters for backend .NET: every service starts from a tiny verified behavior. This chapter teaches the TDD loop on the smallest possible case.

## Write the test first

Add GreeterTests with SayHello_ReturnsHelloWorld asserting Greeter.SayHello returns Hello, World. Use TestClass, TestMethod, and Assert.AreEqual from MSTest.

## Try to run the test

Run dotnet test in fundamentals slash hello-world. It fails because Greeter does not exist yet.

## Write the minimal amount of code for the test to run and check the failing test output

Create the HelloWorld class library and reference it from HelloWorld.Tests. Confirm the failure names the missing Greeter type.

## Write enough code to make it pass

Add Greeter with SayHello returning Hello, World. Create the solution with dotnet new sln, classlib, and mstest, add projects, and add the reference.

## Refactor

Keep one class and one method. Keep one test with Arrange, Act, Assert. No extra branches.

## Repeat for new requirements

Change the test to expect a name parameter later, watch it fail, then extend SayHello. Same loop, slightly larger behavior.

## Wrapping up

Red, Green, Refactor is now muscle memory. The next chapters grow types, control flow, and errors on this rhythm.
