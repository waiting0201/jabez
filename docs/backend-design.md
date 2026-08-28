# Jabez 後端設計規範

本文件彙整 Jabez API（Azure Functions .NET 10）的技術架構與寫作規範。**新增功能或修改後端前，必須先讀本文件確認 Handler / DTO / ReadService / Router / Migration 等規範**；與本文件衝突時以本文件為準（CLAUDE.md 同步引用本文件）。

> 業務邏輯（簽核流程、請假規則、薪資公式、部門可見性、LINE / 打卡提醒等）仍記載於 [CLAUDE.md](../CLAUDE.md)；本文件**只規範技術層面**。

---

## 1. 技術棧

| 項目 | 規格 | 備註 |
|---|---|---|
| 平台 | Azure Functions v4 — **Isolated Worker Model** | 非 In-Process |
| 框架 | .NET 10 | C# 12 List Pattern 廣泛使用 |
| ORM | EF Core（寫入 + Migration）+ Dapper（讀取） | 二選一規則見 §6 |
| 資料庫 | SQL Server | 本地 `JabezDb`（連線字串於 [Api/local.settings.json](../Api/local.settings.json)） |
| 認證 | JWT Bearer Token (HS256) | 由 [JwtService.cs](../Api/Services/JwtService.cs) 簽發 |
| 路由 | 單一入口 RouterFunction → AppRouter | C# 12 List Pattern dispatch |
| Blob | Azure Storage（本地 Azurite） | 容器：`avatars` / `signatures` / `indigenous-proofs` / `low-income-proofs` / `disabled-proofs` / `id-cards` / `education-proofs` / `passbooks` / `invoices` / `vendor-passbooks` / `request-attachments` / `quotes` |
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

JWT 驗證 + 權限檢查由 RouterFunction → AppRouter 統一執行；Handler 內**禁止重複檢查**權限碼。

`GetRequiredPermission` 的 fallback 是 `_ => null`（＝登入即可）。這是**寬鬆預設**：新增端點若忘了加對應，會安靜地對所有登入者開放。因此在自成一區的資源底下（如 attendances）務必補上 `[..]` catch-all 走較嚴的權限，讓遺漏至少落在保守側。

**Superadmin-only 路由**由獨立的 `IsSuperAdminRoute` 判定（不走權限表）。截至 2026-08 涵蓋：`/admin/attendance-reminder*`、`/admin/payment-reminder*`、以及 `/permissions` 的**寫入類與單筆讀取**（`POST` / `PUT` / `PATCH` / `DELETE /permissions[/{id}]` + `GET /permissions/{id}`）。
⚠️ `GET /permissions`（**列表**）刻意留在權限表回 `null` —— 角色編輯頁只要求 `roles:write`，卻要靠這支端點建權限勾選清單，鎖成 Superadmin 會讓管理員無法編輯角色。改動此處前先看 `AppRouter` 內的註解。

#### 同一資源的「前後台雙軌權限」

當一個資源同時有「員工對自己」與「管理者對別人」兩種用法時，**兩者必須是不同的權限碼**，不可共用。現行案例（2026-08，出勤打卡）：

| 對象 | 權限碼 | 端點 |
|---|---|---|
| 員工對自己 | `attendances:read` / `attendances:write` | `GET /attendances/today`、`POST /attendances/clock-*`、`overtime-*` |
| 管理者對別人 | `reports-attendance:read` / `reports-attendance:write` | `GET /attendances`、`PUT/PATCH /attendances/{id}` |

共用一組碼會造成「能打自己的卡 ＝ 能改全公司的卡」。權限碼只回答「**誰**能做」；「**能對誰**做」屬於資料範圍，另由 Handler 內的部門可見性 scope（`IProjectAccessResolver`）負責。讀寫兩端的 scope 必須對稱：若列表端有 scope 而寫入端沒有，就是缺口（`AttendanceHandler.UpdateAsync` 在 2026-08 前即為此例）。

#### 欄位級權限（Handler 內判定的例外）

「Handler 內禁止檢查權限碼」有一個例外：**同一支端點所有人都進得來，但其中某些欄位只給部分人看**。這種需求無法用路由層權限表達（表達得了就該拆端點），只能在 Handler 內讀 principal 的 `permissions` claim 後抹除欄位。

現行案例（2026-08，專案水位表）：

| 端點 | 進入權限（路由層） | 欄位級權限（Handler 層） | 效果 |
|---|---|---|---|
| `GET /reports/project-water-level` | `reports-project-water-level:read` | `reports-project-water-level:total` | 缺後者時 `TotalPercentage` / `PreImportUsedAmount` / `RemainingAmount` 回 `null` / `0`；頁面照進、業務執行水位照看 |
| `GET /users`、`GET /users/{id}`（含 `POST` / `PATCH` 的回應 DTO） | `users:read` / `users:write` | `payroll:read` | 缺後者時 [`PayrollFieldAccess.Mask`](../Api/Common/PayrollFieldAccess.cs) 把 8 個薪資欄回 `null`（底薪 / 伙食費 / 加班費 / 2 種加給 / 勞健保覆寫 / 勞退自提率）；`SendPaySlip`、`CompensatoryOpeningHours` 不含金額故保留 |
| `GET /users/{id}/profile` | `users:read` | `payroll:read` | 缺後者時 `SalaryAdjustmentRecords` 回 `[]`；其餘 8 張子表照常 |
| `PATCH /users/{id}`、`POST /users`、`PUT /users/{id}/profile` | `users:write` | `payroll:read` | 缺後者時薪資欄位的寫入一律忽略（不回 403，其他欄位照常存檔）；見規則 6 |

⚠️ 員工自助端點 `GET /me/user`、`GET /me/profile`、`GET /me/payroll` **刻意不套**此權限 —— 員工看自己的薪資是既有需求，`payroll:read` 只管「看別人的」。三支 Handler 方法內皆有註解防止後人「補齊一致性」時誤加。

規則：

