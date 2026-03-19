using Application.Abstractions.Messaging;
using Domain.Users;

namespace Application.Users.Register;

public sealed record RegisterUserCommand(
    string Email,
    string FirstName,
    string LastName,
    string Password,
    DateOnly BirthDate,
    Gender Gender)
    : ICommand<Guid>;
