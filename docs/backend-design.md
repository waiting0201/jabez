# Jabez 後端設計規範

本文件彙整 Jabez API（Azure Functions .NET 9）的技術架構與寫作規範。**新增功能或修改後端前，必須先讀本文件確認 Handler / DTO / ReadService / Router / Migration 等規範**；與本文件衝突時以本文件為準（CLAUDE.md 同步引用本文件）。

> 業務邏輯（簽核流程、請假規則、薪資公式、部門可見性、LINE / 打卡提醒等）仍記載於 [CLAUDE.md](../CLAUDE.md)；本文件**只規範技術層面**。

---

## 1. 技術棧

| 項目 | 規格 | 備註 |
|---|---|---|
| 平台 | Azure Functions v4 — **Isolated Worker Model** | 非 In-Process |
| 框架 | .NET 9 | C# 12 List Pattern 廣泛使用 |
| ORM | EF Core（寫入 + Migration）+ Dapper（讀取） | 二選一規則見 §6 |
| 資料庫 | SQL Server | 本地 `JabezDb`（連線字串於 [Api/local.settings.json](../Api/local.settings.json)） |
| 認證 | JWT Bearer Token (HS256) | 由 [JwtService.cs](../Api/Services/JwtService.cs) 簽發 |
| 路由 | 單一入口 RouterFunction → AppRouter | C# 12 List Pattern dispatch |
| Blob | Azure Storage（本地 Azurite） | 容器：`avatars` / `signatures` / `indigenous-proofs` / `low-income-proofs` / `disabled-proofs` / `id-cards` / `education-proofs` / `invoices` / `vendor-passbooks` / `request-attachments` |
| LINE | Messaging API + Login API | 簽核通知 + 打卡提醒 |
| Email | Microsoft Graph API | 簽核通知 / 帳號通知 / 薪資明細 |
| 例外處理 | `Middleware/ExceptionMiddleware.cs` | 統一捕捉 `AppException` 與未預期例外，回 `ApiResponse<T>` |

> **禁止引入**：In-Process Function Model、自訂 IoC 容器、Repository Pattern（讀取走 Dapper、寫入走 EF Core，無 Repository 抽象層）。

---

## 2. 目錄結構

```
Api/
├── Functions/
│   ├── RouterFunction.cs              # HttpTrigger，catch-all route {*route}
│   └── AttendanceReminderFunction.cs  # TimerTrigger
├── Routing/
│   └── AppRouter.cs                   # C# 12 List Pattern 路由分派器
├── Handlers/                          # 業務邏輯入口（HTTP → ApiResponse）
│   └── <Module>Handler.cs
├── Middleware/
│   └── ExceptionMiddleware.cs         # 全域例外處理
├── Data/
│   ├── AppDbContext.cs                # EF Core DbContext（含 Migration 自動套用）
│   ├── AppDbContextFactory.cs         # 用於 CLI Migration
│   ├── Configurations/                # EF Core 實體對應設定
│   └── Migrations/                    # EF Core Migration 檔案
├── Models/
│   ├── Entities/                      # DB entity（PascalCase）
│   └── Dtos/                          # 請求 / 回應 DTO（PascalCase）
├── Services/
│   ├── *Service.cs                    # 業務協調 service（JwtService / EscalationService / LineService / AttendanceReminderService …）
│   └── Dapper/                        # 讀取專用 Dapper ReadService
│       ├── I<Module>ReadService.cs
│       └── <Module>ReadService.cs
├── Common/
│   ├── ApiResponse.cs                 # 統一回應格式 ApiResponse<T>
│   ├── AppException.cs                # 自定義例外
│   ├── Clock.cs                       # 統一時間源（Asia/Taipei）
│   └── Constants.cs                   # 常數（部門 code / 權限 sentinel 等）
├── host.json
├── local.settings.json                # 本地開發設定（不進版控）
└── Api.csproj
```

### 2.1 三層責任分工（嚴格）

| 層 | 責任 | 路徑 |
|---|---|---|
| Handler | HTTP 解析 / 權限驗證 / 業務協調 / ApiResponse 包裝 | `Handlers/` |
| Service | 跨 Handler 共用的業務協調（升級審核、JWT、LINE、Email…） | `Services/` |
| ReadService（Dapper） | 純讀取 SQL 查詢 + DTO 投影 | `Services/Dapper/` |
| EF Core（DbContext） | 寫入、Transaction、Schema migration | `Data/AppDbContext.cs` |

**鐵律**：
- Handler 內 **禁止直接寫 SQL** — 讀取走 Dapper ReadService、寫入走 `AppDbContext`
- ReadService **禁止做寫入**（INSERT / UPDATE / DELETE 一律走 EF Core）
- Service **禁止直接回傳 HTTP** — Handler 才呼叫 `ApiResponse<T>.Ok(...)`

---

## 3. 路由分派設計

### 3.1 單一入口

所有 HTTP 請求由 [Functions/RouterFunction.cs](../Api/Functions/RouterFunction.cs) 接收（catch-all `{*route}`），再交給 [Routing/AppRouter.cs](../Api/Routing/AppRouter.cs) 用 **C# 12 List Pattern** 分派：

```csharp
[Function("RouterFunction")]
public async Task<HttpResponseData> Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "patch", "delete",
                 Route = "{*route}")] HttpRequestData req,
    string route,
    FunctionContext context)
{
    // → AppRouter.RouteAsync(...)
}
```

### 3.2 List Pattern dispatch

```csharp
return (method, segments) switch
{
    ("GET",    ["health"])                                   => healthHandler.HealthCheckAsync(req),
    ("POST",   ["auth", "login"])                            => authHandler.LoginAsync(req),
    ("GET",    ["users"])                                    => userHandler.GetAllAsync(req, ctx),
    ("GET",    ["users", "lookup"])                          => userHandler.GetLookupAsync(req, ctx),
    ("GET",    ["users", var id])                            => userHandler.GetByIdAsync(req, id, ctx),
    ("GET",    ["users", var id, "profile"])                 => profileHandler.GetByUserIdAsync(req, id, ctx),
    ("PUT",    ["users", var id, "profile"])                 => profileHandler.UpsertAsync(req, id, ctx),
    // ...
    _ => NotFound(req),
};
```

