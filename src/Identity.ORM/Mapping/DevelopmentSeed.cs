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
}