1. **回 null 而非 403** —— 少一欄不該讓整頁掛掉，前端據此隱藏該欄即可。
2. **連同「能反推出該欄的原料欄」一起抹**。上例只藏 `TotalPercentage` 沒有用，`PreImportUsedAmount` / `RemainingAmount` 是它的分子來源，留著等同把數字送出去讓前端自己算。
3. **判定方式比照 `ApprovalTaskHandler`**：`is_superadmin == "true"` 直接放行，否則 `principal.FindAll("permissions").Any(c => c.Value == PermissionCodes.Xxx)`。principal 由 [`AppRouter`](../Api/Routing/AppRouter.cs) 寫入 `req.HttpContext.User`。
4. **ReadService 與 SQL 不動**，抹除一律在 Handler 用 record `with` 做，維持「Dapper 只管查、Handler 管授權」的分工。
5. 前端同步隱藏（縱深防禦，不是唯一防線）。前端 `hasPermission()` 讀的是 JWT 快照，**新權限上線後既有 token 要到下次登入 / refresh 才會帶到新碼**，期間該欄會暫時消失 —— 因為前後端都是「隱藏」而非報錯，畫面仍然正常。
6. **欄位級權限必須同時套在寫入端**（2026-08 薪資欄位的教訓）。只擋讀不擋寫有兩個獨立的失效模式：
   - **整批替換（delete-then-insert）型子表**：無權者的前端不 render 該區塊 → 送出空陣列 → 後端「先刪光再插入 0 筆」＝**靜默刪光既有資料**。對策是把該子表改為**條件式替換**：payload 的該欄位放寬為 nullable（`null` = 不變更、`[]` = 清空），Handler 以 `canSee && payload.Xxx is not null` 決定是否進入刪除 + 重建區塊。
   - **回應 DTO 未抹除**：`POST` / `PATCH` 成功後回傳重新讀出的完整 DTO，等於繞過 `GET` 的遮蔽。寫入端的回應也要走同一個 `Mask`。
   兩者都**不回 403** —— 同一支端點還要負責存其他欄位 / 子表，不該因為少一塊就整張存不了（同規則 1 的精神）。
7. 判定與抹除邏輯**跨 Handler 共用時抽成 `Api/Common/` 的 static helper**（如 [`PayrollFieldAccess`](../Api/Common/PayrollFieldAccess.cs)），不要每個 Handler 各複製一份 —— 「新增欄位時漏改其中一份」就是外洩。只有單一呼叫點時才比照 `ProjectWaterLevelHandler` 放 private static。

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

### 4.1.1 列表分頁的雙模式約定

需要分頁的列表端點（目前：`GET /projects`、`GET /vendors`）**共用同一個 URL**，由 query 參數決定回應形狀：

```csharp
public async Task<IActionResult> GetAllAsync(HttpRequest req)
{
    string? search = req.Query["search"];

    // 有分頁參數 → 回傳 PagedResult；無分頁參數 → 回傳平面陣列（供下拉選單用）
    if (req.Query.ContainsKey("page") || req.Query.ContainsKey("pageSize"))
    {
        int page     = int.TryParse(req.Query["page"],     out var p)  ? Math.Max(1, p)         : 1;
        int pageSize = int.TryParse(req.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, 100) : 20;
        return new OkObjectResult(ApiResponse.Ok(await reader.GetPagedAsync(page, pageSize, search)));
    }

    return new OkObjectResult(ApiResponse.Ok(await reader.GetAllAsync(search)));
}
```

- `page` 下限 1、`pageSize` 一律 `Math.Clamp(ps, 1, 100)`，預設 20（**禁止**讓前端指定無上限的 pageSize）
  - **例外：Excel 匯出模式**。前端匯出需一次取回全部資料，若沿用 100 的上限會被靜默截斷。做法是加 `?export=true` 旗標並改夾到**模組自訂的顯式常數**（如 `AttendanceLeaveMerger.ExportMaxPageSize = 5000`），仍**禁止**無上限：
    ```csharp
    bool isExport = req.Query["export"] == "true";
    int  maxSize  = isExport ? AttendanceLeaveMerger.ExportMaxPageSize : 100;
    int  pageSize = int.TryParse(req.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, maxSize) : 20;
    ```
    ⚠️ 已知未修：`/reports/overtime` 與 `/reports/payment` 的前端匯出仍送 `pageSize: 9999` 而後端夾到 100，**兩張報表的 Excel 實際只有 100 筆**，待比照此模式修正
- ReadService 同時提供 `GetAllAsync(...)` 與 `GetPagedAsync(page, pageSize, ...)`，兩者**共用同一段 WHERE 條件建構**（抽成 private helper，如 [VendorReadService.BuildSearchFilter](../Api/Services/Dapper/VendorReadService.cs)），避免搜尋條件在兩條路徑上分歧
- `GetPagedAsync` 內另跑一次 `SELECT COUNT(*)` 取 `TotalCount`，回傳 [PagedResult&lt;T&gt;](../Api/Common/PagedResult.cs)；`TotalPages` 以 `Math.Max(1, ceiling)` 保底
- 關鍵字一律以參數化 `@Search`（`%keyword%`）比對，**禁止**字串串接進 SQL

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

### 5.4 payload 形狀演進的向後相容 converter

當既有欄位由**純量陣列**擴充成**物件陣列**（例：假日活動參與日期由 `["2026-08-02"]` → `[{date, slot}]`）時，前端部署與後端部署之間有空窗期，舊版 SPA 快取仍會送舊形狀。以屬性層級 `[JsonConverter]` 掛一支同時吃兩種形狀的 converter，把舊形狀補上預設值，避免炸成 500：

```csharp
[JsonConverter(typeof(FlexibleParticipantDateConverter))]
public sealed record ParticipantDateRequest(DateTime Date, string? Slot = null);
```

- 放在 `Api/Common/`，比照既有 [FlexibleDateTimeJsonConverter.cs](../Api/Common/FlexibleDateTimeJsonConverter.cs)（`sealed class : JsonConverter<T>`，`Read` 依 `reader.TokenType` 分流，未知屬性 `reader.Skip()`）
- 新舊值域的**天數 / 權重換算集中在一個常數類別**（如 `Constants.cs` 的 `ParticipantDateSlots`），`Normalize` / `Weight` 對 null 與未知值一律回退到預設，讓「舊資料」「舊 payload」「DB 預設值」三條路徑收斂到同一行為

