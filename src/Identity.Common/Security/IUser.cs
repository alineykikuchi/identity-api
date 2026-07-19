namespace Identity.Common.Security;

/// <summary>
/// Defines the contract for representing a user in the system.
/// </summary>
public interface IUser
{
    /// <summary>
    /// Gets the unique identifier of the user.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the username.
    /// </summary>
    string Username { get; }

    /// <summary>
    /// Gets the role of the user in the system.
    /// </summary>
    string Role { get; }
}
