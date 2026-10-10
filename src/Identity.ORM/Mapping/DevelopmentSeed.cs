namespace Identity.ORM.Mapping;

/// <summary>
/// Fixed identifiers and values for the development seed shipped with the migrations.
/// </summary>
/// <remarks>
/// EF Core's <c>HasData</c> requires deterministic values — generated ids or
/// <c>DateTime.UtcNow</c> would produce a new migration on every build — so the ids and
/// the creation timestamp are constants. They are exposed so tests can reference the
/// seeded rows without querying by name.
/// </remarks>
public static class DevelopmentSeed
{
    /// <summary>Client id of the seeded sample application.</summary>
    public const string ApplicationClientId = "developer-store";

    /// <summary>Display name of the seeded sample application.</summary>
    public const string ApplicationName = "Developer Store";

    /// <summary>Identifier of the seeded sample application.</summary>
    public static readonly Guid ApplicationId = new("0b6d3f2c-1a44-4f3e-9c1d-6a5f8e2b7c10");

    /// <summary>Identifier of the seeded "Admin" role.</summary>
    public static readonly Guid AdminRoleId = new("1c7e4a3d-2b55-4a4f-8d2e-7b6a9f3c8d21");

    /// <summary>Identifier of the seeded "Customer" role.</summary>
    public static readonly Guid CustomerRoleId = new("2d8f5b4e-3c66-4b5a-9e3f-8c7b0a4d9e32");

    /// <summary>Name of the seeded administrator role.</summary>
    public const string AdminRoleName = "Admin";

    /// <summary>Name of the seeded customer role.</summary>
    public const string CustomerRoleName = "Customer";

    /// <summary>Creation timestamp used by every seeded row (UTC).</summary>
    public static readonly DateTime CreatedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Client id of the seeded party-invitation application.</summary>
    /// <remarks>
    /// Registered so that application can sign in with a client id at all: a login whose
    /// client id matches no active application is refused with the generic invalid-credentials
    /// error, which is indistinguishable from a wrong password.
    ///
    /// No roles are seeded for it. That application has no role-based authorization — every
    /// authenticated user acts on their own records — and a user with no roles signs in
    /// normally, receiving a token with an audience and no role claim.
    /// </remarks>
    public const string PartyPlatformClientId = "festa-facil";

    /// <summary>Display name of the seeded party-invitation application.</summary>
    public const string PartyPlatformName = "Festa Fácil";

    /// <summary>Identifier of the seeded party-invitation application.</summary>
    public static readonly Guid PartyPlatformApplicationId = new("3e9a6c5f-4d77-4c6b-af40-9d8c1b5e0f43");
}
