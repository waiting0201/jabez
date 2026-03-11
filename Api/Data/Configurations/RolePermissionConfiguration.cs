using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

        builder.HasOne(rp => rp.Role)
               .WithMany(r => r.RolePermissions)
               .HasForeignKey(rp => rp.RoleId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.Permission)
               .WithMany(p => p.RolePermissions)
               .HasForeignKey(rp => rp.PermissionId)
               .OnDelete(DeleteBehavior.Cascade);

        // Seed data — 對應資料庫 RolePermissions 表（67 筆）
        builder.HasData(
            // admin: 39 筆（不含 roles/permissions/attendances 權限）
            new RolePermission { RoleId = "admin", PermissionId = "2"  },
            new RolePermission { RoleId = "admin", PermissionId = "3"  },
            new RolePermission { RoleId = "admin", PermissionId = "4"  },
            new RolePermission { RoleId = "admin", PermissionId = "11" },
            new RolePermission { RoleId = "admin", PermissionId = "12" },
            new RolePermission { RoleId = "admin", PermissionId = "13" },
            new RolePermission { RoleId = "admin", PermissionId = "14" },
            new RolePermission { RoleId = "admin", PermissionId = "15" },
            new RolePermission { RoleId = "admin", PermissionId = "16" },
            new RolePermission { RoleId = "admin", PermissionId = "17" },
            new RolePermission { RoleId = "admin", PermissionId = "18" },
            new RolePermission { RoleId = "admin", PermissionId = "19" },
            new RolePermission { RoleId = "admin", PermissionId = "20" },
            new RolePermission { RoleId = "admin", PermissionId = "21" },
            new RolePermission { RoleId = "admin", PermissionId = "22" },
            new RolePermission { RoleId = "admin", PermissionId = "23" },
            new RolePermission { RoleId = "admin", PermissionId = "24" },
            new RolePermission { RoleId = "admin", PermissionId = "25" },
            new RolePermission { RoleId = "admin", PermissionId = "26" },
            new RolePermission { RoleId = "admin", PermissionId = "27" },
            new RolePermission { RoleId = "admin", PermissionId = "28" },
            new RolePermission { RoleId = "admin", PermissionId = "29" },
            new RolePermission { RoleId = "admin", PermissionId = "30" },
            new RolePermission { RoleId = "admin", PermissionId = "31" },
            new RolePermission { RoleId = "admin", PermissionId = "32" },
            new RolePermission { RoleId = "admin", PermissionId = "33" },
            new RolePermission { RoleId = "admin", PermissionId = "34" },
            new RolePermission { RoleId = "admin", PermissionId = "35" },
            new RolePermission { RoleId = "admin", PermissionId = "36" },
            new RolePermission { RoleId = "admin", PermissionId = "39" },
            new RolePermission { RoleId = "admin", PermissionId = "40" },
            new RolePermission { RoleId = "admin", PermissionId = "41" },
            new RolePermission { RoleId = "admin", PermissionId = "44" },
            new RolePermission { RoleId = "admin", PermissionId = "45" },
            new RolePermission { RoleId = "admin", PermissionId = "46" },
            new RolePermission { RoleId = "admin", PermissionId = "47" },
            new RolePermission { RoleId = "admin", PermissionId = "48" },
            new RolePermission { RoleId = "admin", PermissionId = "49" },
            new RolePermission { RoleId = "admin", PermissionId = "50" },

            // manager: 16 筆（員工讀寫 + 各申請模組完整 CRUD + 簽核作業）
            new RolePermission { RoleId = "manager", PermissionId = "2"  },
            new RolePermission { RoleId = "manager", PermissionId = "3"  },
            new RolePermission { RoleId = "manager", PermissionId = "23" },
            new RolePermission { RoleId = "manager", PermissionId = "24" },
            new RolePermission { RoleId = "manager", PermissionId = "25" },
            new RolePermission { RoleId = "manager", PermissionId = "26" },
            new RolePermission { RoleId = "manager", PermissionId = "27" },
            new RolePermission { RoleId = "manager", PermissionId = "28" },
            new RolePermission { RoleId = "manager", PermissionId = "29" },
            new RolePermission { RoleId = "manager", PermissionId = "30" },
            new RolePermission { RoleId = "manager", PermissionId = "31" },
            new RolePermission { RoleId = "manager", PermissionId = "32" },
            new RolePermission { RoleId = "manager", PermissionId = "33" },
            new RolePermission { RoleId = "manager", PermissionId = "34" },
            new RolePermission { RoleId = "manager", PermissionId = "35" },
            new RolePermission { RoleId = "manager", PermissionId = "36" },

            // viewer: 12 筆（各申請模組完整 CRUD）
            new RolePermission { RoleId = "viewer", PermissionId = "23" },
            new RolePermission { RoleId = "viewer", PermissionId = "24" },
            new RolePermission { RoleId = "viewer", PermissionId = "25" },
            new RolePermission { RoleId = "viewer", PermissionId = "28" },
            new RolePermission { RoleId = "viewer", PermissionId = "29" },
            new RolePermission { RoleId = "viewer", PermissionId = "30" },
            new RolePermission { RoleId = "viewer", PermissionId = "31" },
            new RolePermission { RoleId = "viewer", PermissionId = "32" },
            new RolePermission { RoleId = "viewer", PermissionId = "33" },
            new RolePermission { RoleId = "viewer", PermissionId = "34" },
            new RolePermission { RoleId = "viewer", PermissionId = "35" },
            new RolePermission { RoleId = "viewer", PermissionId = "36" }
        );
    }
}
