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
    }
}