### 5.5 欄位型別 `int → decimal` 的連帶檢查（執行期地雷）

放寬數值欄位精度（如個人假日天數因半天改 `decimal(5,1)`）時，`dotnet build` **不會**擋下 Dapper 的 `dynamic` 轉型 —— `(int)row.Xxx` 對 decimal 欄位會在**執行期**丟 `InvalidCastException`。變更欄位型別時必須一併 grep 所有讀取點：

- Dapper mapping 的 `(int)` / `(int?)` 轉型 → 改 `(decimal)` / `(decimal?)`
- SQL 的 `UNION` / `SUM` 分支 → 顯式 `CAST(... AS decimal(5,1))`，不依賴隱含型別提升
- 對外 DTO 欄位型別 → 同步放寬（否則 API 回傳仍被截斷）
- 字串內插的顯示點 → 補格式（`.ToString("0.#")`），避免整數顯示成 `2.0`
- 完成後必須實際打一次 API 冒煙，不能只看建置結果

---

## 6. Dapper vs EF Core 使用原則

| 情境 | 使用 |
|---|---|
| 列表查詢、多表 JOIN、效能敏感 | **Dapper**（`Services/Dapper/<Module>ReadService.cs`） |
| 單筆查詢（含子表） | Dapper（`QueryMultipleAsync` 一次拉多表）|
| CRUD 操作、資料異動、Transaction | **EF Core**（`AppDbContext`） |
| Schema 管理（建表、Migration） | **EF Core Migration** |
| 整批替換子表 | EF Core `ExecuteDeleteAsync` + `AddRangeAsync`，包在 transaction 內；小量子表亦可 `RemoveRange(已 Include 的集合)` + `AddRangeAsync` 於同一次 `SaveChangesAsync`（EF 會在唯一索引衝突時先刪後插，見 `TravelRequestHandler` 參與者、[OvertimeRequestHandler.UpdateAsync](../Api/Handlers/OvertimeRequestHandler.cs) 關聯專案）。**父表若有合計快取欄，替換後必須同步重算**（如 `OvertimeRequest.EstimatedHours = projectRows.Sum(...)`） |

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

範例：[InstallmentUpsertService.Apply](../Api/Services/InstallmentUpsertService.cs)（分期撥款）— 5 個獨立 `PATCH /{type}-requests/{id}/installments` endpoint 與 `ApprovalTaskHandler` 的「財務核准當下原子寫入撥款明細」共用同一份持久化邏輯。5 種子表透過 [IInstallmentEntity](../Api/Models/Entities/IInstallmentEntity.cs) 介面 + `Func<TEntity>` create factory 泛型化（FK 由 factory 設定，其餘欄位由 helper 填）。

**5 種分期撥款父表**（[InstallmentParentTable](../Api/Services/Dapper/InstallmentReadService.cs)）：`PaymentRequest` / `AdvanceRequest` / `TravelRequest` / `TravelPaymentRequest` / **`WriteOffRecord`**。

前 4 種的 `SUM(Amount)` 等於父表整單金額；**第 5 種（預支沖銷）不同** —— 對應的是 [WriteOffRefundCalculator](../Api/Common/WriteOffRefundCalculator.cs) 算出的 `RefundDue`：

```
RefundDue = max(0, 前次已沖銷 + 本次沖銷 − 預支總額)
          − max(0, 前次已沖銷            − 預支總額)
```

以「增額」而非「總超支」計算，讓每張沖銷單各自算得出、彼此不重疊，加總即等於整張預支單的超支總額，**不需等到結案**。`RefundDue = 0`（未超支）的沖銷單不會有任何 installment，財務核准時也不要求填寫。

> 新增第 6 種分期父表時必須同步：`IInstallmentEntity` 實作 + EF Config + `InstallmentParentTable` enum + Router 的 `IsFinanceOrSuperAdminRoute` + `UserHandler.DeleteAsync` 的 `PaidByUserId` 清洗清單。

範例：[DesignatedReviewerHelper](../Api/Common/DesignatedReviewerHelper.cs)（申請人指定審核者）— 9 種申請類型的 `SubmitAsync` / `Create` / `Update` 共用 `BuildEntities`（由請求建實體）/ `ReadForFlowAsync`（讀回傳給 `ResolveStartingStepAsync`）/ `ValidateAndNormalizeAsync`（送單時把未綁定的 `ApprovalStepOrder=0` 正規化成唯一 designated step 的 StepOrder，並驗證每個指定步驟皆有 designee）。`ValidateAndNormalizeAsync` 只改 tracked entity，呼叫端隨後 `SaveChanges`。一條流程多個 `UseApplicantDesignated` 步驟時，每筆 designee 以 `ApprovalStepOrder` 綁定步驟，引擎所有 designee 查詢一律加 `ApprovalStepOrder == CurrentStepOrder`（[ApprovalTaskHandler](../Api/Handlers/ApprovalTaskHandler.cs) / [ApprovalFlowService](../Api/Services/ApprovalFlowService.cs) / [PaymentRequestReadService](../Api/Services/Dapper/PaymentRequestReadService.cs) StepMatch 三者條件須同步）。

範例：[AdvanceSupplementService.RollbackAsync](../Api/Services/AdvanceSupplementService.cs)（追加預支回滾）— 「駁回」（`ApprovalTaskHandler` advance 分支）與「主動放棄」（`DELETE /advance-requests/{id}/supplements/{n}`）兩個入口共用。回傳需在 `SaveChanges` **之後**刪除的 blob 名稱清單（blob 刪除不可進 DB 交易，失敗也不該讓交易 rollback）。⚠️ 由駁回進來時，本次駁回的 `ApprovalRecord` 還在 ChangeTracker 尚未寫入 DB，`Where(...).ToListAsync()` 抓不到，必須另外掃 `ChangeTracker.Entries<T>()` 把 `Added` 狀態的同批次紀錄 `Detach`，否則 SaveChanges 會留下指向已刪除批次的孤兒紀錄。

