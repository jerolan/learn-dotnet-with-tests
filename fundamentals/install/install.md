# Install

Why it matters for backend .NET: nothing builds or tests without a working SDK, IDE, and test project. This chapter gets that foundation green with TDD.

## Write the test first

We want proof the toolchain works from a test, not from memory. Add SdkInfoTests that asserts SdkInfo.GetDotnetVersion returns a non-empty string.

Prerequisites covered here: install the .NET SDK from the official download page, pick VS Code with the C-Sharp extension or Visual Studio with .NET desktop development workload.

## Try to run the test

Run dotnet test in fundamentals slash install. It fails because SdkInfo does not exist yet. That failure is the point.

## Write the minimal amount of code for the test to run and check the failing test output

Create the InstallCheck class library and reference it from InstallCheck.Tests. Read the failing output to confirm it is about the missing type, not the environment.

## Write enough code to make it pass

Add SdkInfo with GetDotnetVersion returning Environment.Version. Create the solution with dotnet new sln, classlib, and mstest, add projects to the solution, and add the project reference.

## Refactor

Keep the production code to one static method. Keep the test to one assertion about non-empty version text. No extra helpers.

## Repeat for new requirements

Verify IDE setup: open the folder in VS Code or Visual Studio, restore, and run dotnet test again from the chapter folder. Same green result from both entry points.

## Wrapping up

The SDK, IDE, solution, and MSTest project are green. Every later chapter repeats this loop: test first, minimal code, refactor.
