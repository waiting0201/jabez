using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.HasKey(ur => new { ur.UserId, ur.RoleId });

        builder.HasOne(ur => ur.User)
               .WithMany(u => u.UserRoles)
               .HasForeignKey(ur => ur.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ur => ur.Role)
               .WithMany(r => r.UserRoles)
               .HasForeignKey(ur => ur.RoleId)
               .OnDelete(DeleteBehavior.Cascade);

        // Seed data
        // Seed: 與本機資料庫同步（2026-03-24）
        builder.HasData(
            new UserRole { UserId = new Guid("b56b8afd-1663-4317-9007-4560da27239d"), RoleId = "admin"   },                                    // Charles → 後端管理者
            new UserRole { UserId = new Guid("6452ad1e-9648-4194-8fb0-0ac55a76f992"), RoleId = "manager" },                                    // Hank → 總管理處
            new UserRole { UserId = new Guid("11111111-1111-1111-1111-111111111111"), RoleId = "manager" },                                    // 洪薇淳 → 總管理處
            new UserRole { UserId = new Guid("22222222-2222-2222-2222-222222222222"), RoleId = "manager" },                                    // Bob → 總管理處
            new UserRole { UserId = new Guid("83f6b1f7-2f25-4f9b-b102-37d1a27f0b35"), RoleId = "manager" },                                    // 陳珊雯 → 總管理處
            new UserRole { UserId = new Guid("281c2016-801e-48eb-b73b-751643464f48"), RoleId = "manager" },                                    // Ting → 總管理處
            new UserRole { UserId = new Guid("33333333-3333-3333-3333-333333333333"), RoleId = "viewer"  },                                    // Carol → 一般員工
            new UserRole { UserId = new Guid("df5d56ad-dd46-4fca-948c-d8301610997a"), RoleId = "3afbfc1e-4caa-4a4e-af1e-ebdc0d9002b4" },       // 徐嘉秀 → 員工-測試
            new UserRole { UserId = new Guid("6a4002be-23e0-4343-8092-f221b97c5098"), RoleId = "fe015c41-d9a8-48fa-994d-5588b9c4a92b" }        // 張雅婷 → 經理副理主管-測試
        );
    }
}