範例：[LeaveDayExpander](../Api/Common/LeaveDayExpander.cs)（請假單逐日展開）— 純讀取型 static helper，收 `ICalendarDayReadService`。把一張 `LeaveRequest` 攤成 `List<LeaveDay>{Date, Hours}`，展開規則與 `LeaveRequestHandler.SubmitAsync` 的權威重算一致，保證 `Σ Hours == LeaveRequest.Hours`。消費點：銷假逐日勾選（`GET /leave-requests/{id}/revocable-dates`）、銷假核准後重算父單 `Hours`、以及出缺勤報表的請假合併（`AttendanceLeaveMerger`）。假別分類常數 `WorkingDayLeaveTypes` / `TimeUnitMap` / `GetTimeUnit` / `TimeUnitToString` 一併收斂於此，`LeaveRequestHandler` 轉引同一份（避免兩地各留一份而漂移）。另提供 `ExpandAsync(calendarReader, leaveType, startDate, endDate)` overload 供 Dapper 投影使用（展開只讀這三個欄位，不必為此撈出完整 entity）。

範例：[AttendanceLeaveMerger](../Api/Common/AttendanceLeaveMerger.cs)（出缺勤報表「打卡 ∪ 請假日」合併）— 純讀取型 static helper，收 `IAttendanceReadService` + `ICalendarDayReadService` 為參數。合併粒度＝**(員工, 日期) 一列**：有打卡+有請假合併同列、只有請假產 `Id = null` 的虛擬列、沒打卡也沒請假不產列。**刻意不做成 SQL JOIN**：逐日請假時數必須走 `LeaveDayExpander`（C# 的行事曆 + 半天/小時規則），SQL 端複製一份必然漂移；且同日多張假單 JOIN 會產生重複列（舊實作的 `ListSql` / `CountFromSql` 因此 total 與列數不一致）。代價是分頁改為「**區間全量載入 → 記憶體合併 → 記憶體切頁**」，因此：(1) 呼叫端必須把區間收斂在 `MaxRangeDays`（400 天）內，未指定起訖時回退近一年；(2) 排序必須是 total order（日期 DESC → 姓名 Ordinal → `Id ?? int.MaxValue`），否則翻頁會漏列 / 重複列。ReadService 端配合拆成三支純原料查詢：`ListInRangeAsync`（打卡，不分頁）/ `ListApprovedLeavesInRangeAsync`（假單，**不可加銷假過濾** —— 銷假是逐日的，整張單層級過濾會誤刪部分銷假的其餘日子）/ `ListApprovedRevokedDatesAsync`（批次銷假日，空清單提前 return 避免 Dapper 產生 `IN ()`）。

範例：[CachedCalendarDayReadService](../Api/Services/Dapper/CachedCalendarDayReadService.cs)（行事曆快取 decorator）— 包裝 `ICalendarDayReadService`，以「年」為粒度快取 holiday set 與 `HasDataForRange`。解決 `LeaveDayExpander` 逐張假單展開造成的 N+1（N 張假單 2N+ 次 round-trip → 每年度最多 2 次）。**刻意不註冊進 DI**：只在唯讀合併流程中 `new`，生命週期限縮在單次作業，避免與同請求內的行事曆寫入產生陳舊快取。

範例：[LeaveRevocationService.ApplyAsync](../Api/Services/LeaveRevocationService.cs)（銷假核准套用）— 不呼叫 `SaveChanges`，交易邊界交呼叫端（同 `AdvanceSupplementService` 慣例）。**從「該假單所有已核准銷假單的 distinct 日期」整組重算**父單 `Hours`，而非 `Hours -= X`：天然冪等、併發安全，兩張銷假單搶同一天也會收斂。⚠️ 由 `ApprovalTaskHandler` 進來時，本張銷假單的 `ApprovalStatus="approved"` 還在 ChangeTracker 尚未寫入 DB，只查 DB 會漏掉自己 —— 必須明確併入自己的日期（取聯集）。同檔另提供 `NotRevokedClause(alias, dateExpr)` 供下游 Dapper 查詢共用「該日未被核准銷假」的 `NOT EXISTS` 片段（打卡阻擋 / 休假日免下班卡 / 出缺勤報表 / 打卡提醒）。

範例：[WorkCalendarHelper](../Api/Common/WorkCalendarHelper.cs)（公司行事曆判定）— 純讀取型 static helper，收 `ICalendarDayReadService` 為第一個參數。「**行事曆有資料 → 用 `CalendarDay.IsHoliday`；該年度無資料 → 退回週六 / 週日**」是全系統假日判定的單一真相，兩種粒度共用同一份規則：區間版 `ComputeWorkingDatesAsync`（[LeaveRequestHandler](../Api/Handlers/LeaveRequestHandler.cs) 算請假日清單 / Hour 單位時數 / Submit 擋件）與單日版 `IsHolidayAsync`（[AttendanceHandler](../Api/Handlers/AttendanceHandler.cs) 判「休假日加班免下班卡」）。新增需要判假日的功能時**一律呼叫此 helper**，不要在 Handler 內自己寫一份 `DayOfWeek` fallback。**`bool ignoreHolidays` 必填參數（2026-08 新增）**：三個方法的第二個參數為排班制旗標（`User.IsShiftWorker`，賣店 / 營業所六日與國定假日照常營業），為 `true` 時整段皆為工作日、**完全不查行事曆**，且 `HasCalendarForAllYearsAsync` 恆回 `true`（送件不會被「尚未匯入行事曆」擋）。此參數**刻意必填、不給預設值** —— 新增消費點時漏傳是編譯錯誤，而不是讓某人的請假天數被靜默算成 0；[LeaveDayExpander](../Api/Common/LeaveDayExpander.cs) 的兩個 `ExpandAsync` 同此原則。旗標一律以**假單所有人 / 打卡本人**解析（單人路徑走 [WorkPatternReadService](../Api/Services/Dapper/WorkPatternReadService.cs) 的 request-scoped memo；出缺勤報表這類逐員工迴圈則由 SQL 隨資料列帶出，避免 N+1），**不可用呼叫者 id** —— Superadmin 代送、主管核准銷假時呼叫者都不是本人。搭配 [Constants.cs](../Api/Common/Constants.cs) 的 `WorkdayHours`（08:00–17:00，午休 12:00–13:00，`FullDayHours = 8`）為工作日時段常數單一來源 —— 消費點含請假時數計算、全日請假判定、以及 [AuthHandler](../Api/Handlers/AuthHandler.cs) 登入時的**自動補下班卡**（上班打卡時間 + `FullDayHours` + 午休 `LunchEndHour - LunchStartHour` → 一律 +9，不分上下午打卡）。與 `SystemSetting.WorkStartTime / WorkEndTime`（09:00 / 18:00）語意不同，刻意不合併：後者自 2026-08 起**只**服務打卡提醒的時點判斷，不再參與補卡。

