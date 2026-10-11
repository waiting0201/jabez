using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class UserAuditLogConfiguration : IEntityTypeConfiguration<UserAuditLog>
{
    public void Configure(EntityTypeBuilder<UserAuditLog> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Action).IsRequired().HasMaxLength(50);
        builder.Property(l => l.Changes).HasMaxLength(2000);

        builder.HasIndex(l => new { l.TargetUserId, l.CreatedAt });
        builder.HasIndex(l => new { l.OperatorUserId, l.CreatedAt });

        // TargetUserId / OperatorUserId 刻意無 FK（見 entity 註解）
    }
}