### 3.3 路由次序規則（重要）

dispatch 表與 permission 表都採 **「具體路徑優先於 catch-all」** 規則：

```csharp
// ✅ 正確
("GET", ["users", "lookup"])         => ...     // 先匹配
("GET", ["users", var id])           => ...     // 後匹配 catch-all

// ❌ 錯誤：lookup 永遠匹配不到，會被 catch-all 攔截
("GET", ["users", var id])           => ...
("GET", ["users", "lookup"])         => ...
```

### 3.4 權限表

`AppRouter` 同步維護一份 permission table，每條路由對應一個權限 code（或 `null` = 公開、`"superadmin"` = 僅 Superadmin）：

```csharp
private static string? RequiredPermission((string method, string[] segments) route) => route switch
{
    ("GET",    ["users"])                       => "users:read",
    ("POST",   ["users"])                       => "users:write",
    ("GET",    ["users", "lookup"])             => null,            // 輕量端點：免特定權限（仍需 JWT）
    ("GET",    ["files", "avatars", _])         => null,            // 公開
    ("GET",    ["files", "id-cards", _])        => "users:read",    // HR 敏感 PII
    ("GET",    ["roles"])                       => "superadmin",
    // ...
};
```

JWT 驗證 + 權限檢查由 RouterFunction → AppRouter 統一執行；Handler 內**禁止重複檢查**。

### 3.5 公開路由（不需 JWT）

| Method | Path | 說明 |
|---|---|---|
| GET | `/health` | 健康檢查 |
| POST | `/auth/login` | 登入取得 JWT |
| POST | `/auth/refresh` | 刷新 Token |
| GET | `/files/signatures/{fileName}` | 簽名檔代理（PDF 匯出用） |
| GET | `/files/avatars/{fileName}` | 頭像代理（topbar 顯示用） |

---

## 4. Handler 設計

### 4.1 命名與方法簽名

Handler class 命名：`<Module>Handler.cs`（PascalCase），對應一組相關業務動作。

標準方法名：

| 方法 | 用途 | HTTP |
|---|---|---|
| `GetListAsync` / `GetAllAsync` | 列表 / 分頁 | GET `/<resource>` |
| `GetByIdAsync` | 單筆查詢 | GET `/<resource>/{id}` |
| `GetLookupAsync` | 輕量下拉資料 | GET `/<resource>/lookup` |
| `CreateAsync` | 新增 | POST `/<resource>` |
| `UpdateAsync` | 更新 | PUT/PATCH `/<resource>/{id}` |
| `DeleteAsync` | 刪除 | DELETE `/<resource>/{id}` |
| `SubmitAsync` | 申請類送出（draft → pending） | PATCH `/<resource>/{id}/submit` |
| `<Custom>Async` | 業務動作（如 `BatchApproveAsync` / `UpsertAsync`） | 視 API 設計而定 |

### 4.2 標準方法骨架

```csharp
public async Task<HttpResponseData> GetByIdAsync(HttpRequestData req, string id, FunctionContext ctx)
{
    if (!Guid.TryParse(id, out var guid))
        throw AppException.BadRequest("無效的 ID 格式");

    var data = await readService.GetByIdAsync(guid)
               ?? throw AppException.NotFound("找不到指定資源");

    return await req.OkAsync(ApiResponse<MyDto>.Ok(data));
}

public async Task<HttpResponseData> CreateAsync(HttpRequestData req, FunctionContext ctx)
{
    var dto = await req.ReadFromJsonAsync<CreateMyRequest>()
              ?? throw AppException.BadRequest("請求 body 不可為空");

    // 驗證
    if (string.IsNullOrWhiteSpace(dto.Name))
        throw AppException.BadRequest("名稱必填");

    // 寫入（EF Core）
    var entity = new MyEntity { Name = dto.Name, CreatedAt = Clock.Now };
    db.MyEntities.Add(entity);
    await db.SaveChangesAsync();

    // 回讀（Dapper，避免 EF Core navigation 重新查 DB）
    var created = await readService.GetByIdAsync(entity.Id);
    return await req.OkAsync(ApiResponse<MyDto>.Ok(created!));
}
```

### 4.3 Multipart 處理

當 API 接受檔案上傳時（multipart/form-data）：

```csharp
public async Task<HttpResponseData> UpsertAsync(HttpRequestData req, string id, FunctionContext ctx)
{
    if (!Guid.TryParse(id, out var userId))
        throw AppException.BadRequest("無效的使用者 ID");

    var form = await MultipartParser.ParseAsync(req); // helper
    var payload = form.GetText("payload") ?? throw AppException.BadRequest("payload 必填");
    var dto = JsonSerializer.Deserialize<MyUpsertRequest>(payload, JsonOptions);

    var idCardFront = form.GetFile("idCardFront");
    var removeIdCardFront = form.GetBool("removeIdCardFront");

    if (idCardFront is { Length: > 1024 * 1024 })
        throw AppException.BadRequest("上傳照片勿超過1MB");

    // ... 寫入邏輯
}
```

> 標準參考：[UserHandler.cs](../Api/Handlers/UserHandler.cs) `CreateAsync` / `UpdateAsync`、[EmployeeProfileHandler.cs](../Api/Handlers/EmployeeProfileHandler.cs) `UpsertAsync`。

### 4.4 例外處理

**禁止** 自行 `throw new Exception(...)`。使用 [AppException.cs](../Api/Common/AppException.cs)：

| 方法 | HTTP status | 用途 |
|---|---|---|
| `AppException.BadRequest(msg)` | 400 | 請求格式 / 驗證錯誤 |
| `AppException.Unauthorized(msg)` | 401 | 未登入 / Token 失效 |
| `AppException.Forbidden(msg)` | 403 | 權限不足 |
| `AppException.NotFound(msg)` | 404 | 資源不存在 |
| `AppException.Conflict(msg)` | 409 | 衝突（如重複申請、版本不符） |