**部門最高層級抑制（單一真相）**：同檔 `GetSuppressedDesignatedStepOrdersAsync(db, approvalItemId, designatedReviewers)` 為送單驗證與簽核解析共用的判定 — 若第一個指定步驟為 `DesignatedRequiresDepartment=true` 且其首位 designee ＝所選部門（`SelectedDepartmentId`）中 active／非 superadmin／有職稱者最高職稱（min `JobTitle.Level`）本人 → 回傳其後所有指定步驟 StepOrder 為「被抑制」集合。三處呼叫：`ValidateAndNormalizeAsync`（被抑制步驟不要求 designee）、`ResolveStartingStepAsync` 與 `SkipUnreviewableStepsAsync`（被抑制步驟走「乾淨跳過、不寫代簽」）。判定放靜態 helper 而非 `ApprovalFlowService` 私有方法，是為了讓 static 的 `ValidateAndNormalizeAsync` 也能共用同一份邏輯。

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
> **兩個 FK 的關聯子表一律「一 Cascade + 一 NoAction」**：子表若同時指向父表與另一主檔（如 `ApprovalStepException` → ApprovalStep + Users、`ApprovalStepDesignatedJobTitle` → ApprovalStep + JobTitles、`OvertimeRequestProject` → OvertimeRequests + Projects），兩邊都設 Cascade 會撞 SQL Server 1785 multiple cascade paths，且錯誤發生在 migration 套用當下（API 啟動即失敗）。慣例是父表 Cascade、第二個主檔 NoAction + 在該主檔的 `DeleteAsync` 處理。
>
> 第二主檔的處理有**兩種形式，依「清空是否無損」決定**：
> - **清洗**（清空語意無損）：`ApprovalStepDesignatedJobTitles` 於 [JobTitleHandler.DeleteAsync](../Api/Handlers/JobTitleHandler.cs) 用 `ExecuteDeleteAsync` 移除，語意退回「不限職稱」，無資料損失。
> - **阻擋**（清空會破壞其他不變式）：`OvertimeRequestProjects` 於 [ProjectHandler.DeleteAsync](../Api/Handlers/ProjectHandler.cs) 直接 `throw AppException.BadRequest`，因為刪掉明細列會使父表 `OvertimeRequest.EstimatedHours` 合計快取失真（且含已核准單），比照同檔既有的 `PaymentRequests` 引用保護。
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

### 8.4 索引一律宣告在 Configuration，不可只寫在手寫 Migration（**重要**）

在 Migration 裡直接 `migrationBuilder.CreateIndex(...)` 但**沒有**在 `<Entity>Configuration.cs` 補
`builder.HasIndex(...)`，資料庫會有索引、model snapshot 卻沒有 → **設定與資料庫漂移**：

- 後續 `migrations add` 完全看不到這個索引，也不會維護它
- Code Review 讀 Configuration 會誤判「這個欄位沒有唯一約束」而寫出仰賴唯一索引擋併發的邏輯
- 2026-08 踩過：`WriteOffRecords.RequestNo` 的唯一索引只存在於手寫 migration，Handler 註解寫著「唯一索引保護並發」，但從 EF 模型完全看不出來；同期新增的 `TravelWriteOffRecords` 就漏了索引

**補宣告時，Migration 必須寫成可重入**（既有 DB 已有索引、全新 DB 沒有，同一份 migration 都要能跑）：

```csharp
migrationBuilder.Sql("""
    -- 先清洗會違反唯一性的舊資料（空白值補流水號、重複值加後綴）
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = 'IX_Xxx_RequestNo' AND object_id = OBJECT_ID('Xxx'))
        CREATE UNIQUE INDEX IX_Xxx_RequestNo ON Xxx (RequestNo);
    """);
```

參考：[20260817015432_AddWriteOffRequestNoUniqueIndex](../Api/Data/Migrations/)

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
| `passbooks` | 員工存摺封面 | 授權 `users:read` |
| `invoices` | 發票檔 | 授權 |
| `vendor-passbooks` | 廠商存摺封面 | **登入即可** `/files/vendor-passbooks/{fileName}`（一般檔，與 avatars/signatures 同層） |
| `quotes` | 報價單（預審 / 請款品項） | **登入即可** `/files/quotes/{*path}`（一般業務檔；blob name 含日期子路徑 `yyyy/MM/{guid}{ext}`，代理需以 slice pattern 接多段並用 `IsSafeSubPath` 放行 `/`） |
| `request-attachments` | 整單批次附件（請款（廠商 / 一般）/ 預支沖銷 / 預審的照片或 PDF） | **登入即可** `/files/request-attachments/{*path}`（同 quotes，blob name 含日期子路徑） |

