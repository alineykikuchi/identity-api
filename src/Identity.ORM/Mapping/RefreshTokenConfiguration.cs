using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.ORM.Mapping;

/// <summary>EF Core mapping for the <see cref="RefreshToken"/> entity.</summary>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.UserId).IsRequired();

        builder.Property(rt => rt.TokenHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(rt => rt.TokenHash);

        // Optional: null for logins that carried no client id.
        builder.Property(rt => rt.ApplicationId);

        builder.Property(rt => rt.ExpiresAt).IsRequired();
        builder.Property(rt => rt.RevokedAt);
        builder.Property(rt => rt.CreatedAt).IsRequired();

        // Computed in the domain; never persisted.
        builder.Ignore(rt => rt.IsActive);

        // FK to the owning user; deleting a user removes its refresh tokens.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK to the application the token is scoped to. Cascade rather than set-null:
        // clearing the column would silently turn an application-scoped token into a
        // global one, letting refresh mint tokens for an application that no longer exists.
        builder.HasOne<Application>()
            .WithMany()
            .HasForeignKey(rt => rt.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
