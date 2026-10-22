namespace Mocking;

public sealed record User(string Name);

public interface IUserRepository
{
    User? Find(int id);
}

public sealed class UserService
{
    private readonly IUserRepository _repository;

    public UserService(IUserRepository repository)
    {
        _repository = repository;
    }

    public string GetName(int id)
    {
        var user = _repository.Find(id);

        return user?.Name ?? "Unknown";
    }
}

