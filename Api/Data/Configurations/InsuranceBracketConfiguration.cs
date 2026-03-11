using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class InsuranceBracketConfiguration : IEntityTypeConfiguration<InsuranceBracket>
{
    public void Configure(EntityTypeBuilder<InsuranceBracket> builder)
    {
        // ── Primary Key ──────────────────────────────────────────
        builder.HasKey(b => b.Id);

        // ── Properties ───────────────────────────────────────────
        builder.Property(b => b.SalaryBracket).IsRequired().HasColumnType("decimal(12,2)");
        builder.Property(b => b.LaborInsuranceEmployee).HasColumnType("decimal(10,2)");
        builder.Property(b => b.HealthInsuranceEmployee).HasColumnType("decimal(10,2)");
        builder.Property(b => b.CreatedAt).HasDefaultValueSql("DATEADD(hour, 8, GETUTCDATE())");

        // ── Unique Index ─────────────────────────────────────────
        builder.HasIndex(b => b.SalaryBracket).IsUnique();

        // ── Seed Data: 2026（民國 115）年勞健保費用對照表 ────────
        var d = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        builder.HasData(
            // 正式員工（級數 1~58）— 58 筆
            B( 1,  29500,  738,  458, d),
            B( 2,  30300,  758,  470, d),
            B( 3,  31800,  795,  493, d),
            B( 4,  33300,  833,  516, d),
            B( 5,  34800,  870,  540, d),
            B( 6,  36300,  908,  563, d),
            B( 7,  38200,  955,  592, d),
            B( 8,  40100, 1002,  622, d),
            B( 9,  42000, 1050,  651, d),
            B(10,  43900, 1098,  681, d),
            B(11,  45800, 1145,  710, d),
            B(12,  48200, 1145,  748, d),
            B(13,  50600, 1145,  785, d),
            B(14,  53000, 1145,  822, d),
            B(15,  55400, 1145,  859, d),
            B(16,  57800, 1145,  896, d),
            B(17,  60800, 1145,  943, d),
            B(18,  63800, 1145,  990, d),
            B(19,  66800, 1145, 1036, d),
            B(20,  69800, 1145, 1083, d),
            B(21,  72800, 1145, 1129, d),
            B(22,  76500, 1145, 1187, d),
            B(23,  80200, 1145, 1244, d),
            B(24,  83900, 1145, 1301, d),
            B(25,  87600, 1145, 1359, d),
            B(26,  92100, 1145, 1428, d),
            B(27,  96600, 1145, 1498, d),
            B(28, 101100, 1145, 1568, d),
            B(29, 105600, 1145, 1638, d),
            B(30, 110100, 1145, 1708, d),
            B(31, 115500, 1145, 1791, d),
            B(32, 120900, 1145, 1875, d),
            B(33, 126300, 1145, 1959, d),
            B(34, 131700, 1145, 2043, d),
            B(35, 137100, 1145, 2126, d),
            B(36, 142500, 1145, 2210, d),
            B(37, 147900, 1145, 2294, d),
            B(38, 150000, 1145, 2327, d),
            B(39, 156400, 1145, 2426, d),
            B(40, 162800, 1145, 2525, d),
            B(41, 169200, 1145, 2624, d),
            B(42, 175600, 1145, 2724, d),
            B(43, 182000, 1145, 2823, d),
            B(44, 189500, 1145, 2939, d),
            B(45, 197000, 1145, 3055, d),
            B(46, 204500, 1145, 3172, d),
            B(47, 212000, 1145, 3288, d),
            B(48, 219500, 1145, 3404, d),
            B(49, 228200, 1145, 3539, d),
            B(50, 236900, 1145, 3674, d),
            B(51, 245600, 1145, 3809, d),
            B(52, 254300, 1145, 3944, d),
            B(53, 263000, 1145, 4079, d),
            B(54, 273000, 1145, 4234, d),
            B(55, 283000, 1145, 4389, d),
            B(56, 293000, 1145, 4544, d),
            B(57, 303000, 1145, 4700, d),
            B(58, 313000, 1145, 4855, d)
        );
    }

    /// <summary>建立 Seed 資料的輔助方法</summary>
    private static InsuranceBracket B(int id, decimal salary, decimal labEmp, decimal hlthEmp, DateTime created)
    {
        return new InsuranceBracket
        {
            Id                      = id,
            SalaryBracket           = salary,
            LaborInsuranceEmployee  = labEmp,
            HealthInsuranceEmployee = hlthEmp,
            CreatedAt               = created,
        };
    }
}
