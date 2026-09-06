using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.ORM.Mapping;

/// <summary>EF Core mapping for the <see cref="Role"/> entity.</summary>
public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ApplicationId).IsRequired();

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(r => r.Description)
            .HasMaxLength(256);

        // A role name only means something inside its own application: "Admin" here is a
        // different role from "Admin" registered by another application.
        builder.HasIndex(r => new { r.ApplicationId, r.Name })
            .IsUnique();

        // FK to the owning application; removing an application removes its roles.
        builder.HasOne<Application>()
            .WithMany()
            .HasForeignKey(r => r.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Development seed: the two sample roles of the seeded application.
        builder.HasData(
            new
            {
                Id = DevelopmentSeed.AdminRoleId,
                ApplicationId = DevelopmentSeed.ApplicationId,
                Name = DevelopmentSeed.AdminRoleName,
                Description = (string?)"Full access to the sample application"
            },
            new
            {
                Id = DevelopmentSeed.CustomerRoleId,
                ApplicationId = DevelopmentSeed.ApplicationId,
                Name = DevelopmentSeed.CustomerRoleName,
                Description = (string?)"Regular customer of the sample application"
            });
    }
}
