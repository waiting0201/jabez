using Jabez.Api.Data;
using Jabez.Api.Handlers;
using Jabez.Api.Middleware;
using Jabez.Api.Routing;
using Jabez.Api.Services;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Data;
using System.Text.Json;

var host = new HostBuilder()
    // ─── ASP.NET Core Integration (ConfigureFunctionsWebApplication) ─────────
    .ConfigureFunctionsWebApplication(worker =>
    {
        // Worker-level middleware：包住整個 Function 執行，捕捉所有例外
        worker.UseMiddleware<ExceptionMiddleware>();
    })
    // ─── DI Services ──────────────────────────────────────────────────────────
    .ConfigureServices((ctx, services) =>
    {
        var cfg = ctx.Configuration;

        // ── 全域 JSON 序列化：camelCase（前端 TypeScript 慣例）────────────
        // IActionResult 回應序列化（OkObjectResult 等）
        services.Configure<JsonOptions>(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy        = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        });
        // ReadFromJsonAsync 反序列化
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy        = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.PropertyNameCaseInsensitive = true;
        });

        // ConnectionStrings 在 local.settings.json 以 ConnectionStrings:Key 形式存取
        var connStr = cfg["ConnectionStrings:DefaultConnection"]
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");

        // ── EF Core + SQL Server ───────────────────────────────────────────
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlServer(connStr, sql =>
            {
                sql.EnableRetryOnFailure(3);
                sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            }));

        // ── Dapper IDbConnection（與 EF Core 共用同一 connection string）──
        services.AddScoped<IDbConnection>(_ => new SqlConnection(connStr));

        // ── JWT Service（Singleton — 只讀設定，可安全共用）───────────────
        services.AddSingleton<IJwtService, JwtService>();

        // ── Blob Storage Service ─────────────────────────────────────────
        services.AddSingleton<IBlobStorageService, BlobStorageService>();

        // ── Email Service（SMTP）──────────────────────────────────────────
        services.AddSingleton<IEmailService, EmailService>();

        // ── LINE Service（HttpClient 注入）───────────────────────────────
        // 顯式 10s timeout：LINE API 對單 request 通常 < 1s，
        // 若異常則寧可快速失敗也不要拖延整批推播（預設 100s 會讓 50 人推播在最壞情況下耗 5,000s）。
        services.AddHttpClient<ILineService, LineService>(c =>
        {
            c.Timeout = TimeSpan.FromSeconds(10);
        });

        // ── 簽核通知服務（Scoped，依賴 AppDbContext）──────────────────────
        services.AddScoped<IApprovalNotificationService, ApprovalNotificationService>();

        // ── 打卡提醒服務（Timer Trigger 排程使用）──────────────────────────
        services.AddScoped<IAttendanceReminderService, AttendanceReminderService>();

        // ── 簽核流程輔助服務 ────────────────────────────────────────────────
        services.AddScoped<IApprovalFlowService, ApprovalFlowService>();
        services.AddScoped<IEscalationService, EscalationService>();

        // ── 專案可見性解析（依使用者部門 + 財務體系 + Superadmin 判定）─────
        services.AddScoped<IProjectAccessResolver, ProjectAccessResolver>();
        services.AddHttpContextAccessor();

        // ── Dapper 讀取服務（Scoped，依賴 IDbConnection）─────────────────
        services.AddScoped<IUserReadService, UserReadService>();
        services.AddScoped<IRoleReadService, RoleReadService>();
        services.AddScoped<IDepartmentReadService, DepartmentReadService>();
        services.AddScoped<IJobTitleReadService, JobTitleReadService>();
        services.AddScoped<IApprovalReadService, ApprovalReadService>();
        services.AddScoped<IProjectReadService, ProjectReadService>();
        services.AddScoped<IPaymentRequestReadService, PaymentRequestReadService>();
        services.AddScoped<ILeaveRequestReadService, LeaveRequestReadService>();
        services.AddScoped<ITravelRequestReadService, TravelRequestReadService>();
        services.AddScoped<IOvertimeRequestReadService, OvertimeRequestReadService>();
        services.AddScoped<IAttendanceReadService, AttendanceReadService>();
        services.AddScoped<IAttendanceReminderReadService, AttendanceReminderReadService>();
        services.AddScoped<IPermissionReadService, PermissionReadService>();
        services.AddScoped<IInsuranceBracketReadService, InsuranceBracketReadService>();
        services.AddScoped<IPayrollReadService, PayrollReadService>();
        services.AddScoped<IAdvanceRequestReadService, AdvanceRequestReadService>();
        services.AddScoped<IWriteOffRequestReadService, WriteOffRequestReadService>();
        services.AddScoped<ITravelWriteOffRequestReadService, TravelWriteOffRequestReadService>();
        services.AddScoped<IOvertimeReportReadService, OvertimeReportReadService>();
        services.AddScoped<IPaymentReportReadService, PaymentReportReadService>();
        services.AddScoped<IProjectWaterLevelReadService, ProjectWaterLevelReadService>();
        services.AddScoped<ICalendarDayReadService, CalendarDayReadService>();
        services.AddScoped<ITravelPaymentRequestReadService, TravelPaymentRequestReadService>();
        services.AddScoped<IAttendanceReminderLogReadService, AttendanceReminderLogReadService>();
        services.AddScoped<IEmployeeProfileReadService, EmployeeProfileReadService>();

        // ── Handlers（Scoped，依賴 DbContext）────────────────────────────
        services.AddScoped<AuthHandler>();
        services.AddScoped<UserHandler>();
        services.AddScoped<RoleHandler>();
        services.AddScoped<PermissionHandler>();
        services.AddScoped<SettingsHandler>();
        services.AddScoped<HealthHandler>();
        services.AddScoped<DepartmentHandler>();
        services.AddScoped<JobTitleHandler>();
        services.AddScoped<ApprovalHandler>();
        services.AddScoped<ProjectHandler>();
        services.AddScoped<PaymentRequestHandler>();
        services.AddScoped<LeaveRequestHandler>();
        services.AddScoped<TravelRequestHandler>();
        services.AddScoped<ApprovalTaskHandler>();
        services.AddScoped<OvertimeRequestHandler>();
        services.AddScoped<AttendanceHandler>();
        services.AddScoped<InsuranceBracketHandler>();
        services.AddScoped<PayrollHandler>();
        services.AddScoped<OvertimeReportHandler>();
        services.AddScoped<PaymentReportHandler>();
        services.AddScoped<ProjectWaterLevelHandler>();
        services.AddScoped<InvoiceOcrHandler>();
        services.AddScoped<AdvanceRequestHandler>();
        services.AddScoped<WriteOffRequestHandler>();
        services.AddScoped<TravelWriteOffRequestHandler>();
        services.AddScoped<CalendarDayHandler>();
        services.AddScoped<FileHandler>();
        services.AddScoped<LineHandler>();
        services.AddScoped<TravelPaymentRequestHandler>();
        services.AddScoped<AttendanceReminderAdminHandler>();
        services.AddScoped<AttendanceReminderLogHandler>();
        services.AddScoped<EmployeeProfileHandler>();

        // ── Router（Scoped）──────────────────────────────────────────────
        services.AddScoped<AppRouter>();
    })
    .Build();

