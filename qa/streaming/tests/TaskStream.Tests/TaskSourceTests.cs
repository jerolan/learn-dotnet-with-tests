using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TaskStream.Tests;

[TestClass]
public sealed class TaskSourceTests
{
    [TestMethod]
    public async Task ReadsEveryItemInOrder()
    {
        var tasks = new[]
        {
            new TaskItem(Guid.NewGuid(), "First"),
            new TaskItem(Guid.NewGuid(), "Second"),
            new TaskItem(Guid.NewGuid(), "Third")
        };
        var source = new TaskSource(tasks);

        var seen = new List<TaskItem>();

        await foreach (var task in source.ReadAsync())
        {
            seen.Add(task);
        }

        CollectionAssert.AreEqual(tasks, seen);
    }

    [TestMethod]
    public async Task CancellationStopsTheStream()
    {
        var tasks = Enumerable.Range(1, 5)
            .Select(index => new TaskItem(Guid.NewGuid(), $"Task {index}"))
            .ToArray();
        var source = new TaskSource(tasks);
        using var cancellation = new CancellationTokenSource();
        var seen = new List<TaskItem>();

        await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
        {
            await foreach (var task in source.ReadAsync(cancellation.Token))
            {
                seen.Add(task);
                cancellation.Cancel();
            }
        });

        Assert.AreEqual(1, seen.Count);
    }

    [TestMethod]
    public async Task ItemsBeforeAFailureAreDelivered()
    {
        var seen = new List<int>();

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
        {
            await foreach (var item in FailsAfterOne())
            {
                seen.Add(item);
            }
        });

        CollectionAssert.AreEqual(new[] { 1 }, seen);
    }

    private static async IAsyncEnumerable<int> FailsAfterOne()
    {
        yield return 1;
        await Task.Yield();
        throw new InvalidOperationException("The source failed.");
    }
}
