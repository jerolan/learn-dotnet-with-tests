# Topic

[You can find all the code for this chapter here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/part/folder)

Before writing, read the closest chapter in [Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/). Mirror its rhythm, its level of explanation and its honesty about failures. Translate the language to .NET; do not translate the prose into a report about the chapter.

Open with the problem, not with a description of the chapter. One or two paragraphs that put the reader in front of something concrete: a program that prints, a wrong total, a missing type. Show the code, the command and the output. Do not start with "this chapter teaches" or with a list of conventions; the reader should learn the shape by reading it.

## How it works

Only the language or tooling the reader needs to understand the code they just saw.

## How to test

Separate the domain from the side effect, then write the test.

## Write the test first

Show the test. If the chapter has already shown production code, explain why this one starts with the test.

## Try to run the test

Run it and show the real compiler or test output. The failure should name what is missing.

## Write the minimal amount of code for the test to run and check the failing test output

Write enough to compile and nothing more. Then show the test failing for the right reason.

## Write enough code to make it pass

The smallest real implementation. Run the tests and show that they pass.

## Refactor

Clean the production code or the tests. Run the tests again. "Nothing to change" is a valid outcome, but say why.

## Repeat for new requirements

Each new requirement arrives as a failing test. Keep the steps small. Show the red, the change and the green for each one.

## A note on source control

Optional. Commit working software before a refactor.

## Wrapping up

A short list of what the reader can now do. Link the next chapter.

