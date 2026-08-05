using Jabez.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jabez.Api.Data.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .IsRequired()
               .HasMaxLength(20);

        builder.Property(p => p.Code)
               .IsRequired()
               .HasMaxLength(100);

        builder.HasIndex(p => p.Code)
               .IsUnique();

        builder.Property(p => p.Name)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(p => p.Module)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(p => p.Description)
               .HasMaxLength(500);

        // Seed data — 對應資料庫 Permissions 表
        builder.HasData(
            // 員工管理
            new Permission { Id = "2",  Code = "users:read",                       Name = "瀏覽",     Module = "員工管理",   Description = "員工管理" },
            new Permission { Id = "3",  Code = "users:write",                      Name = "新增/修改", Module = "員工管理",   Description = "員工管理" },
            new Permission { Id = "4",  Code = "users:delete",                     Name = "刪除",     Module = "員工管理",   Description = "員工管理" },
            // 角色管理
            new Permission { Id = "5",  Code = "roles:read",                       Name = "瀏覽",     Module = "角色管理",   Description = "角色管理" },
            new Permission { Id = "6",  Code = "roles:write",                      Name = "新增/修改", Module = "角色管理",   Description = "角色管理" },
            new Permission { Id = "7",  Code = "roles:delete",                     Name = "刪除",     Module = "角色管理",   Description = "角色管理" },
            // 權限管理
            new Permission { Id = "8",  Code = "permissions:read",                 Name = "瀏覽",     Module = "權限管理",   Description = "權限管理" },
            new Permission { Id = "9",  Code = "permissions:write",                Name = "新增/修改", Module = "權限管理",   Description = "權限管理" },
            new Permission { Id = "10", Code = "permissions:delete",               Name = "刪除",     Module = "權限管理",   Description = "權限管理" },
            // 部門管理
            new Permission { Id = "11", Code = "departments:read",                 Name = "瀏覽",     Module = "部門管理",   Description = "部門管理" },
            new Permission { Id = "12", Code = "departments:write",                Name = "新增/修改", Module = "部門管理",   Description = "部門管理" },
            new Permission { Id = "13", Code = "departments:delete",               Name = "刪除",     Module = "部門管理",   Description = "部門管理" },
            // 職稱管理
            new Permission { Id = "14", Code = "job-titles:read",                  Name = "瀏覽",     Module = "職稱管理",   Description = "職稱管理" },
            new Permission { Id = "15", Code = "job-titles:write",                 Name = "新增/修改", Module = "職稱管理",   Description = "職稱管理" },
            new Permission { Id = "16", Code = "job-titles:delete",                Name = "刪除",     Module = "職稱管理",   Description = "職稱管理" },
            // 簽核管理
            new Permission { Id = "17", Code = "approvals:read",                   Name = "瀏覽",     Module = "簽核管理",   Description = "簽核管理" },
            new Permission { Id = "18", Code = "approvals:write",                  Name = "新增/修改", Module = "簽核管理",   Description = "簽核管理" },
            new Permission { Id = "19", Code = "approvals:delete",                 Name = "刪除",     Module = "簽核管理",   Description = "簽核管理" },
            // 專案管理
            new Permission { Id = "20", Code = "projects:read",                    Name = "瀏覽",     Module = "專案管理",   Description = "專案管理" },
            new Permission { Id = "21", Code = "projects:write",                   Name = "新增/修改", Module = "專案管理",   Description = "專案管理" },
            new Permission { Id = "22", Code = "projects:delete",                  Name = "刪除",     Module = "專案管理",   Description = "專案管理" },
            // 請款申請
            new Permission { Id = "23", Code = "payment-requests:read",            Name = "瀏覽",     Module = "請款申請",   Description = "請款申請" },
            new Permission { Id = "24", Code = "payment-requests:write",           Name = "新增/修改", Module = "請款申請",   Description = "請款申請" },
            new Permission { Id = "25", Code = "payment-requests:delete",          Name = "刪除",     Module = "請款申請",   Description = "請款申請" },
            // 簽核作業
            new Permission { Id = "26", Code = "approval-tasks:read",              Name = "瀏覽",     Module = "簽核作業",   Description = "簽核作業" },
            new Permission { Id = "27", Code = "approval-tasks:write",             Name = "審核",     Module = "簽核作業",   Description = "簽核作業" },
            new Permission { Id = "67", Code = "approval-tasks:batch-approve",     Name = "全選核准", Module = "簽核作業",   Description = "簽核作業" },
            // 請假申請
            new Permission { Id = "28", Code = "leave-requests:read",              Name = "瀏覽",     Module = "請假申請",   Description = "請假申請" },
            new Permission { Id = "29", Code = "leave-requests:write",             Name = "新增/修改", Module = "請假申請",   Description = "請假申請" },
            new Permission { Id = "30", Code = "leave-requests:delete",            Name = "刪除",     Module = "請假申請",   Description = "請假申請" },
            // 出差預支申請
            new Permission { Id = "31", Code = "travel-requests:read",             Name = "瀏覽",     Module = "出差預支申請",   Description = "出差預支申請" },
            new Permission { Id = "32", Code = "travel-requests:write",            Name = "新增/修改", Module = "出差預支申請",   Description = "出差預支申請" },
            new Permission { Id = "33", Code = "travel-requests:delete",           Name = "刪除",     Module = "出差預支申請",   Description = "出差預支申請" },
            // 加班申請
            new Permission { Id = "34", Code = "overtime-requests:read",           Name = "瀏覽",     Module = "加班申請",   Description = "加班申請" },
            new Permission { Id = "35", Code = "overtime-requests:write",          Name = "新增/修改", Module = "加班申請",   Description = "加班申請" },
            new Permission { Id = "36", Code = "overtime-requests:delete",         Name = "刪除",     Module = "加班申請",   Description = "加班申請" },
            // 出勤打卡（員工本人的打卡動作）
            // Id 37/38 為重用的歷史空號：2026-02 曾建立、2026-03 由 SyncSeedDataWithDatabase 刪除，2026-08 重新啟用。
            // 刻意不取 78+ —— PermissionHandler.CreateAsync 以 max(Id)+1 配號，78 起可能已被 UI 建立的權限占用。
            new Permission { Id = "37", Code = "attendances:read",                 Name = "瀏覽",     Module = "出勤打卡",   Description = "出勤打卡" },
            new Permission { Id = "38", Code = "attendances:write",                Name = "打卡",     Module = "出勤打卡",   Description = "出勤打卡" },
            // 系統設定
            new Permission { Id = "39", Code = "settings:read",                    Name = "瀏覽",     Module = "系統設定",   Description = "系統設定" },
            new Permission { Id = "40", Code = "settings:write",                   Name = "編輯",     Module = "系統設定",   Description = "系統設定" },
            // Reports
            new Permission { Id = "41", Code = "reports-attendance:read",          Name = "出缺勤紀錄", Module = "Reports" },
            // Id 42 為重用的歷史空號（原 reports-leave:read，已由 RemoveUnusedReportPermissions 刪除）
            new Permission { Id = "42", Code = "reports-attendance:write",         Name = "出缺勤紀錄編輯", Module = "Reports" },
            // 勞健保級距
            new Permission { Id = "44", Code = "insurance-brackets:read",          Name = "瀏覽",     Module = "勞健保級距" },
            new Permission { Id = "45", Code = "insurance-brackets:write",         Name = "新增/修改", Module = "勞健保級距" },
            new Permission { Id = "46", Code = "insurance-brackets:delete",        Name = "刪除",     Module = "勞健保級距" },
            // 人事薪資
            new Permission { Id = "47", Code = "payroll:read",                     Name = "瀏覽",     Module = "人事薪資" },
            new Permission { Id = "66", Code = "payroll:write",                    Name = "新增/修改", Module = "人事薪資" },
            // Reports
            new Permission { Id = "48", Code = "reports-overtime:read",            Name = "加班紀錄",   Module = "Reports" },
            new Permission { Id = "49", Code = "reports-payment:read",             Name = "款項統計",   Module = "Reports" },
            new Permission { Id = "50", Code = "reports-project-water-level:read", Name = "專案水位表", Module = "Reports" },
            // 預支申請
            new Permission { Id = "51", Code = "advance-requests:read",   Name = "瀏覽",     Module = "預支申請" },
            new Permission { Id = "52", Code = "advance-requests:write",  Name = "新增/修改", Module = "預支申請" },
            new Permission { Id = "53", Code = "advance-requests:delete", Name = "刪除",     Module = "預支申請" },
            // 預支沖銷申請
            new Permission { Id = "54", Code = "write-off-requests:read",          Name = "瀏覽",     Module = "預支沖銷申請" },
            new Permission { Id = "55", Code = "write-off-requests:write",         Name = "新增/修改", Module = "預支沖銷申請" },
            new Permission { Id = "56", Code = "write-off-requests:delete",        Name = "刪除",     Module = "預支沖銷申請" },
            // 出差預支沖銷申請
            new Permission { Id = "57", Code = "travel-write-off-requests:read",   Name = "瀏覽",     Module = "出差預支沖銷申請" },
            new Permission { Id = "58", Code = "travel-write-off-requests:write",  Name = "新增/修改", Module = "出差預支沖銷申請" },
            new Permission { Id = "59", Code = "travel-write-off-requests:delete", Name = "刪除",     Module = "出差預支沖銷申請" },
            // 假日執行活動申請
            new Permission { Id = "60", Code = "holiday-travel-requests:read",   Name = "瀏覽",     Module = "假日執行活動申請" },
            new Permission { Id = "61", Code = "holiday-travel-requests:write",  Name = "新增/修改", Module = "假日執行活動申請" },
            new Permission { Id = "62", Code = "holiday-travel-requests:delete", Name = "刪除",     Module = "假日執行活動申請" },
            // 行事曆管理
            new Permission { Id = "63", Code = "calendar-days:read",   Name = "瀏覽",     Module = "行事曆管理" },
            new Permission { Id = "64", Code = "calendar-days:write",  Name = "新增/修改", Module = "行事曆管理" },
            new Permission { Id = "65", Code = "calendar-days:delete", Name = "刪除",     Module = "行事曆管理" },
            // 出差請款申請
            new Permission { Id = "68", Code = "travel-payment-requests:read",   Name = "瀏覽",     Module = "出差請款申請" },
            new Permission { Id = "69", Code = "travel-payment-requests:write",  Name = "新增/修改", Module = "出差請款申請" },
            new Permission { Id = "70", Code = "travel-payment-requests:delete", Name = "刪除",     Module = "出差請款申請" },
            // 廠商管理
            new Permission { Id = "71", Code = "vendors:read",   Name = "瀏覽",     Module = "廠商管理",   Description = "廠商管理" },
            new Permission { Id = "72", Code = "vendors:write",  Name = "新增/修改", Module = "廠商管理",   Description = "廠商管理" },
            new Permission { Id = "73", Code = "vendors:delete", Name = "刪除",     Module = "廠商管理",   Description = "廠商管理" },
            // LINE 整合
            new Permission { Id = "74", Code = "line-quota:read", Name = "推播用量查詢", Module = "LINE 整合", Description = "查看 LINE Messaging API 月度推播用量（Dashboard 顯示）" },
            // 預審申請
            new Permission { Id = "75", Code = "pre-review-requests:read",   Name = "瀏覽",   Module = "預審申請", Description = "預審申請" },
            new Permission { Id = "76", Code = "pre-review-requests:write",  Name = "新增/修改", Module = "預審申請", Description = "預審申請" },
            new Permission { Id = "77", Code = "pre-review-requests:delete", Name = "刪除",   Module = "預審申請", Description = "預審申請" }
        );
    }
}
