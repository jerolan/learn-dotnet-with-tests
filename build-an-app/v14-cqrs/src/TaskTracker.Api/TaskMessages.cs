using MediatR;

namespace TaskTracker.Api;

public sealed record GetTasksQuery : IRequest<IReadOnlyList<TaskItem>>;

public sealed class GetTasksHandler : IRequestHandler<GetTasksQuery, IReadOnlyList<TaskItem>>
{
    private readonly ITaskReader _reader;

    public GetTasksHandler(ITaskReader reader)
    {
        _reader = reader;
    }

    public Task<IReadOnlyList<TaskItem>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_reader.All());
    }
}

public sealed record GetTaskQuery(int Id) : IRequest<TaskItem?>;

public sealed class GetTaskHandler : IRequestHandler<GetTaskQuery, TaskItem?>
{
    private readonly ITaskReader _reader;

    public GetTaskHandler(ITaskReader reader)
    {
        _reader = reader;
    }

    public Task<TaskItem?> Handle(GetTaskQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_reader.Find(request.Id));
    }
}

public sealed record CreateTaskCommand(string Title) : IRequest<TaskItem>;

public sealed class CreateTaskHandler : IRequestHandler<CreateTaskCommand, TaskItem>
{
    private readonly ITaskWriter _writer;

    public CreateTaskHandler(ITaskWriter writer)
    {
        _writer = writer;
    }

    public Task<TaskItem> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_writer.Add(request.Title));
    }
}

public sealed record MarkTaskDoneCommand(int Id) : IRequest<bool>;

public sealed class MarkTaskDoneHandler : IRequestHandler<MarkTaskDoneCommand, bool>
{
    private readonly ITaskWriter _writer;

    public MarkTaskDoneHandler(ITaskWriter writer)
    {
        _writer = writer;
    }

    public Task<bool> Handle(MarkTaskDoneCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_writer.MarkDone(request.Id) is not null);
    }
}

