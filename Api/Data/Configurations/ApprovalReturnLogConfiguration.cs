using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class ApprovalReturnLogConfiguration : IEntityTypeConfiguration<ApprovalReturnLog>
{
    public void Configure(EntityTypeBuilder<ApprovalReturnLog> builder)
    {
        builder.ToTable("ApprovalReturnLogs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ApplicationType).IsRequired().HasMaxLength(30);
        builder.Property(x => x.Action).IsRequired().HasMaxLength(20);
        builder.Property(x => x.ReviewerName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.ReviewerJobTitle).HasMaxLength(100);
        builder.Property(x => x.ReviewerDepartmentName).HasMaxLength(100);
        builder.Property(x => x.OnBehalfOfName).HasMaxLength(100);
        builder.Property(x => x.ReviewNote).HasMaxLength(500);
        builder.Property(x => x.RoundNo).HasDefaultValue(1);

        // ReviewedById 刻意無 FK（見 entity 註解）
        builder.HasIndex(x => new { x.ApplicationType, x.ApplicationId });
    }
}