`ExceptionMiddleware.cs` 自動捕捉並轉成 `ApiResponse<T>.Fail(message)`；未預期例外回 500 + 通用訊息。

---

## 5. DTO 設計

### 5.1 位置與命名

- **位置**：`Models/Dtos/<Module>Dtos.cs`（一個 module 一個檔案，**禁止散落於 Handler 內**）
- **命名**：
  - `<Entity>Dto` — 完整回應
  - `<Entity>SummaryDto` / `<Entity>LookupDto` — 輕量版（少欄位）
  - `Create<Entity>Request` / `Update<Entity>Request` — 寫入請求
  - `<Entity>DetailDto` — 含子表的詳情

### 5.2 序列化

camelCase JSON（System.Text.Json 預設，於 `Program.cs` 設定）：

```csharp
.ConfigureFunctionsWebApplication()
.ConfigureServices(services =>
{
    services.Configure<JsonSerializerOptions>(o =>
    {
        o.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
})
```

### 5.3 範例

```csharp
public record UserDto(
    Guid Id,
    string Name,
    string Email,
    int? DepartmentId,
    string? DepartmentName,
    int? JobTitleId,
    string? JobTitleName,
    decimal? BaseSalary,
    bool IsLowIncome,
    string? LowIncomeProofUrl,
    DateTime CreatedAt
);

public record CreateUserRequest(
    string Name,
    string Email,
    string Password,
    int? DepartmentId,
    int? JobTitleId,
    DateTime? Birthday
);
```

---

## 6. Dapper vs EF Core 使用原則

| 情境 | 使用 |
|---|---|
| 列表查詢、多表 JOIN、效能敏感 | **Dapper**（`Services/Dapper/<Module>ReadService.cs`） |
| 單筆查詢（含子表） | Dapper（`QueryMultipleAsync` 一次拉多表）|
| CRUD 操作、資料異動、Transaction | **EF Core**（`AppDbContext`） |
| Schema 管理（建表、Migration） | **EF Core Migration** |
| 整批替換子表 | EF Core `ExecuteDeleteAsync` + `AddRangeAsync`，包在 transaction 內 |

### 6.1 Dapper ReadService 寫法

```csharp
public interface IUserReadService
{
    Task<UserDto?> GetByIdAsync(Guid id);
    Task<List<UserDto>> GetAllAsync(int page, int pageSize);
}

public class UserReadService(IDbConnectionFactory connFactory) : IUserReadService
{
    private const string BaseSelect = """
        SELECT u.Id, u.Name, u.Email,
               u.DepartmentId, d.Name AS DepartmentName,
               u.JobTitleId, j.Name AS JobTitleName,
               u.BaseSalary, u.IsLowIncome, u.LowIncomeProofUrl,
               u.CreatedAt
        FROM Users u
        LEFT JOIN Departments d ON u.DepartmentId = d.Id
        LEFT JOIN JobTitles  j ON u.JobTitleId   = j.Id
        WHERE u.IsSuperAdmin = 0
        """;

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        using var conn = connFactory.Create();
        return await conn.QueryFirstOrDefaultAsync<UserDto>(
            $"{BaseSelect} AND u.Id = @id", new { id });
    }
}
```

**規範**：
- SQL 全部用 raw string literal（`"""..."""`）並維持縮排可讀
- 參數化查詢（`@param`）— **禁止字串拼接 SQL**
- 同一 module 共用 `BaseSelect` 常數避免漂移
- 多表合併查 → `QueryMultipleAsync` 配合 `ReadAsync<T>()`

### 6.2 EF Core 寫入

```csharp
// 簡單寫入
db.Users.Add(user);
await db.SaveChangesAsync();

// Transaction（多 entity 一致性）
// AppDbContext 啟用 EnableRetryOnFailure，直接 BeginTransactionAsync() 會被
// SqlServerRetryingExecutionStrategy 阻擋；必須用 CreateExecutionStrategy 包裝整批操作。
var strategy = db.Database.CreateExecutionStrategy();
await strategy.ExecuteAsync(async () =>
{
    await using var tx = await db.Database.BeginTransactionAsync();
    await db.SalaryAdjustmentRecords.Where(s => s.UserId == userId).ExecuteDeleteAsync();
    await db.SalaryAdjustmentRecords.AddRangeAsync(newRecords);
    await db.SaveChangesAsync();
    await tx.CommitAsync();
    // await using：未 Commit 即離開 scope（含例外）會自動 Rollback。
});
```

### 6.3 跨流程共用寫入核心（不 SaveChanges 的 helper）

當同一段寫入邏輯被多個入口共用、且需與呼叫端的其他寫入**同交易**時，把「validate + diff + 套用變更」抽成 static helper，**內部只 `db.Add/Remove` 與改 tracked entity，不呼叫 `SaveChangesAsync`**，交易邊界交由呼叫端決定。

範例：[InstallmentUpsertService.Apply](../Api/Services/InstallmentUpsertService.cs)（分期撥款）— 4 個獨立 `PATCH /{type}-requests/{id}/installments` endpoint 與 `ApprovalTaskHandler` 的「財務核准當下原子寫入撥款明細」共用同一份持久化邏輯。4 種子表透過 [IInstallmentEntity](../Api/Models/Entities/IInstallmentEntity.cs) 介面 + `Func<TEntity>` create factory 泛型化（FK 由 factory 設定，其餘欄位由 helper 填）。

---

## 7. EF Core Configuration

### 7.1 位置

每個 entity 對應一個 `Data/Configurations/<Entity>Configuration.cs`，由 `AppDbContext.OnModelCreating` 透過 `ApplyConfigurationsFromAssembly` 自動載入。

### 7.2 寫法

```csharp
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(200).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.BaseSalary).HasColumnType("decimal(18,2)");
        builder.Property(u => u.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(u => u.Department)
               .WithMany()
               .HasForeignKey(u => u.DepartmentId)
               .OnDelete(DeleteBehavior.SetNull);

        // Seed data
        builder.HasData(new User { /* superadmin */ });
    }
}
```

