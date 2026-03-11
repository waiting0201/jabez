using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jabez.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixReportPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Ensure Permissions 48, 49, 50 exist (they were defined in Configuration
            //    HasData but never had a corresponding InsertData in any migration)
            migrationBuilder.Sql(@"
                INSERT INTO [Permissions] ([Id], [Code], [Name], [Module])
                SELECT v.[Id], v.[Code], v.[Name], v.[Module]
                FROM (VALUES
                    ('48', 'reports-overtime:read',            'View Overtime Report',             'Reports'),
                    ('49', 'reports-payment:read',             'View Payment Report',              'Reports'),
                    ('50', 'reports-project-water-level:read', 'View Project Water Level Report',  'Reports')
                ) AS v([Id], [Code], [Name], [Module])
                WHERE NOT EXISTS (
                    SELECT 1 FROM [Permissions] p WHERE p.[Id] = v.[Id]
                );");

            // 2. Fix permission codes / names for existing rows
            migrationBuilder.Sql(
                "UPDATE [Permissions] SET [Code] = 'reports-overtime:read', [Name] = 'View Overtime Report', [Module] = 'Reports' WHERE [Id] = '48';");
            migrationBuilder.Sql(
                "UPDATE [Permissions] SET [Code] = 'reports-payment:read', [Name] = 'View Payment Report', [Module] = 'Reports' WHERE [Id] = '49';");
            migrationBuilder.Sql(
                "UPDATE [Permissions] SET [Name] = 'View Project Water Level Report', [Module] = 'Reports' WHERE [Id] = '50';");

            // 3. Add missing RolePermission entries (skip if already exist)
            migrationBuilder.Sql(@"
                INSERT INTO [RolePermissions] ([RoleId], [PermissionId])
                SELECT v.[RoleId], v.[PermissionId]
                FROM (VALUES
                    ('admin',   '47'),
                    ('admin',   '48'),
                    ('admin',   '49'),
                    ('admin',   '50'),
                    ('manager', '41'),
                    ('manager', '47'),
                    ('manager', '48'),
                    ('manager', '49'),
                    ('manager', '50')
                ) AS v([RoleId], [PermissionId])
                WHERE NOT EXISTS (
                    SELECT 1 FROM [RolePermissions] rp
                    WHERE rp.[RoleId] = v.[RoleId] AND rp.[PermissionId] = v.[PermissionId]
                );");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert permission codes
            migrationBuilder.Sql(
                "UPDATE [Permissions] SET [Code] = 'overtimes:read' WHERE [Id] = '48';");
            migrationBuilder.Sql(
                "UPDATE [Permissions] SET [Code] = 'payment:read' WHERE [Id] = '49';");

            // Remove added RolePermission entries for manager
            migrationBuilder.Sql(
                "DELETE FROM [RolePermissions] WHERE [RoleId] = 'manager' AND [PermissionId] IN ('47','48','49','50');");
        }
    }
}
