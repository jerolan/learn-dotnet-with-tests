using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tasks.Tests;

[TestClass]
public sealed class TaskCombinatorsTests
{
    [TestMethod]
    public async Task SumAsync_AddsCompletedTasks()
    {
        var sum = await TaskCombinators.SumAsync(
            Task.FromResult(1),
            Task.FromResult(2),
            Task.FromResult(3));

        Assert.AreEqual(6, sum);
    }

    [TestMethod]
    public async Task SumAsync_WaitsForEveryTask()
    {
        var first = new TaskCompletionSource<int>();
        var second = new TaskCompletionSource<int>();

        var sumTask = TaskCombinators.SumAsync(first.Task, second.Task);

        Assert.IsFalse(sumTask.IsCompleted);

        first.SetResult(1);

        Assert.IsFalse(sumTask.IsCompleted);

        second.SetResult(2);

        Assert.AreEqual(3, await sumTask);
    }

    [TestMethod]
    public async Task FirstAsync_ReturnsTheFirstCompletedTask()
    {
        var slow = new TaskCompletionSource<int>();
        var fast = new TaskCompletionSource<int>();

        var firstTask = TaskCombinators.FirstAsync(slow.Task, fast.Task);

        fast.SetResult(20);

        Assert.AreEqual(20, await firstTask);
    }

    [TestMethod]
    public async Task SumAsync_WithNoTasks_ReturnsZero()
    {
        Assert.AreEqual(0, await TaskCombinators.SumAsync());
    }
}