> **歷史教訓（2026-06）**：`quotes` / `request-attachments` 為私有容器（`PublicAccessType.None`），DB 存的是**無 SAS 的原始 blob URL**。前端若直接 `fetch()` / iframe 這些 URL 會 403 / CORS——預審 PDF 合併上傳檔曾因此**靜默失敗**（`_fetchFileBytes` 回 null 略過）。修正方式：(1) 後端補上述兩個代理路由（blob name 含 `/`，需 slice pattern + `IsSafeSubPath`，**不可**放寬既有 `IsSafeFileName`）；(2) 前端以 `resolveFileProxyUrl()`（[pdf-core.service.ts](../Admin/src/app/shared/services/pdf-core.service.ts)）把原始 blob URL 轉成代理路徑後再經 HttpClient（帶 JWT）取用。

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
| `GET /users/lookup` | `GET /users`（需 `users:read`） | 申請表「指定審核者」、人員下拉；回傳含 `jobTitleLevel`（供「部門最高層級」判定，數字越小越高） |
| `GET /projects/active` | `GET /projects`（需 `projects:read`） | 申請表「專案」下拉，僅回傳 `active` 狀態；預設依使用者部門可見範圍過濾。帶 `?all=true` 時不過濾部門，回傳全部 `active` 專案（供加班申請等跨部門支援情境瀏覽用） |
| `GET /approval-items/active?type=<applicationType>` | `GET /approval-items`（需 `approvals:read`） | 申請表判斷流程是否含 `useApplicantDesignated` 步驟 |
| `GET /job-titles/lookup` | `GET /job-titles`（需 `job-titles:read`） | 申請表「指定審核者」職稱下拉 |
| `GET /vendors/lookup` | `GET /vendors`（需 `vendors:read`） | 請款表單「廠商」下拉，僅回 `IsActive=true` |
| `GET /vendors/lookup-by-tax-id?taxId=XXXXXXXX` | — | 以統編查 GCIS 公司登記資料，自動帶出廠商名稱 / 地址 / 負責人；任何登入者可用 |
| `GET /leave-requests/working-days?start=&end=&leaveType=` | `GET /calendar-days`（需 `calendar-days:read`） | 請假表單即時計算扣除國定假日與六日後的請假日清單與天數；重用 `CalendarDayReadService`，工作日型假別（除歲時祭儀假外的 16 種）才扣假日，避免把後台行事曆權限強加給請假員工 |
| `POST /vendors` *(無需權限)* | — | 請款表單 quick-add modal：任何登入者皆可新建廠商，避免後台 CRUD 權限被強加給請款人 |
| `GET /files/signatures/{fileName}` / `/files/avatars/{fileName}` | — | 簽名檔 / 頭像 Blob 代理（公開路由） |
| `GET /me/user` | `GET /users/{id}`（需 `users:read`） | 「個人資訊」唯讀頁：員工查看自己的帳號資料（從 JWT `sub` 取自身 id） |
| `GET /me/profile` | `GET /users/{id}/profile`（需 `users:read`） | 「個人資訊」唯讀頁：員工查看自己的人事資料卡 + 健保眷屬 |
| `GET /me/files/{container}/{fileName}` | `GET /files/<PII container>/{fileName}`（需 `users:read`） | 「個人資訊」唯讀頁：員工讀自己的 PII 檔案，見下方 §13.4 |
| `GET /me/payroll?months=12` | `GET /payroll`（需 `payroll:read`，且一次回全公司） | 「個人資訊」→「過往薪資」Tab：員工查自己近 N 個月薪資明細。共用 `IPayrollReadService.CalculateMonthlyPayrollAsync(year, month, employeeId)` 的同一份公式，只多帶 employeeId 過濾，不另開計算邏輯 |

> HR 敏感 PII（`/files/indigenous-proofs/`、`/files/low-income-proofs/`、`/files/disabled-proofs/`、`/files/id-cards/`、`/files/education-proofs/`、`/files/passbooks/`）的**管理端**代理**不**走輕量模式，仍需 `users:read`；員工要讀**自己的** PII 改走 `/me/files/{container}/{fileName}`（§13.4）。

### 13.4 「自己讀自己」模式（Self / Me Endpoints）

當員工要查看**自己的**完整資料（含薪資、PII 檔案）時，不能放寬管理端權限，否則會洩漏他人資料。改採 **`me` 自助端點**：從 JWT `sub` claim 取當前 userId，只回傳 / 服務該 userId 的資料。

- **取得 userId**：Router 在驗證後已將 principal 寫入 `req.HttpContext.User`（[AppRouter.cs](../Api/Routing/AppRouter.cs) line ~87）；Handler 以 `req.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)` 取出，解析失敗回 401。複用既有 Read Service（如 `reader.GetByIdAsync(userId)` / `EmployeeProfileReadService.GetByUserIdAsync(userId)`），不需新 DTO / Migration。
- **PII 檔案自助代理** [`FileHandler.GetMineAsync`](../Api/Handlers/FileHandler.cs)：
  1. **白名單容器**：`SelfServiceContainers`（id-cards / education-proofs / passbooks / 三種 proofs / avatars / signatures），不在白名單一律 404。
  2. **前綴檢查**：所有 blob 命名都以 `{userId}` 開頭（`{userId}{ext}` / `{userId}_front{ext}` / `{userId}_education{ext}` / `{userId}_passbook{ext}`…），驗證 `fileName` 以自身 `userId` 開頭且後接 `.` 或 `_`，否則 403 — 防止員工竄改 `fileName` 讀他人檔案（GUID 定長 + 分隔符，無前綴包含風險）。
  3. 通過後複用既有私有 `GetFileAsync`（blob 串流 + Content-Type 驗證）。
- **即時計算型自助端點** [`PayrollHandler.GetMineAsync`](../Api/Handlers/PayrollHandler.cs)（`GET /me/payroll?months=12`）：資料不是「查一張表」而是「算出來的」，故不新增 Read Service，改在既有 `CalculateMonthlyPayrollAsync` 加上可為 null 的 `employeeId` 過濾（7 段 SQL 各加 `AND (@EmployeeId IS NULL OR ... = @EmployeeId)`），Handler 逐月呼叫並取 `Employees.FirstOrDefault()`。兩點必須注意：
  1. `months` 一律 clamp（1~24），避免有人送 `months=99999` 打爆 DB。
  2. **必須自行擋掉到職日之前的月份** —— 員工查詢 SQL 只濾 `Status` / `ResignDate`，不擋的話到職前的月份會算出一筆「全額底薪」的假資料。
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

### 13.6 部門受限篩選端點（Restricted Filter Lookup）

與 §13.1 相反的情境：列表的**某個篩選條件只開放特定部門**（不是靠 Permission，而是靠部門 Code）。此時選項端點與篩選參數**兩者都要擋**，只擋一邊等於沒擋。