### 7.3 1:1 關聯（PK = FK）

當子表是父表的延伸（如 `EmployeeProfile` 1:1 對 `User`）：

```csharp
builder.HasKey(p => p.UserId);
builder.HasOne<User>()
       .WithOne()
       .HasForeignKey<EmployeeProfile>(p => p.UserId)
       .OnDelete(DeleteBehavior.Cascade);
```

### 7.4 1:N 子表

```csharp
builder.HasKey(e => e.Id);
builder.Property(e => e.UserId).IsRequired();
builder.HasOne<User>()
       .WithMany()
       .HasForeignKey(e => e.UserId)
       .OnDelete(DeleteBehavior.Cascade);
builder.HasIndex(e => new { e.UserId, e.Order });  // 子表常需依 UserId 撈
```

### 7.5 刪除主檔時的 NO_ACTION 外鍵清洗

刪除 `User`（或其他被多表引用的主檔）時，指向它的外鍵分三類：

| delete 行為 | 處理 | 範例 |
|---|---|---|
| `Cascade` | DB 自動連帶刪除 | EmployeeProfile + 子表 / AttendanceRecords / UserRoles |
| `SetNull` | DB 自動設 NULL | 各申請單 `SubmittedById` / `EmployeeId` |
| `NoAction` | **會擋住刪除，須在 Handler 手動清洗** | 審核 / 撥款 / 代理 / 提醒 log 等欄位 |

`NoAction` 外鍵的清洗原則（見 [UserHandler.DeleteAsync](../Api/Handlers/UserHandler.cs)，用 `ExecuteDeleteAsync` / `ExecuteUpdateAsync` 包 `BeginTransactionAsync`）：
- **列的主體即被刪主檔**（如 `RequestDesignatedReviewers.ReviewerId`、不可為 NULL 的欄位）→ `ExecuteDeleteAsync` 刪列
- **列屬於其他單據、僅將主檔列為審核者 / 撥款者 / 代理人**（可為 NULL）→ `ExecuteUpdateAsync(SetProperty(..., (Guid?)null))` 設 NULL，保留單據本體

> 用 `sys.foreign_keys` 查 `delete_referential_action_desc` 可列出全部引用。**新增任何指向某主檔的 `NoAction` 外鍵時，必須同步補進該主檔 Delete 的清洗清單**，否則日後刪不掉主檔。
>
> 多型關聯（如 `RequestDesignatedReviewer` / `ApprovalRecord` 用 `RequestType+RequestId` 對應 9 種申請父表）**沒有真 FK**，刪父表時 EF Cascade 不處理，須在各申請 Handler 的 `DeleteAsync` 手動 `RemoveRange`。

---

## 8. Migration

### 8.1 建立

```bash
cd /Users/tim/webapps/Jabez/Api
dotnet ef migrations add <DescriptiveName>
```

命名：`yyyyMMddHHmmss_<DescriptiveName>.cs`（自動加 timestamp）。

### 8.2 規範

- **禁止修改既有 Migration** 檔案（已套用至生產 / 共享 dev DB 後）
- 遇到「想改錯誤的 Migration」→ 加新 Migration 修正，不回頭改舊的
- `AppDbContext` 啟動時自動執行 `Database.MigrateAsync()`，無須手動 `dotnet ef database update`（本地開發例外）
- Seed data 寫在 `<Entity>Configuration.cs` 的 `builder.HasData(...)`，不要寫在 Migration 內

### 8.3 Schema 變更同步

```
新增 entity → 加入 Configuration → AppDbContext.cs 加 DbSet
        → dotnet ef migrations add <Name> → 啟動 API 自動套用
```

---

## 9. JWT 認證

### 9.1 規格

| 項目 | 值 |
|---|---|
| 演算法 | HS256 |
| Issuer | `jabez-api` |
| Audience | `jabez-admin` |
| Access Token TTL | 60 分鐘 |
| Refresh Token TTL | 7 天 |

### 9.2 Claims

| Claim | 用途 |
|---|---|
| `sub` | User Id (Guid) |
| `name` | 使用者名稱 |
| `email` | Email |
| `jti` | JWT ID（每次發新 token 改變） |
| `roles` | 角色名稱陣列 |
| `permissions` | 權限 code 陣列（Superadmin 為全部 DB 權限） |
| `is_superadmin` | bool |
| `department_name`、`department_code` | 部門 |
| `job_title_name`、`job_title_level` | 職稱 |
| `avatar` | 頭像 URL |

### 9.3 環境變數（雙底線慣例）

Azure Functions config 用雙底線取代冒號：

```
Jwt__Secret           ↔ IConfiguration["Jwt:Secret"]
Jwt__Issuer           ↔ IConfiguration["Jwt:Issuer"]
Jwt__Audience         ↔ IConfiguration["Jwt:Audience"]
Jwt__ExpiryMinutes    ↔ IConfiguration["Jwt:ExpiryMinutes"]
Jwt__RefreshExpiryDays ↔ IConfiguration["Jwt:RefreshExpiryDays"]
```

### 9.4 密碼

