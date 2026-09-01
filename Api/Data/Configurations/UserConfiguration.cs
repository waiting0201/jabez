using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(u => u.Email)
               .IsRequired()
               .HasMaxLength(200);

        builder.HasIndex(u => u.Email)
               .IsUnique();

        builder.Property(u => u.PasswordHash)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(u => u.Avatar)
               .HasMaxLength(500);

        builder.Property(u => u.AvatarPositionX)
               .HasPrecision(5, 2)
               .HasDefaultValue(50m);

        builder.Property(u => u.AvatarPositionY)
               .HasPrecision(5, 2)
               .HasDefaultValue(50m);

        builder.Property(u => u.AvatarScale)
               .HasPrecision(3, 2)
               .HasDefaultValue(1m);

        builder.Property(u => u.SignatureUrl)
               .HasMaxLength(500);

        builder.Property(u => u.IndigenousProofUrl)
               .HasMaxLength(500);

        // 低收入 / 身心障礙身份
        builder.Property(u => u.IsLowIncome)
               .HasDefaultValue(false);

        builder.Property(u => u.LowIncomeProofUrl)
               .HasMaxLength(500);

        builder.Property(u => u.IsDisabled)
               .HasDefaultValue(false);

        builder.Property(u => u.DisabledProofUrl)
               .HasMaxLength(500);

        // 健保 / 勞保金額手動覆寫
        builder.Property(u => u.HealthInsuranceOverride)
               .HasColumnType("decimal(18,2)");

        builder.Property(u => u.LaborInsuranceOverride)
               .HasColumnType("decimal(18,2)");

        // 勞退自提率（%，0~6 整數，業務層驗證，DB 層留彈性）
        builder.Property(u => u.LaborPensionSelfContributionRate)
               .HasPrecision(5, 2);

        builder.Property(u => u.Status)
               .IsRequired()
               .HasMaxLength(20)
               .HasDefaultValue("active");

        builder.Property(u => u.CreatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        builder.Property(u => u.UpdatedAt)
               .HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        builder.Property(u => u.IsSuperAdmin)
               .HasDefaultValue(false);

        builder.Property(u => u.MustChangePassword)
               .HasDefaultValue(false);

        builder.Property(u => u.IsIndigenous)
               .HasDefaultValue(false);

        // LINE 綁定
        builder.Property(u => u.LineUserId).HasMaxLength(50);
        builder.HasIndex(u => u.LineUserId)
               .IsUnique()
               .HasFilter("[LineUserId] IS NOT NULL");

        // Employee fields
        builder.Property(u => u.BaseSalary)
               .HasColumnType("decimal(18,2)");

        // 期初補休時數（系統上線前累計，116/6/30 到期歸零）
        builder.Property(u => u.CompensatoryOpeningHours)
               .HasColumnType("decimal(18,2)")
               .HasDefaultValue(0m);

        // 排班制員工（六日與國定假日視為工作日）
        builder.Property(u => u.IsShiftWorker)
               .HasDefaultValue(false);

        builder.HasOne(u => u.Department)
               .WithMany(d => d.Users)
               .HasForeignKey(u => u.DepartmentId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(u => u.JobTitle)
               .WithMany(j => j.Users)
               .HasForeignKey(u => u.JobTitleId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(u => u.Agent)
               .WithMany()
               .HasForeignKey(u => u.AgentUserId)
               .OnDelete(DeleteBehavior.NoAction);

        // Seed data（加入 Employee 欄位）
        // Seed: 與本機資料庫同步（2026-03-24）
        builder.HasData(
            // 超管帳號（隱藏，不出現在使用者管理列表）
            new User
            {
                Id           = new Guid("00000000-0000-0000-0000-000000000001"),
                Name         = "System Admin",
                Email        = "sa@system.local",
                PasswordHash = "$2a$11$hBaZunc8xtFIsRVh738SJuHisvnVAsIODyfkzLxjMN.is7jZn3K7e",
                Status       = "active",
                IsSuperAdmin = true,
                CreatedAt    = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt    = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id           = new Guid("11111111-1111-1111-1111-111111111111"),
                Name         = "洪薇淳",
                Email        = "cherng1217@hotmail.com",
                PasswordHash = "$2a$11$uanLV/06EWO8c4vt4h0gv.NwFEDxbAzLGfO7M1wvbeyZW0oScGCqy",
                Status       = "active",
                DepartmentId = 2,  // 行政財務部
                JobTitleId   = 7,  // CFO
                HireDate     = new DateTime(2023, 12, 14, 0, 0, 0, DateTimeKind.Utc),
                BaseSalary   = 80000m,
                Birthday     = new DateTime(1990, 5, 10, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt    = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt    = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id           = new Guid("22222222-2222-2222-2222-222222222222"),
                Name         = "Bob Wang",
                Email        = "bob@example.com",
                PasswordHash = "$2a$11$jnbTwU3kFJLXuVyQecTy4e1rMkMfsBa191aEHHZswHpLcI2jRnJzW",
                Status       = "active",
                DepartmentId = 1,  // 會計部
                JobTitleId   = 9,  // 專案經理
                HireDate     = new DateTime(2024, 2, 7, 0, 0, 0, DateTimeKind.Utc),
                BaseSalary   = 60000m,
                Birthday     = new DateTime(1988, 8, 22, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt    = new DateTime(2024, 2, 10, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt    = new DateTime(2024, 2, 10, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id           = new Guid("33333333-3333-3333-3333-333333333333"),
                Name         = "Carol Liu",
                Email        = "carol@example.com",
                PasswordHash = "$2a$11$zq1cZo7mM27tuI.W4ZwbKuu5rn3PBsBeP7IzdEPw8i1ynhJJltP5m",
                Status       = "active",
                DepartmentId = 3,  // 疆界
                JobTitleId   = 1,  // 工程師
                HireDate     = new DateTime(2024, 3, 2, 0, 0, 0, DateTimeKind.Utc),
                BaseSalary   = 50000m,
                Birthday     = new DateTime(1995, 3, 10, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt    = new DateTime(2024, 3, 5, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt    = new DateTime(2024, 3, 5, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id           = new Guid("6452ad1e-9648-4194-8fb0-0ac55a76f992"),
                Name         = "Hank",
                Email        = "hank@example.com",
                PasswordHash = "$2a$11$yNJIPqp5rr/PhDhajQJwIOoQ88yVQbJQLuU.tRPOmH4/xEmlsPb3i",
                Status       = "active",
                DepartmentId = 4,  // 總監室
                JobTitleId   = 5,  // 總監
                HireDate     = new DateTime(2010, 1, 4, 0, 0, 0, DateTimeKind.Utc),
                BaseSalary   = 100000m,
                Birthday     = new DateTime(1975, 2, 2, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt    = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt    = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id           = new Guid("b56b8afd-1663-4317-9007-4560da27239d"),
                Name         = "Charles",
                Email        = "cherng1217@gmail.com",
                PasswordHash = "$2a$11$J5eke3qMONqpznSkAUKQ9OaIwKYOv52VqF2cu.fSU3qtKiYaS7NLO",
                Status       = "active",
                DepartmentId = 5,  // 雅比斯總公司管理部
                JobTitleId   = 3,  // 主任工程師
                HireDate     = new DateTime(2012, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                BaseSalary   = 62530m,
                Birthday     = new DateTime(1986, 12, 17, 0, 0, 0, DateTimeKind.Utc),
                MealAllowance = 3000m,
                OvertimePay  = 3000m,
                CreatedAt    = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt    = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id           = new Guid("281c2016-801e-48eb-b73b-751643464f48"),
                Name         = "Ting",
                Email        = "Ting@example.com",
                PasswordHash = "$2a$11$FXRjg2SgqKby.WL/hEMkoupv3pl/iffpeyvBxRjF0qY51SFwClWiK",
                Status       = "active",
                DepartmentId = 5,  // 雅比斯總公司管理部
                JobTitleId   = 8,  // 協理
                HireDate     = new DateTime(2020, 5, 29, 0, 0, 0, DateTimeKind.Utc),
                BaseSalary   = 80000m,
                Birthday     = new DateTime(1981, 1, 24, 0, 0, 0, DateTimeKind.Utc),
                MealAllowance = 2400m,
                OvertimePay  = 2400m,
                CreatedAt    = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt    = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id           = new Guid("83f6b1f7-2f25-4f9b-b102-37d1a27f0b35"),
                Name         = "陳珊雯",
                Email        = "accounting@example.com",
                PasswordHash = "$2a$11$QajMz1QlJ4W.w.EdwzAH6eRQjThAHAbtYK09WM0RfOehln.FKH8ym",
                Status       = "active",
                DepartmentId = 1,  // 會計部
                JobTitleId   = 11, // 會計
                HireDate     = new DateTime(2009, 12, 29, 0, 0, 0, DateTimeKind.Utc),
                BaseSalary   = 55000m,
                Birthday     = new DateTime(1985, 12, 29, 0, 0, 0, DateTimeKind.Utc),
                MealAllowance = 2450m,
                OvertimePay  = 2570m,
                CreatedAt    = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt    = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id           = new Guid("6a4002be-23e0-4343-8092-f221b97c5098"),
                Name         = "張雅婷",
                Email        = "tin@jacreative.com.tw",
                PasswordHash = "$2a$11$UVOY3lj8t7A4YnxhKBAigedGTYYIkBfFxl3bmBaUuBxq0JEs54k6e",
                Status       = "active",
                DepartmentId = 8,  // 雅比斯專案
                JobTitleId   = 10, // 經理
                HireDate     = new DateTime(2019, 12, 26, 0, 0, 0, DateTimeKind.Utc),
                BaseSalary   = 65000m,
                Birthday     = new DateTime(1982, 12, 27, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt    = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt    = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id           = new Guid("df5d56ad-dd46-4fca-948c-d8301610997a"),
                Name         = "徐嘉秀",
                Email        = "arwen@jacreative.com.tw",
                PasswordHash = "$2a$11$2v7wVtBf77gz0vuZ/jZGseOQ2dvureuLomMb5HVECKoBNO3wcyzYy",
                Status       = "active",
                DepartmentId = 9,  // 壯圍營業所
                JobTitleId   = 4,  // 專案副理/店長
                HireDate     = new DateTime(2024, 7, 28, 0, 0, 0, DateTimeKind.Utc),
                BaseSalary   = 35000m,
                Birthday     = new DateTime(1972, 8, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt    = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt    = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