// ─── 啟動前自動執行 EF Core Migration ────────────────────────────────────────
// 首次執行：建立 JabezDb 資料庫 + Schema + Seed Data
// 後續執行：套用 pending migrations（若有）
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    // 一次性清理：CleanupHolidayActivityItems migration 將待刪 Blob URL 寫入 __HolidayBlobCleanup；
    // 此處讀取並呼叫 BlobStorageService 刪除實際檔案，完成後 DROP 暫存表。
    // 包 try/catch：清理失敗不擋 Function 啟動，下次啟動會自動重試。
    try
    {
        await HolidayBlobCleanup.RunAsync(db, scope.ServiceProvider.GetRequiredService<IBlobStorageService>());
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[HolidayBlobCleanup] skipped due to error: {ex.Message}");
    }
}

await host.RunAsync();

// 一次性清理工具：清除假日執行活動的 TravelRequestItems 與對應 Blob
// Migration 只建立 __HolidayBlobCleanup 旗標表，實際清理由此工具執行，單步失敗不阻擋 Functions 啟動
static class HolidayBlobCleanup
{
    public static async Task RunAsync(AppDbContext db, IBlobStorageService blob)
    {
        const string containerName = "invoices";

        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();

        // 旗標表不存在 → 代表已清理完成，直接跳過
        using (var checkCmd = conn.CreateCommand())
        {
            checkCmd.CommandText = "IF OBJECT_ID('dbo.__HolidayBlobCleanup', 'U') IS NULL SELECT 0 ELSE SELECT 1";
            var exists = (int)(await checkCmd.ExecuteScalarAsync() ?? 0);
            if (exists == 0) return;
        }

        // 1. 收集假日活動所有 Items 的 FileUrl（供 Blob 刪除）
        var urls = new List<string>();
        try
        {
            using var readCmd = conn.CreateCommand();
            readCmd.CommandText = """
                SELECT FileUrl
                FROM TravelRequestItems
                WHERE FileUrl IS NOT NULL AND FileUrl <> ''
                  AND TravelRequestId IN (SELECT Id FROM TravelRequests WHERE IsHolidayTravel = 1)
                """;
            using var reader = await readCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var url = reader.GetString(0);
                if (!string.IsNullOrEmpty(url)) urls.Add(url);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[HolidayBlobCleanup] collect FileUrl failed: {ex.Message}");
        }

        // 2. 刪除假日活動的 Items
        try
        {
            using var delCmd = conn.CreateCommand();
            delCmd.CommandText = """
                DELETE FROM TravelRequestItems
                WHERE TravelRequestId IN (SELECT Id FROM TravelRequests WHERE IsHolidayTravel = 1);
                """;
            await delCmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[HolidayBlobCleanup] delete items failed: {ex.Message}");
            return; // Items 沒刪除就別刪 Blob，保留 __HolidayBlobCleanup 下次重試
        }

        // 3. 假日活動 GrandTotal 歸零
        try
        {
            using var updCmd = conn.CreateCommand();
            updCmd.CommandText = "UPDATE TravelRequests SET GrandTotal = 0 WHERE IsHolidayTravel = 1;";
            await updCmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[HolidayBlobCleanup] reset grand total failed: {ex.Message}");
        }

        // 4. 刪除 Blob
        foreach (var url in urls.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var blobName = blob.ExtractBlobName(url, containerName);
                if (blobName is not null)
                    await blob.DeleteAsync(containerName, blobName);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[HolidayBlobCleanup] delete blob '{url}' failed: {ex.Message}");
            }
        }

        // 5. 完成後移除旗標表
        try
        {
            using var dropCmd = conn.CreateCommand();
            dropCmd.CommandText = "DROP TABLE dbo.__HolidayBlobCleanup";
            await dropCmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[HolidayBlobCleanup] drop marker table failed: {ex.Message}");
        }
    }
}