- 雜湊用 **BCrypt**（[BCrypt.Net-Next](https://github.com/BcryptNet/bcrypt.net) NuGet）
- 新增 / 重設密碼 → `BCrypt.HashPassword(plain)`
- 登入驗證 → `BCrypt.Verify(plain, hash)`
- 預設密碼為使用者出生日期 `yyyyMMdd`（首次登入應強制改密碼，`User.MustChangePassword`）

---

## 10. ApiResponse 統一回應

所有 API 回應格式為 `ApiResponse<T>`：

```csharp
public class ApiResponse<T>
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public T? Data { get; init; }

    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    {
        Success = true, Data = data, Message = message
    };

    public static ApiResponse<T> Fail(string message) => new()
    {
        Success = false, Message = message
    };
}
```

**規範**：
- Handler 一律 `return await req.OkAsync(ApiResponse<X>.Ok(...))` — **禁止直接 return data**
- 例外由 `ExceptionMiddleware` 自動轉成 `ApiResponse<object>.Fail(...)`
- 前端 [api-response.interceptor.ts](../Admin/src/app/core/auth/interceptors/api-response.interceptor.ts) 自動解包，service 直接拿到 `data`

---

## 11. 時區處理（重要）

### 11.1 統一時間源

**禁止** 在業務邏輯使用 `DateTime.Now` 或 `DateTime.UtcNow`。一律用 [Common/Clock.cs](../Api/Common/Clock.cs)：

```csharp
public static class Clock
{
    private static readonly TimeZoneInfo TaipeiTz =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");

    public static DateTime Now =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TaipeiTz);

    public static DateTime Today => Now.Date;
}
```

### 11.2 用法

```csharp
entity.CreatedAt = Clock.Now;
if (record.EffectiveDate <= Clock.Today) { /* 已生效 */ }
```

### 11.3 例外（可用 UTC 的場景）

- DB 預設值：`HasDefaultValueSql("GETUTCDATE()")` — DB 引擎層
- JWT 簽發 / 驗證：JWT 標準用 UTC（`DateTime.UtcNow`）
- Azure Functions Timer / cron：UTC（[AttendanceReminderFunction.cs](../Api/Functions/AttendanceReminderFunction.cs) 的 cron 設計於 UTC，內部再用 `Clock.Now` 比對）

---

## 12. 檔案上傳（multipart + Blob）

### 12.1 流程

1. Handler 接收 multipart/form-data
2. 解析 `IFormFile`：驗證 size（1 MB 上限）+ 副檔名 + 磁性 byte（防偽造）
3. 上傳至 Azure Blob Storage（容器分類見 §1）
4. 將 Blob URL 寫回 entity

### 12.2 Size 限制

| 檔案類型 | 上限 |
|---|---|
| 員工頭像 / 簽名 | 1 MB |
| 證明文件（原住民 / 低收入 / 殘障 / 身分證） | 1 MB |
| 發票圖檔 | 1 MB |

### 12.3 驗證

```csharp
const long MaxBytes = 1024 * 1024;
if (file.Length > MaxBytes)
    throw AppException.BadRequest("上傳照片勿超過1MB");

// 磁性 byte 驗證（防偽造副檔名）
var allowedSignatures = new Dictionary<string, byte[][]>
{
    ["image/jpeg"] = [[0xFF, 0xD8, 0xFF]],
    ["image/png"]  = [[0x89, 0x50, 0x4E, 0x47]],
    ["application/pdf"] = [[0x25, 0x50, 0x44, 0x46]],
};
```

### 12.4 Blob 容器分類

| 容器 | 內容 | 公開 / 授權 |
|---|---|---|
| `avatars` | 頭像 | **公開** `/files/avatars/{fileName}` |
| `signatures` | 簽名檔 | **公開** `/files/signatures/{fileName}` |
| `indigenous-proofs` | 原住民證明 | 授權 `users:read` |
| `low-income-proofs` | 低收入證明 | 授權 `users:read` |
| `disabled-proofs` | 殘障證明 | 授權 `users:read` |
| `id-cards` | 身分證影本 | 授權 `users:read` |
| `education-proofs` | 最高學歷證明 | 授權 `users:read` |
| `invoices` | 發票檔 | 授權 |
| `vendor-passbooks` | 廠商存摺封面 | **登入即可** `/files/vendor-passbooks/{fileName}`（一般檔，與 avatars/signatures 同層） |
| `request-attachments` | 整單批次附件（請款（廠商 / 一般）/ 預支沖銷的照片或 PDF） | 授權 |

### 12.5 條件式刪除

當開關欄位（如 `IsIndigenous`）由 `true → false` 時，必須**主動刪除** Blob：

```csharp
if (oldUser.IsIndigenous && !dto.IsIndigenous && !string.IsNullOrEmpty(oldUser.IndigenousProofUrl))
{
    await blobService.DeleteAsync("indigenous-proofs", oldUser.IndigenousProofUrl);
    oldUser.IndigenousProofUrl = null;
}
```

DELETE entity 時，相關 Blob 全部一起刪。

### 12.7 整單批次附件（共用 AttachmentProcessor）

請款（`PaymentRequest`，type=vendor 廠商請款 / type=general 一般請款皆可）與預支沖銷（`WriteOffRecord`）支援**整單層級**批次附件（照片 / PDF），存於專屬子表 `PaymentRequestAttachment` / `WriteOffAttachment`（真實 FK + Cascade delete），統一走 [`AttachmentProcessor`](../Api/Common/AttachmentProcessor.cs)：

- **multipart 欄位**（與明細列檔案的 `files` 區隔）：
  - `attachments`（JSON）：`AttachmentMetadata[] { FileName, FileUrl, FileIndex }`；既有檔保留 `FileUrl`，新檔以 `FileIndex` 對應檔案部分
  - `attachmentFiles`：實際檔案（順序與 `FileIndex` 一致）
- **驗證**：`AttachmentProcessor.ResolveAsync` 以 `FileSignatureValidator` magic-byte 驗證（允許 PNG/JPEG/GIF/WebP/HEIC/AVIF/PDF）、單檔 ≤ 10MB（前端已壓縮，此為安全網），上傳至 `request-attachments`
- **Create/Update/Delete**：Update 時 `attachments` 欄位缺席＝不更新，存在＝整組替換（保留既有 URL、上傳新檔、`RemoveRange` 舊列、比對刪除孤兒 blob）；請款兩種 type（vendor / general）皆帶附件；Delete 收集附件 blob 一併刪除
- **顯示**：`PaymentRequestReadService` / `WriteOffRequestReadService` 以**獨立查詢**（避免與明細 JOIN 笛卡兒相乘）回傳 `AttachmentDto[]`，填入 `PaymentRequestDto` / `WriteOffRequestDto` / `PaymentTaskDetailDto` / `WriteOffTaskDetailDto`

### 12.6 外部 API 整合（IHttpClientFactory + Service 注入）

對外部 REST API（如 LINE Messaging API、GCIS 政府開放資料、Azure Document Intelligence）一律走 typed-client 模式：

```csharp
// Program.cs
services.AddHttpClient<IGcisService, GcisService>(c =>
{
    c.BaseAddress = new Uri("https://data.gcis.nat.gov.tw/");
    c.Timeout     = TimeSpan.FromSeconds(8);   // 顯式 timeout，避免拖累 Function
});
```

```csharp
// GcisService.cs
public sealed class GcisService(HttpClient http, ILogger<GcisService> logger) : IGcisService
{
    public async Task<VendorTaxIdLookupResponse?> LookupByTaxIdAsync(string taxId, CancellationToken ct = default)
    {
        try
        {
            using var response = await http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return null;
            // 解析 JSON…
        }
        catch (TaskCanceledException) { /* timeout → null */ }
        catch (Exception ex) { logger.LogError(ex, "..."); return null; }
    }
}
```

設計原則：
- **顯式 timeout**：預設 100s 對 Function 太長，依 API 特性設 5–10s。
- **失敗回 null**：外部 API timeout / 5xx / 解析失敗一律回 null，由呼叫端 Handler 轉成 404 + toast，避免外部錯誤往上層擴散。
- **介面 + 實作分離**：`IGcisService` / `GcisService`，方便測試 mock。
- **不快取**：除非有明確業務需求，否則查詢即時（避免外部資料變更與本地快取不同步）。

---

## 13. 輕量讀取端點模式（Lightweight Lookup Pattern）

當「全體員工都會用到」的功能依賴「需後台管理權限的 CRUD 端點」時，會把後者的權限隱含地強加到前者上，造成一般員工功能異常。本專案以 **輕量讀取端點** 解決：對同一資源額外開一支 read-only、欄位精簡、**免特定權限**（仍需 JWT）的子端點。

### 13.1 已採用此模式的端點

| 輕量端點 | 對應的權限端點 | 用途 |
|---|---|---|
| `GET /users/lookup` | `GET /users`（需 `users:read`） | 申請表「指定審核者」、人員下拉 |
| `GET /projects/active` | `GET /projects`（需 `projects:read`） | 申請表「專案」下拉，僅回傳 `active` 狀態 |
| `GET /approval-items/active?type=<applicationType>` | `GET /approval-items`（需 `approvals:read`） | 申請表判斷流程是否含 `useApplicantDesignated` 步驟 |
| `GET /job-titles/lookup` | `GET /job-titles`（需 `job-titles:read`） | 申請表「指定審核者」職稱下拉 |
| `GET /vendors/lookup` | `GET /vendors`（需 `vendors:read`） | 請款表單「廠商」下拉，僅回 `IsActive=true` |
| `GET /vendors/lookup-by-tax-id?taxId=XXXXXXXX` | — | 以統編查 GCIS 公司登記資料，自動帶出廠商名稱 / 地址 / 負責人；任何登入者可用 |
| `POST /vendors` *(無需權限)* | — | 請款表單 quick-add modal：任何登入者皆可新建廠商，避免後台 CRUD 權限被強加給請款人 |
| `GET /files/signatures/{fileName}` / `/files/avatars/{fileName}` | — | 簽名檔 / 頭像 Blob 代理（公開路由） |
| `GET /me/user` | `GET /users/{id}`（需 `users:read`） | 「個人資訊」唯讀頁：員工查看自己的帳號資料（從 JWT `sub` 取自身 id） |
| `GET /me/profile` | `GET /users/{id}/profile`（需 `users:read`） | 「個人資訊」唯讀頁：員工查看自己的人事資料卡 + 健保眷屬 |
| `GET /me/files/{container}/{fileName}` | `GET /files/<PII container>/{fileName}`（需 `users:read`） | 「個人資訊」唯讀頁：員工讀自己的 PII 檔案，見下方 §13.4 |

> HR 敏感 PII（`/files/indigenous-proofs/`、`/files/low-income-proofs/`、`/files/disabled-proofs/`、`/files/id-cards/`、`/files/education-proofs/`）的**管理端**代理**不**走輕量模式，仍需 `users:read`；員工要讀**自己的** PII 改走 `/me/files/{container}/{fileName}`（§13.4）。

### 13.4 「自己讀自己」模式（Self / Me Endpoints）

當員工要查看**自己的**完整資料（含薪資、PII 檔案）時，不能放寬管理端權限，否則會洩漏他人資料。改採 **`me` 自助端點**：從 JWT `sub` claim 取當前 userId，只回傳 / 服務該 userId 的資料。

- **取得 userId**：Router 在驗證後已將 principal 寫入 `req.HttpContext.User`（[AppRouter.cs](../Api/Routing/AppRouter.cs) line ~87）；Handler 以 `req.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)` 取出，解析失敗回 401。複用既有 Read Service（如 `reader.GetByIdAsync(userId)` / `EmployeeProfileReadService.GetByUserIdAsync(userId)`），不需新 DTO / Migration。
- **PII 檔案自助代理** [`FileHandler.GetMineAsync`](../Api/Handlers/FileHandler.cs)：
  1. **白名單容器**：`SelfServiceContainers`（id-cards / education-proofs / 三種 proofs / avatars / signatures），不在白名單一律 404。
  2. **前綴檢查**：所有 blob 命名都以 `{userId}` 開頭（`{userId}{ext}` / `{userId}_front{ext}` / `{userId}_education{ext}`…），驗證 `fileName` 以自身 `userId` 開頭且後接 `.` 或 `_`，否則 403 — 防止員工竄改 `fileName` 讀他人檔案（GUID 定長 + 分隔符，無前綴包含風險）。
  3. 通過後複用既有私有 `GetFileAsync`（blob 串流 + Content-Type 驗證）。
- **權限對應**：`me` 路由在 `GetRequiredPermission` 無對應項，落到 `_ => null`（登入即可），且不在 `IsPublicRoute`（強制 JWT）。
- **前端對應**：`<img src>` 不能帶 Authorization header，故簽名 / 頭像（公開容器）走公開 `/files/...`；其餘 PII 改以 HttpClient 下載 blob（interceptor 帶 token）再 `URL.createObjectURL` 顯示 / 開新分頁。

### 13.2 設計原則

1. **欄位最小化**：只回傳前端真正需要的欄位，**不**回傳敏感配置（部門設定、角色權限、薪資等）
2. **read-only**：絕不接受 `POST` / `PUT` / `PATCH` / `DELETE`
3. **路由命名**：以子路徑（`/<resource>/active`、`/<resource>/lookup`）區隔
4. **路由次序**：dispatch table 中**輕量子路由必須在 `[var id]` catch-all 之前**
5. **DTO 獨立**：用獨立的 Summary DTO（如 `ApprovalFlowSummaryDto` ≠ `ApprovalItemDto`），避免主 DTO 新增欄位時意外洩漏

### 13.3 何時要新增？

- **症狀**：某前端表單對「沒有 X 管理權限」的使用者異常（欄位不顯示、下拉空、按鈕無效）
- **檢查**：F12 看 Network 是否有 `403 Forbidden`，且該 API 的 `requiredPermission` 對應到大多數員工不會持有的管理權限
- **修法**：[AppRouter.cs](../Api/Routing/AppRouter.cs) 加 `("GET", ["<resource>", "<sub>"]) => null` 路由 + Handler + Reader，前端改呼叫此端點

> **歷史教訓**（2026-05）：所有 9 種申請表單的「指定審核者」欄位都呼叫 `GET /approval-items`（需 `approvals:read`），導致無此權限的員工看不到欄位。最後以 `GET /approval-items/active?type=` 解決。Code Review 看到一般員工頁面呼叫 admin CRUD 端點，要立刻警覺。

---

## 14. 部門可見性（Project Access Scope）

員工看「資料相關報表」時，依使用者 ClaimsPrincipal 解析可見部門：

| 優先序 | 使用者類別 | 可見範圍 |
|---|---|---|
| 1 | Superadmin | 全部 |
| 2 | `Department.CanSeeAll = true` 部門成員 | 全部 |
| 3 | 一般員工 | 自己部門；可選擴展：`CanViewSiblings` / `CanViewDescendants` / `CanViewParent` |

實作於 [Api/Services/ProjectAccessResolver.cs](../Api/Services/ProjectAccessResolver.cs)，提供 `Task<ProjectAccessScope> ResolveAsync()`，回傳 `(SeeAll bool, AllowedDepartmentIds int[])`。

**使用方式**：
```csharp
var scope = await resolver.ResolveAsync();
var query = await readService.GetReportAsync(scope, dateFrom, dateTo);
```

ReadService 內依 scope 組 WHERE：
```sql
WHERE (@SeeAll = 1 OR u.DepartmentId IN @AllowedDeptIds)
```

完整業務套用清單見 [CLAUDE.md](../CLAUDE.md#部門可見性規則).

---

## 15. 命名規範

### 15.1 通用

| 對象 | 規則 | 範例 |
|---|---|---|
| C# class / method / property | PascalCase | `UserHandler`, `GetByIdAsync`, `IsActive` |
| C# private field | `_camelCase` 或 `camelCase`（隨檔案內既有風格） | `_db`, `db`, `connFactory` |
| C# const | PascalCase 或 SCREAMING_SNAKE | `MaxFileSize`, `JABEZ_HQ` |
| 檔名 | 與 class 同名 PascalCase | `UserHandler.cs` |
| DB 表名 | PascalCase 複數 | `Users`, `EmployeeProfiles` |
| DB 欄位 | PascalCase | `CreatedAt`, `IsLowIncome` |
| API path | kebab-case | `/payment-requests`, `/approval-items` |
| JSON property | camelCase | `userId`, `createdAt` |

### 15.2 模組命名

| 類型 | 模式 |
|---|---|
| Handler | `<Module>Handler.cs` |
| DTO 檔案 | `<Module>Dtos.cs` |
| Configuration | `<Entity>Configuration.cs` |
| ReadService | `<Module>ReadService.cs` + `I<Module>ReadService.cs` |
| Service | `<Module>Service.cs` + `I<Module>Service.cs`（功能 service 介面在前） |
| Migration | `yyyyMMddHHmmss_<Action>.cs` |

---

## 16. 環境變數慣例

### 16.1 雙底線（`__`）取代冒號

Azure Functions config 限制 key 不能含 `:`，改用 `__`：

```
Jwt__Secret                       ↔ IConfiguration["Jwt:Secret"]
ConnectionStrings__DefaultConnection ↔ IConfiguration.GetConnectionString("DefaultConnection")
Line__LoginChannelId              ↔ IConfiguration["Line:LoginChannelId"]
```

### 16.2 local.settings.json 結構

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "Jwt__Secret": "...",
    "Jwt__Issuer": "jabez-api",
    "Jwt__Audience": "jabez-admin"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=JabezDb;..."
  },
  "Host": {
    "LocalHttpPort": 7071,
    "CORS": "*",
    "CORSCredentials": false
  }
}
```

> `local.settings.json` **不進版控**（已加入 `.gitignore`）。

### 16.3 Production（Azure）

直接在 Function App → Configuration → Application Settings 設定，名稱與 `local.settings.json` 相同（雙底線格式）。

### 16.4 一次性 Seeder 工具（Startup Hook 模式）

需一次性灌資料時（如批次匯入既有員工人事卡），比照 `Program.cs` 既有 `HolidayBlobCleanup` 寫法：在 `host.MigrateAsync()` 後的 `using (scope)` 區塊內，以**環境旗標**保護呼叫一個 `static RunAsync(AppDbContext, IBlobStorageService, IConfiguration)` 工具（獨立 try/catch 不阻擋啟動），重用已注入的 DI（DbContext / Blob / BCrypt / Clock），免另開 console 專案。

- 範例：[Api/Data/Seed/EmployeeImporter.cs](../Api/Data/Seed/EmployeeImporter.cs)（讀 `employee-import.json` → User + EmployeeProfile + 子表 + 附件）。旗標 `RUN_EMPLOYEE_IMPORT=true` 觸發、`IMPORT_UPLOAD_FILES` 控制附件是否實際上 blob、`EMPLOYEE_IMPORT_SOURCE_DIR` 指來源夾。
- 因 DbContext 啟用 `EnableRetryOnFailure`，每筆須包 `CreateExecutionStrategy() + BeginTransactionAsync()`（同 §4 Handler 規範）。
- 去重採 **upsert**（Email 或唯一業務鍵命中即覆蓋 update-in-place、子表 `ExecuteDelete` 後重建），確保可重跑不重複。
- 民國/西元日期解析共用 [RocDateParser](../Api/Data/Seed/RocDateParser.cs)（年 ≤ 150 視民國 +1911）。
- 旗標預設 `false`；`local.settings.json` 不進版控、`CopyToPublishDirectory=Never`，旗標不會帶到 prod。

---

## 17. Coding Style Checklist（每次撰寫前自我檢查）

### 17.1 後端（.NET）

- [ ] Handler 是否符合既有 `<Module>Handler.cs` 的方法命名（`GetListAsync` / `GetByIdAsync` / `CreateAsync` / `UpdateAsync` / `DeleteAsync` / `SubmitAsync`）？
- [ ] 是否使用 `ApiResponse<T>.Ok(...)` / `ApiResponse<T>.Fail(...)` 回應，未直接 `return data`？
- [ ] 例外是否使用 `AppException.BadRequest` / `NotFound` / `Forbidden`，未自行 throw `Exception`？
- [ ] DTO 是否放在 `Models/Dtos/<Module>Dtos.cs` 而非散落於 Handler 內？
- [ ] 讀取查詢是否走 `Services/Dapper/<Module>ReadService.cs`，未在 Handler 直接寫 SQL？
- [ ] 寫入操作是否走 EF Core `AppDbContext`，未 mix Dapper INSERT/UPDATE？
- [ ] 是否所有 I/O 都 `async/await`，未出現 `.Result` / `.Wait()` / `.GetAwaiter().GetResult()`？
- [ ] 時間是否使用 `Clock.Now`（Asia/Taipei），未直接呼叫 `DateTime.UtcNow` / `DateTime.Now`？
- [ ] AppRouter 路由次序是否「具體路徑在 catch-all 之前」？
- [ ] 新 entity 是否同步：建 entity → 建 Configuration → AppDbContext 加 DbSet → 加 Migration？
- [ ] Migration 沒有改既有檔，只新增？

### 17.2 前端

> 詳見 [docs/frontend-design.md §19](frontend-design.md#19-一致性-checklistcode-review-用)

### 17.3 命名與結構

- [ ] C# 類別 / 方法 / 屬性 PascalCase；TypeScript 變數 / 函式 camelCase；DB 欄位 PascalCase
- [ ] Handler / DTO / Configuration / ReadService 命名遵循 §15.2 模式
- [ ] Migration 命名 `yyyyMMddHHmmss_<DescriptiveName>.cs`，已加 timestamp
- [ ] API path kebab-case，JSON property camelCase

---

## 18. Coding Style 一致性原則

### 18.1 強制原則

1. **先讀後寫**：新增 Handler 前先讀 [PaymentRequestHandler.cs](../Api/Handlers/PaymentRequestHandler.cs)；新增 ReadService 前先讀 [PaymentRequestReadService.cs](../Api/Services/Dapper/PaymentRequestReadService.cs)。**不可憑空想像架構**。
2. **跟隨既有模式**：命名、檔案結構、import 順序、方法排列順序、錯誤處理風格、回應格式 **一律比照既有檔案**。發現既有寫法有問題時，先提出討論再統一重構，不可單獨在新檔案改寫。
3. **同類功能同寫法**：所有 Handler 套用相同的 try/await/ApiResponse 模式；所有 ReadService 套用相同的 Dapper SQL 風格。
4. **禁止個人風格混入**：不得引入既有檔案沒用過的程式設計模式（如 Repository Pattern、自訂 IoC 容器、In-Process Function Model）。

### 18.2 違反處理

- **小幅偏離**（命名 / 檔案位置）：發現後立即修正
- **架構性偏離**（引入新模式 / 新框架）：**禁止單獨變更**，須先在本文件提案討論並更新規範後才能套用，並一次性重構所有同類檔案
- **Code Review 重點**：審查時優先確認「與既有檔案是否一致」，再看正確性與效能

> **判斷原則**：當你不確定該怎麼寫，就找 3 份相似的既有檔案，**取多數派寫法**。寧可保持「不完美但統一」，也不要「個別完美但分散」。

### 18.3 程式碼註解與邏輯同步

修改程式邏輯時，**必須一併更新該段程式碼的註解**，避免註解與實作矛盾造成後續誤解。

> 範例：[ApprovalFlowService.cs:51](../Api/Services/ApprovalFlowService.cs#L51) 曾因註解寫「`holiday_travel` 屬全程禁止自審」，但實際排除清單已將其歸入「首位跳過」群組，導致閱讀者誤判規則歸屬。

程式碼 / 註解 / 文件三者須同步：邏輯異動 → 同步檔案內註解 → 跨檔關鍵規則同步更新本文件 + CLAUDE.md。

---

## 19. 常用指令

```bash
cd /Users/tim/webapps/Jabez/Api
dotnet restore                          # 還原套件
dotnet build                            # 建置（必須 0 errors / 0 warnings）
func start                              # 本地啟動 Azure Functions（Port 7071）
dotnet ef migrations add <Name>         # 新增 Migration
dotnet ef database update               # 套用 Migration（本地手動，正常啟動會自動套用）
dotnet ef migrations remove             # 移除最後一個未套用的 Migration
```

---

## 20. 連結引用（Markdown）

template / 文件中引用其他檔案時，使用相對路徑 markdown link：

```markdown
[UserHandler.cs](../Api/Handlers/UserHandler.cs)
[UserHandler.cs:217](../Api/Handlers/UserHandler.cs#L217)
[UserHandler.cs:217-253](../Api/Handlers/UserHandler.cs#L217-L253)
[Api/Services/Dapper/](../Api/Services/Dapper/)
```

**禁止** 使用 backtick 包路徑或 HTML tag。
