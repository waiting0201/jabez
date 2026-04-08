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
        services.AddHttpClient<ILineService, LineService>();

        // ── 簽核通知服務（Scoped，依賴 AppDbContext）──────────────────────
        services.AddScoped<IApprovalNotificationService, ApprovalNotificationService>();

        // ── 簽核流程輔助服務 ────────────────────────────────────────────────
        services.AddScoped<IApprovalFlowService, ApprovalFlowService>();
        services.AddScoped<IEscalationService, EscalationService>();

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
}

await host.RunAsync();