- **已採用**：`GET /approval-tasks/applicants`（簽核作業「申請人」下拉，僅財務體系部門 / Superadmin）
- **判定共用一個 private static**，選項端點與列表端點都呼叫它，避免兩處條件漂移：

  ```csharp
  private static bool CanFilterByApplicant(bool isSuperAdmin, string? deptCode)
      => isSuperAdmin || DepartmentCodes.FinancialAndAbove.Contains(deptCode ?? "");
  ```

### 13.7 選項端點與寫入端點的可視範圍必須一致

「下拉能選到的東西」與「送出時後端接受的東西」**必須同一組條件**，否則使用者選得到卻送不出去，
而且錯誤訊息通常是無從理解的 404。

2026-08 踩過：`GET /write-off-requests/available-advances` 對 Superadmin 回傳**所有人**的預支單，
但 `POST /write-off-requests` 的查詢寫死 `x.SubmittedById == submittedById` → Superadmin 選了別人的
預支單，送出必定 `404 AdvanceRequest not found`（同一個 Handler 的 Update / Delete / GetById 都有放行
Superadmin，只有 Create 沒有）。

**Checklist**：新增 / 修改「先選一個父單再建子單」的流程時，比對三處是否同一條件 ——
① 選項端點的 `Where`、② 寫入端點載入父單的 `Where`、③ 同 Handler 其他方法的可視範圍。

- **選項端點**：不符資格回 `403`（比照 `scope=director` 的擋法）
- **列表篩選參數**：不符資格**靜默忽略**（當成沒帶），不回 403 —— 篩選只會縮小自己看得到的範圍，回錯誤反而讓正常瀏覽變脆弱

  ```csharp
  Guid? submittedByUserId = CanFilterByApplicant(callerIsSuperAdmin, callerDeptCode)
                            && Guid.TryParse(req.Query["submittedByUserId"], out var sbu) ? sbu : null;
  ```

- **多維度篩選不得塞進同一個 query param**：正交的維度（範圍 vs 狀態）各開一個參數，別組成 `director_approved` 這種複合字串。複合字串會逼 Handler 用前綴判斷，且該值若同時被當成 SQL 參數拿去和欄位等值比對（`ApprovalStatus = @StatusFilter`），就會靜默回空。
- **列舉型 query param 一律走白名單正規化**，非法值收斂到安全預設，不得讓未知值 fall through 到某個 `if` 都沒命中的分支：

  ```csharp
  private static readonly HashSet<string> ValidListStatuses = ["pending", "approved", "returned", "rejected"];
  string? status = rawStatus is null ? null
                 : ValidListStatuses.Contains(rawStatus) ? rawStatus
                 : "pending";
  ```

  **實際踩過的坑**：`StepMatchClause` 原本沒有 `returned` 分支，非 Superadmin 傳 `status=returned` 會掉到最後的待審核 fallback（該分支寫死 `ApprovalStatus = 'pending'`），**回傳完全不相干的待審清單而非空集合** —— 沒有錯誤、沒有 log，只有使用者覺得「這頁怪怪的」。詳見 [approval-flow.md](business/approval-flow.md) 的「退回修改中」節。

