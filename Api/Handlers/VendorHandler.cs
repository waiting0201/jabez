using Jabez.Api.Common;
using Jabez.Api.Data;
using Jabez.Api.Models.Dtos;
using Jabez.Api.Models.Entities;
using Jabez.Api.Services;
using Jabez.Api.Services.Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Jabez.Api.Handlers;

public sealed class VendorHandler(
    AppDbContext        db,
    IVendorReadService  reader,
    IBlobStorageService blob,
    IGcisService        gcis)
{
    private const string BankBookContainer = "vendor-passbooks";
    private const string IdCardContainer   = "vendor-id-cards";
    private static readonly string[] AllowedFileTypes = ["image/png", "image/jpeg", "application/pdf"];
    private static readonly Regex TaxIdPattern    = new(@"^\d{8}$", RegexOptions.Compiled);
    private static readonly Regex IdNumberPattern = new(@"^[A-Za-z][0-9]{9}$", RegexOptions.Compiled);

    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    // GET /api/vendors/lookup — 輕量級廠商清單（供下拉選單，不需 vendors:read 權限）
    public async Task<IActionResult> GetLookupAsync()
    {
        var list = await reader.GetLookupAsync();
        return new OkObjectResult(ApiResponse.Ok(list));
    }

    // GET /api/vendors/lookup-by-tax-id?taxId=XXXXXXXX — 以統編查詢公司資料（GCIS Open Data）
    // 任何登入者可用，避免員工建請款時要 vendors:read 才能查統編。
    public async Task<IActionResult> LookupByTaxIdAsync(HttpRequest req)
    {
        var taxId = req.Query["taxId"].ToString().Trim();
        if (!TaxIdPattern.IsMatch(taxId))
            return new BadRequestObjectResult(ApiResponse.Fail("統編格式錯誤，須為 8 位數字。"));

        var result = await gcis.LookupByTaxIdAsync(taxId);
        return result is null
            ? new NotFoundObjectResult(ApiResponse.Fail("查無此統編資料。"))
            : new OkObjectResult(ApiResponse.Ok(result));
    }

    public async Task<IActionResult> GetAllAsync(HttpRequest req)
    {
        string? search = req.Query["search"];

        // 有分頁參數 → 回傳 PagedResult；無分頁參數 → 回傳平面陣列（供下拉選單用）
        if (req.Query.ContainsKey("page") || req.Query.ContainsKey("pageSize"))
        {
            int page     = int.TryParse(req.Query["page"],     out var p)  ? Math.Max(1, p)         : 1;
            int pageSize = int.TryParse(req.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, 100) : 20;
            var result = await reader.GetPagedAsync(page, pageSize, search);
            return new OkObjectResult(ApiResponse.Ok(result));
        }

        var vendors = await reader.GetAllAsync(search);
        return new OkObjectResult(ApiResponse.Ok(vendors));
    }

    public async Task<IActionResult> GetByIdAsync(string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid vendor ID format."));

        var vendor = await reader.GetByIdAsync(intId);
        return vendor is null
            ? new NotFoundObjectResult(ApiResponse.Fail("Vendor not found.", $"No vendor with id '{id}'."))
            : new OkObjectResult(ApiResponse.Ok(vendor));
    }

    // POST /api/vendors — multipart：text part `payload` + 檔案 bankBookImage / idCardFront / idCardBack
    public async Task<IActionResult> CreateAsync(HttpRequest req)
    {
        var mp = await ReadMultipartAsync<CreateVendorRequest>(req);
        var body = mp.Body;
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        var name = body.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return new BadRequestObjectResult(ApiResponse.Fail("廠商名稱為必填。"));

        var taxId    = string.IsNullOrWhiteSpace(body.TaxId)    ? null : body.TaxId.Trim();
        var idNumber = string.IsNullOrWhiteSpace(body.IdNumber) ? null : body.IdNumber.Trim();

        if (ValidateIdentifier(taxId, idNumber) is { } idErr)
            return new BadRequestObjectResult(ApiResponse.Fail(idErr));

        if (taxId is not null && await db.Vendors.AnyAsync(v => v.TaxId == taxId))
            return new BadRequestObjectResult(ApiResponse.Fail($"統編「{taxId}」已存在。"));
        if (idNumber is not null && await db.Vendors.AnyAsync(v => v.IdNumber == idNumber))
            return new BadRequestObjectResult(ApiResponse.Fail($"身分證字號「{idNumber}」已存在。"));

        // 存摺封面為必填
        if (mp.BankBookFile is null)
            return new BadRequestObjectResult(ApiResponse.Fail("存摺封面為必填。"));

        // 個人工作室（身分證字號）須上傳身分證正反面
        if (idNumber is not null && (mp.IdCardFrontFile is null || mp.IdCardBackFile is null))
            return new BadRequestObjectResult(ApiResponse.Fail("個人工作室須上傳身分證正反面。"));

        var vendor = new Vendor
        {
            Name          = name,
            TaxId         = taxId,
            IdNumber      = idNumber,
            Phone         = string.IsNullOrWhiteSpace(body.Phone)         ? null : body.Phone.Trim(),
            ContactPerson = string.IsNullOrWhiteSpace(body.ContactPerson) ? null : body.ContactPerson.Trim(),
            Address       = string.IsNullOrWhiteSpace(body.Address)       ? null : body.Address.Trim(),
            BankAccount   = string.IsNullOrWhiteSpace(body.BankAccount)   ? null : body.BankAccount.Trim(),
            Note          = string.IsNullOrWhiteSpace(body.Note)          ? null : body.Note.Trim(),
            IsActive      = body.IsActive,
            CreatedAt     = Clock.Now,
        };
        db.Vendors.Add(vendor);
        await db.SaveChangesAsync();

        // 取得 Id 後再上傳檔案（與 EmployeeProfile 同模式：blob 命名以 entity Id 為主鍵）
        vendor.BankBookImageUrl = await UploadBankBookAsync(vendor.Id, mp.BankBookFile);
        if (idNumber is not null)
        {
            vendor.IdCardFrontUrl = await UploadIdCardAsync(vendor.Id, "front", mp.IdCardFrontFile!);
            vendor.IdCardBackUrl  = await UploadIdCardAsync(vendor.Id, "back",  mp.IdCardBackFile!);
        }
        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(vendor.Id);
        return new ObjectResult(ApiResponse.Ok(dto, "Vendor created.")) { StatusCode = 201 };
    }

    // PATCH/PUT /api/vendors/{id} — multipart：text part `payload` + optional file/remove flags
    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid vendor ID format."));

        var mp = await ReadMultipartAsync<UpdateVendorRequest>(req);
        var body = mp.Body;
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        var vendor = await db.Vendors.FindAsync(intId)
            ?? throw AppException.NotFound("Vendor");

        if (body.Name is not null)
        {
            var name = body.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return new BadRequestObjectResult(ApiResponse.Fail("廠商名稱不可為空。"));
            vendor.Name = name;
        }

        // 識別碼（統編 / 身分證字號）擇一，整組原子更新：只要表單帶了任一個就重設兩欄。
        // 兩者皆空 → 視為未觸及識別碼（例如僅切換 IsActive 的部分更新），不變動。
        var newTaxId    = string.IsNullOrWhiteSpace(body.TaxId)    ? null : body.TaxId.Trim();
        var newIdNumber = string.IsNullOrWhiteSpace(body.IdNumber) ? null : body.IdNumber.Trim();
        if (newTaxId is not null || newIdNumber is not null)
        {
            if (ValidateIdentifier(newTaxId, newIdNumber) is { } idErr)
                return new BadRequestObjectResult(ApiResponse.Fail(idErr));

            if (newTaxId is not null && newTaxId != vendor.TaxId
                && await db.Vendors.AnyAsync(v => v.Id != intId && v.TaxId == newTaxId))
                return new BadRequestObjectResult(ApiResponse.Fail($"統編「{newTaxId}」已存在。"));
            if (newIdNumber is not null && newIdNumber != vendor.IdNumber
                && await db.Vendors.AnyAsync(v => v.Id != intId && v.IdNumber == newIdNumber))
                return new BadRequestObjectResult(ApiResponse.Fail($"身分證字號「{newIdNumber}」已存在。"));

            vendor.TaxId    = newTaxId;
            vendor.IdNumber = newIdNumber;
        }

        if (body.Phone         is not null) vendor.Phone         = string.IsNullOrWhiteSpace(body.Phone)         ? null : body.Phone.Trim();
        if (body.ContactPerson is not null) vendor.ContactPerson = string.IsNullOrWhiteSpace(body.ContactPerson) ? null : body.ContactPerson.Trim();
        if (body.Address       is not null) vendor.Address       = string.IsNullOrWhiteSpace(body.Address)       ? null : body.Address.Trim();
        if (body.BankAccount   is not null) vendor.BankAccount   = string.IsNullOrWhiteSpace(body.BankAccount)   ? null : body.BankAccount.Trim();
        if (body.Note          is not null) vendor.Note          = string.IsNullOrWhiteSpace(body.Note)          ? null : body.Note.Trim();
        if (body.IsActive.HasValue)         vendor.IsActive      = body.IsActive.Value;

        // 存摺封面為必填：更新後須為既有（未刪）或本次有上傳
        var bankBookWillExist = mp.BankBookFile is not null || (vendor.BankBookImageUrl is not null && !mp.RemoveBankBook);
        if (!bankBookWillExist)
            return new BadRequestObjectResult(ApiResponse.Fail("存摺封面為必填。"));

        // 個人工作室須備齊身分證正反面
        if (vendor.IdNumber is not null)
        {
            var frontWillExist = mp.IdCardFrontFile is not null || (vendor.IdCardFrontUrl is not null && !mp.RemoveIdCardFront);
            var backWillExist  = mp.IdCardBackFile  is not null || (vendor.IdCardBackUrl  is not null && !mp.RemoveIdCardBack);
            if (!frontWillExist || !backWillExist)
                return new BadRequestObjectResult(ApiResponse.Fail("個人工作室須上傳身分證正反面。"));
        }

        // 存摺封面：刪除 / 上傳（上傳優先）
        await ApplyFileChangeAsync(BankBookContainer, mp.BankBookFile, mp.RemoveBankBook,
            () => UploadBankBookAsync(vendor.Id, mp.BankBookFile!),
            () => vendor.BankBookImageUrl, url => vendor.BankBookImageUrl = url);

        if (vendor.IdNumber is null)
        {
            // 轉為公司（統編）：清掉不再適用的身分證影本
            await TryDeleteBlobByUrlAsync(IdCardContainer, vendor.IdCardFrontUrl);
            await TryDeleteBlobByUrlAsync(IdCardContainer, vendor.IdCardBackUrl);
            vendor.IdCardFrontUrl = null;
            vendor.IdCardBackUrl  = null;
        }
        else
        {
            await ApplyFileChangeAsync(IdCardContainer, mp.IdCardFrontFile, mp.RemoveIdCardFront,
                () => UploadIdCardAsync(vendor.Id, "front", mp.IdCardFrontFile!),
                () => vendor.IdCardFrontUrl, url => vendor.IdCardFrontUrl = url);
            await ApplyFileChangeAsync(IdCardContainer, mp.IdCardBackFile, mp.RemoveIdCardBack,
                () => UploadIdCardAsync(vendor.Id, "back", mp.IdCardBackFile!),
                () => vendor.IdCardBackUrl, url => vendor.IdCardBackUrl = url);
        }

        await db.SaveChangesAsync();

        var dto = await reader.GetByIdAsync(vendor.Id);
        return new OkObjectResult(ApiResponse.Ok(dto, "Vendor updated."));
    }

    public async Task<IActionResult> DeleteAsync(string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid vendor ID format."));

        var vendor = await db.Vendors.FindAsync(intId)
            ?? throw AppException.NotFound("Vendor");

        if (await db.PaymentRequests.AnyAsync(p => p.VendorId == intId))
            return new BadRequestObjectResult(ApiResponse.Fail(
                "此廠商已被請款單引用，無法刪除。請改用「停用」（將 IsActive 設為 false）。"));

        // 刪除廠商前一併移除存摺封面與身分證影本 blob
        await TryDeleteBlobByUrlAsync(BankBookContainer, vendor.BankBookImageUrl);
        await TryDeleteBlobByUrlAsync(IdCardContainer, vendor.IdCardFrontUrl);
        await TryDeleteBlobByUrlAsync(IdCardContainer, vendor.IdCardBackUrl);

        db.Vendors.Remove(vendor);
        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok($"Vendor '{id}' deleted."));
    }

    // ────────────────────────────────────────────────────────────────
    // 私有輔助
    // ────────────────────────────────────────────────────────────────

    /// <summary>解析後的 multipart 內容：payload 物件 + 三組檔案（存摺 / 身分證正 / 身分證反）與各自的刪除旗標。</summary>
    private sealed record VendorMultipart<T>(
        T?         Body,
        IFormFile? BankBookFile,    bool RemoveBankBook,
        IFormFile? IdCardFrontFile, bool RemoveIdCardFront,
        IFormFile? IdCardBackFile,  bool RemoveIdCardBack) where T : class;

    /// <summary>
    /// 讀取 multipart：text part `payload` 反序列化為 <typeparamref name="T"/>，
    /// 加上 file part `bankBookImage` / `idCardFront` / `idCardBack` 與對應的 remove 旗標（皆 optional）。
    /// 也支援舊版 JSON body（沒上傳檔案、純文字 PATCH）以維持相容性。
    /// </summary>
    private static async Task<VendorMultipart<T>> ReadMultipartAsync<T>(HttpRequest req) where T : class
    {
        // 兼容純 JSON（無 multipart）的呼叫
        if (!req.HasFormContentType)
        {
            var body = await req.ReadFromJsonAsync<T>();
            return new VendorMultipart<T>(body, null, false, null, false, null, false);
        }

        var form        = await req.ReadFormAsync();
        var payloadJson = form["payload"].ToString();
        if (string.IsNullOrWhiteSpace(payloadJson))
            return new VendorMultipart<T>(null, null, false, null, false, null, false);

        T? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<T>(payloadJson, PayloadJsonOptions);
        }
        catch (JsonException)
        {
            return new VendorMultipart<T>(null, null, false, null, false, null, false);
        }

        return new VendorMultipart<T>(
            parsed,
            PickFile(form, "bankBookImage"), form["removeBankBookImage"].ToString() == "true",
            PickFile(form, "idCardFront"),   form["removeIdCardFront"].ToString()   == "true",
            PickFile(form, "idCardBack"),    form["removeIdCardBack"].ToString()    == "true");
    }

    private static IFormFile? PickFile(IFormCollection form, string name)
    {
        var file = form.Files.GetFile(name);
        return file is not null && file.Length == 0 ? null : file;
    }

    /// <summary>
    /// 驗證並上傳存摺封面到 Blob，回傳前端可用的 proxy 路徑。
    /// 命名規則：{vendorId}{ext}，同 Id 上傳會自動覆蓋舊檔。
    /// </summary>
    private Task<string> UploadBankBookAsync(int vendorId, IFormFile file)
        => UploadFileAsync(BankBookContainer, $"{vendorId}", file, "存摺封面");

    /// <summary>
    /// 驗證並上傳身分證影本到 Blob，回傳 proxy 路徑。
    /// 命名規則：{vendorId}_{side}{ext}（side = front / back）。
    /// </summary>
    private Task<string> UploadIdCardAsync(int vendorId, string side, IFormFile file)
        => UploadFileAsync(IdCardContainer, $"{vendorId}_{side}", file, "身分證影本");

    /// <summary>共用：驗證大小 / magic bytes 後上傳，回傳 proxy 路徑。</summary>
    private async Task<string> UploadFileAsync(string container, string baseName, IFormFile file, string label)
    {
        if (file.Length > 1 * 1024 * 1024)
            throw AppException.BadRequest("上傳照片勿超過1MB");

        string? actualType;
        using (var peek = file.OpenReadStream())
            actualType = await FileSignatureValidator.DetectAsync(peek);

        if (actualType is null || !AllowedFileTypes.Contains(actualType))
            throw AppException.BadRequest($"{label}僅支援 PNG、JPEG 圖片或 PDF 格式。");

        var ext      = Path.GetExtension(file.FileName);
        var blobName = $"{baseName}{ext}";

        using (var stream = file.OpenReadStream())
            await blob.UploadAsync(container, blobName, stream, actualType);

        return $"files/{container}/{blobName}";
    }

    /// <summary>驗證統編 / 身分證字號擇一且格式正確；回傳錯誤訊息或 null（通過）。</summary>
    private static string? ValidateIdentifier(string? taxId, string? idNumber)
    {
        if (taxId is null && idNumber is null)
            return "請填寫統編或身分證字號。";
        if (taxId is not null && idNumber is not null)
            return "統編與身分證字號僅能擇一填寫。";
        if (taxId is not null && !TaxIdPattern.IsMatch(taxId))
            return "統編格式錯誤，須為 8 位數字。";
        if (idNumber is not null && !IdNumberPattern.IsMatch(idNumber))
            return "身分證字號格式錯誤。";
        return null;
    }

    /// <summary>單一檔案欄位的刪除 / 上傳處理（上傳優先；換檔時刪舊 blob）。</summary>
    private async Task ApplyFileChangeAsync(
        string container, IFormFile? file, bool remove,
        Func<Task<string>> upload, Func<string?> getUrl, Action<string?> setUrl)
    {
        if (file is not null)
        {
            var newUrl = await upload();
            if (!string.Equals(getUrl(), newUrl, StringComparison.OrdinalIgnoreCase))
                await TryDeleteBlobByUrlAsync(container, getUrl());
            setUrl(newUrl);
        }
        else if (remove)
        {
            await TryDeleteBlobByUrlAsync(container, getUrl());
            setUrl(null);
        }
    }

    /// <summary>嘗試刪除 Blob；失敗只忽略（孤兒檔案可事後清理）。</summary>
    private async Task TryDeleteBlobByUrlAsync(string container, string? url)
    {
        if (string.IsNullOrEmpty(url)) return;
        try
        {
            var proxyPrefix = $"files/{container}/";
            var blobName = url.StartsWith(proxyPrefix, StringComparison.OrdinalIgnoreCase)
                ? url[proxyPrefix.Length..]
                : blob.ExtractBlobName(url, container);
            if (blobName is not null)
                await blob.DeleteAsync(container, blobName);
        }
        catch (Exception)
        {
            // 孤兒檔案：可接受
        }
    }
}
