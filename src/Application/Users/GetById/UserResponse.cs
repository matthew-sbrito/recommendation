using Domain.Users;

namespace Application.Users.GetById;

public sealed record UserResponse
{
    public Guid Id { get; init; }
    public string Email { get; init; }
    public string FirstName { get; init; }
    public string LastName { get; init; }
    public DateOnly BirthDate { get; init; }
    public Gender Gender { get; init; }
}
