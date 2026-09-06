using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.ORM.Mapping;

/// <summary>EF Core mapping for the <see cref="Application"/> entity.</summary>
public class ApplicationConfiguration : IEntityTypeConfiguration<Application>
{
    public void Configure(EntityTypeBuilder<Application> builder)
    {
        builder.ToTable("Applications");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ClientId)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(a => a.ClientId)
            .IsUnique();

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(a => a.IsActive).IsRequired();
        builder.Property(a => a.CreatedAt).IsRequired();

        // Development seed: a sample consuming application. Anonymous objects are used
        // because the entity's setters are private.
        builder.HasData(new
        {
            Id = DevelopmentSeed.ApplicationId,
            ClientId = DevelopmentSeed.ApplicationClientId,
            Name = DevelopmentSeed.ApplicationName,
            IsActive = true,
            CreatedAt = DevelopmentSeed.CreatedAt
        });
    }
}
