using System.Collections.Concurrent;
using Grpc.Core;
using TaskApi.Grpc;

namespace TaskApi;

public sealed class TaskGrpcService : TaskService.TaskServiceBase
{
    private readonly ConcurrentDictionary<Guid, string> _tasks = new();

    public override Task<TaskReply> Create(CreateTaskRequest request, ServerCallContext context)
    {
        var id = Guid.NewGuid();
        _tasks[id] = request.Title;

        return Task.FromResult(new TaskReply { Id = id.ToString(), Title = request.Title });
    }

    public override Task<TaskReply> Get(GetTaskRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id) || !_tasks.TryGetValue(id, out var title))
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Task not found."));
        }

        return Task.FromResult(new TaskReply { Id = id.ToString(), Title = title });
    }
}
