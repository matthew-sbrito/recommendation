using SharedKernel;
using SharedKernel.DomainEvents;

namespace Domain.Users;

public sealed record UserRegisteredDomainEvent(Guid UserId) : IDomainEvent;