- **部門 Code 集合**一律取自 [Constants.cs](../Api/Common/Constants.cs) 的 `DepartmentCodes.*`，並與前端同名集合同步（見 [frontend-design.md §3 權限差異化篩選控件](frontend-design.md#權限差異化篩選控件依部門--權限顯示)）
- **路由次序**：選項端點是字面段（`["approval-tasks", "applicants"]`），**必須**排在 `["approval-tasks", var id]` 之前（§3.3）

---

## 13.5 父單多批次（Round）模式

**目前唯一採用者：追加預支**（[AdvanceRequestHandler](../Api/Handlers/AdvanceRequestHandler.cs) + [AdvanceSupplementService](../Api/Services/AdvanceSupplementService.cs)）。當「已核准的單需要再追加內容，且追加要重跑簽核、金額要併回原單」時採用此模式，而非開一種新申請類型。

**設計原則：**

1. **批次表只存 ≥2**：Round 1 就是父單本身（父單的 `AdvanceDate` + `RoundNo=1` 的明細），批次表只存追加批次 → 零資料重複、migration 免 backfill。
2. **批次表不存金額**：各批次金額一律由 `SUM(子表 WHERE RoundNo = N)` 推導（同 CLAUDE.md「分期撥款單一真相」精神）。父表的 `GrandTotal` 是全批次加總，仍保留（撥款驗證、沖銷餘額、報表都吃它）。
3. **`CurrentRoundNo` 用 NOT NULL DEFAULT 1，不要用 nullable「PendingRoundNo」**：nullable 版本在「批次核准後歸 null」時會退化成 1，讓 SQL 去重子查詢比對到錯誤批次。
4. **回滾快照**：批次表帶 `Prev*` 欄位快照父單送出前的核准狀態（`CurrentStepOrder / ReviewedAt / ReviewedById / ReviewNote`），駁回時原樣還原 —— 不要試圖從歷史紀錄推導。
5. **`ApprovalRecord.RoundNo` 為多型共用欄位**：`DEFAULT 1` 讓其餘 9 種申請與既有資料自動相容。
6. **⚠️ 新增批次時重算父表總額，不可 `ar.Items.Concat(newItems)`**：`db.AddRange` 後 EF 的 change-tracker fixup 會把 newItems 補進 `ar.Items`，直接 Concat 會**重複計算**。一律寫 `ar.Items.Where(i => i.RoundNo != roundNo).Concat(newItems)`。
7. **「此人已審過」的判定一律加批次條件**：見 [approval-flow.md 追加預支重跑簽核](business/approval-flow.md#追加預支重跑簽核2026-07-新增) 列出的四處；批次由 `AdvanceSupplementService.ResolveCurrentRoundAsync(db, appType, appId)` 解析（非 advance 恆回 1）。**新增任何「查 ApprovalRecords 判斷是否已審」的程式碼時，必須加進該清單。**
8. **父單編輯 / 刪除守門**：有進行中批次時（`CurrentRoundNo > 1 && status ∈ {pending, returned}`）禁止整單 `PATCH` / `DELETE` —— 批次被退回時父單狀態是 `returned`，不擋的話申請人可以改掉甚至刪掉已核准、已撥款的原始內容。
9. **必須提供「放棄批次」出路**：否則批次被退回而申請人不想改時，整張單會永久凍結（不能沖銷、不能結案、不能刪）。

**Dapper 讀取**：批次清單另起一段查詢，與明細在 C# 端合併（[AdvanceRequestReadService.BuildRounds](../Api/Services/Dapper/AdvanceRequestReadService.cs)，`internal static` 讓 `PaymentRequestReadService` 組簽核作業頁資料時共用同一份組裝邏輯）。明細 SQL 的 `ORDER BY` 一律加 `RoundNo` 於 `SortOrder` 之前，否則不同批次的明細會交錯。

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
- 範例：[Api/Data/Seed/ProjectImporter.cs](../Api/Data/Seed/ProjectImporter.cs)（讀 `project-import.json` → Project + ProjectPaymentSchedule）。旗標 `RUN_PROJECT_IMPORT=true` 觸發、`PROJECT_IMPORT_DRY_RUN=true` 只印計畫不寫 DB。去重鍵為 `Project.Code`，期別明細比照 `ProjectHandler.UpdateAsync` **全量重建**；刻意直寫 entity 繞過「已結案不可修改」限制，資料有誤可改 JSON 後重跑覆蓋。
- 範例：[Api/Data/Seed/VendorImporter.cs](../Api/Data/Seed/VendorImporter.cs)（讀匯入 JSON → Vendor）。旗標 `RUN_VENDOR_IMPORT=true` 觸發、`VENDOR_IMPORT_DRY_RUN=true` 只印計畫不寫 DB、**`VENDOR_IMPORT_FILE` 指定 `Data/Seed` 下的資料檔**（預設 `vendor-import.json`）—— 多來源檔共用同一支 importer，各檔獨立重跑，避免重跑新檔時連帶覆蓋掉舊批已在後台手動補的件。目前有 `vendor-import.json`（31 筆，壯圍沙丘匯款資料，缺統編／地址／存摺封面，`Note` 寫入待補標記）與 `vendor-import-1150820.json`（109 筆＝廠商 79 ＋ 個人 30，廠商及個人資料建置表；兩 sheet 欄位結構相同，唯一差別是識別碼欄對應 `TaxId` 或 `IdNumber`，`Note` 只存來源原文）。
  **去重鍵為「識別碼優先、名稱 fallback」：`TaxId` → `IdNumber` → `Name`。** `VendorConfiguration` 對 `TaxId` / `IdNumber` 皆有 filtered unique index，只用 `Name` 比對會在「同一統編換名字」時撞索引；識別碼命中 A、名稱命中 B 且非同一列時跳過該筆並印錯誤，交人工判讀（新增類似匯入工具時，凡目標表有 unique 業務鍵，去重鍵一律以該鍵優先）。刻意直寫 entity 繞過 `VendorHandler` 的「統編／身分證字號二擇一必填」、格式驗證與「存摺封面必填」，故匯入的廠商在後台**編輯儲存時仍會被擋**，須先補件。
- 中間 JSON 須在 `Api.csproj` 加 `<None Update="Data/Seed/xxx.json"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></None>`，否則 `AppContext.BaseDirectory` 讀不到。
- 因 DbContext 啟用 `EnableRetryOnFailure`，每筆須包 `CreateExecutionStrategy() + BeginTransactionAsync()`（同 §4 Handler 規範）。
- 去重採 **upsert**（Email 或唯一業務鍵命中即覆蓋 update-in-place、子表 `ExecuteDelete` 後重建），確保可重跑不重複。
- 民國/西元日期解析共用 [RocDateParser](../Api/Data/Seed/RocDateParser.cs)（年 ≤ 150 視民國 +1911）。
- 旗標預設 `false`；`local.settings.json` 不進版控、`CopyToPublishDirectory=Never`，旗標不會帶到 prod。

### 16.5 TimerTrigger 排程規範（重要）

正式站是 **Flex Consumption（scale-to-zero）**，冷啟動會讓 tick 延遲數十秒到數分鐘，也可能讓同一個 occurrence 被兩個實例各跑一次。排程程式一律照以下三條寫，不要假設 tick 會準時、也不要假設只會跑一次：

1. **不要用「精確時刻等值」判斷命中**。以時間窗（例：目標時刻起算 10 分鐘）取代 `now.ToString("HH:mm") == target`——錯過那一分鐘就整天不執行。
2. **自己做冪等，不要依賴平台的 singleton lock**。以 DB 既有的執行紀錄當去重鍵（打卡提醒用 `AttendanceReminderLogs` 的 `batchStart`、撥款提醒用 `PaymentReminderLog` 的同日 success），且**紀錄必須寫在主要工作之前**才擋得住第二個實例。
3. **`timer.IsPastDue` 只記 log，不要 `return`**。有第 2 點保護後補跑是安全的；提前 return 等於主動放棄該槽位。

實例與事故紀錄詳見 [business/attendance-reminder.md](business/attendance-reminder.md#時間窗--冪等2026-08-重構重要)。

### 16.6 Application Insights（isolated worker）

`Program.cs` 的 `ConfigureServices` 必須有這兩行，否則 worker 內所有 `ILogger` 輸出都不會進 App Insights：

```csharp
services.AddApplicationInsightsTelemetryWorkerService();
services.ConfigureFunctionsApplicationInsights();
```

⚠️ **版本必須配對**：`Microsoft.ApplicationInsights.WorkerService` 要用 **2.x**（目前 2.23.0）。裝成 3.x 會拉進 `Microsoft.ApplicationInsights` 3.x，與 `Microsoft.Azure.Functions.Worker.ApplicationInsights` 需要的 2.x 型別衝突——**`dotnet build` 會過，但 host 啟動時 `TypeLoadException: ITelemetryInitializer` 直接掛掉整個 Function App**。改動這兩個套件版本後，務必用 `func start` 實際啟動驗證，不能只看建置結果。

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
