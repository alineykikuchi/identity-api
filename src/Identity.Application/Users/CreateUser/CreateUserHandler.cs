using Identity.Common.Security;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using MediatR;

namespace Identity.Application.Users.CreateUser;

/// <summary>
/// Handles user registration: rejects duplicate emails, hashes the password and
/// persists a new active user. Input is validated upstream by the pipeline.
/// </summary>
public class CreateUserHandler : IRequestHandler<CreateUserCommand, CreateUserResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public CreateUserHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<CreateUserResult> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var email = User.Normalize(command.Email);

        var existingUser = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (existingUser is not null)
        {
            // Generic message: never reveal details of the existing account. Mapped to 409.
            throw new InvalidOperationException("A user with the provided email already exists.");
        }

        var passwordHash = _passwordHasher.HashPassword(command.Password);
        var user = new User(email, passwordHash);

        var createdUser = await _userRepository.CreateAsync(user, cancellationToken);

        return new CreateUserResult
        {
            Id = createdUser.Id,
            Email = createdUser.Email
        };
    }
}
