namespace Identity.Domain.Enums;

/// <summary>
/// Lifecycle status of a user account. Only <see cref="Active"/> users may authenticate.
/// </summary>
public enum UserStatus
{
    /// <summary>The account is active and may authenticate.</summary>
    Active,

    /// <summary>The account is disabled and may not authenticate.</summary>
    Inactive,

    /// <summary>The account is temporarily suspended and may not authenticate.</summary>
    Suspended
}
