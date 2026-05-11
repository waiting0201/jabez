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
    private static readonly string[] AllowedBankBookTypes = ["image/png", "image/jpeg", "application/pdf"];
    private static readonly Regex TaxIdPattern = new(@"^\d{8}$", RegexOptions.Compiled);

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

    public async Task<IActionResult> GetAllAsync()
    {
        var vendors = await reader.GetAllAsync();
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

    // POST /api/vendors — multipart：text part `payload` + optional file part `bankBookImage`
    public async Task<IActionResult> CreateAsync(HttpRequest req)
    {
        var (body, bankBookFile, _) = await ReadMultipartAsync<CreateVendorRequest>(req);
        if (body is null)
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid request body."));

        var name = body.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return new BadRequestObjectResult(ApiResponse.Fail("廠商名稱為必填。"));

        var taxId = string.IsNullOrWhiteSpace(body.TaxId) ? null : body.TaxId.Trim();

        if (taxId is not null && await db.Vendors.AnyAsync(v => v.TaxId == taxId))
            return new BadRequestObjectResult(ApiResponse.Fail($"統編「{taxId}」已存在。"));

        var vendor = new Vendor
        {
            Name          = name,
            TaxId         = taxId,
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

        // 取得 Id 後再上傳存摺封面（與 EmployeeProfile 同模式：blob 命名以 entity Id 為主鍵）
        if (bankBookFile is not null)
        {
            var newUrl = await UploadBankBookAsync(vendor.Id, bankBookFile);
            vendor.BankBookImageUrl = newUrl;
            await db.SaveChangesAsync();
        }

        var dto = await reader.GetByIdAsync(vendor.Id);
        return new ObjectResult(ApiResponse.Ok(dto, "Vendor created.")) { StatusCode = 201 };
    }

    // PATCH/PUT /api/vendors/{id} — multipart：text part `payload` + optional file/remove flags
    public async Task<IActionResult> UpdateAsync(HttpRequest req, string id)
    {
        if (!int.TryParse(id, out var intId))
            return new BadRequestObjectResult(ApiResponse.Fail("Invalid vendor ID format."));

        var (body, bankBookFile, removeBankBook) = await ReadMultipartAsync<UpdateVendorRequest>(req);
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

        if (body.TaxId is not null)
        {
            var taxId = string.IsNullOrWhiteSpace(body.TaxId) ? null : body.TaxId.Trim();
            if (taxId != vendor.TaxId
                && taxId is not null
                && await db.Vendors.AnyAsync(v => v.Id != intId && v.TaxId == taxId))
                return new BadRequestObjectResult(ApiResponse.Fail($"統編「{taxId}」已存在。"));
            vendor.TaxId = taxId;
        }

        if (body.Phone         is not null) vendor.Phone         = string.IsNullOrWhiteSpace(body.Phone)         ? null : body.Phone.Trim();
        if (body.ContactPerson is not null) vendor.ContactPerson = string.IsNullOrWhiteSpace(body.ContactPerson) ? null : body.ContactPerson.Trim();
        if (body.Address       is not null) vendor.Address       = string.IsNullOrWhiteSpace(body.Address)       ? null : body.Address.Trim();
        if (body.BankAccount   is not null) vendor.BankAccount   = string.IsNullOrWhiteSpace(body.BankAccount)   ? null : body.BankAccount.Trim();
        if (body.Note          is not null) vendor.Note          = string.IsNullOrWhiteSpace(body.Note)          ? null : body.Note.Trim();
        if (body.IsActive.HasValue)         vendor.IsActive      = body.IsActive.Value;

        // 存摺封面：先處理刪除，再處理上傳（兩者互斥，上傳優先）
        if (removeBankBook && bankBookFile is null)
        {
            await TryDeleteBlobByUrlAsync(BankBookContainer, vendor.BankBookImageUrl);
            vendor.BankBookImageUrl = null;
        }
        else if (bankBookFile is not null)
        {
            var newUrl = await UploadBankBookAsync(vendor.Id, bankBookFile);
            if (!string.Equals(vendor.BankBookImageUrl, newUrl, StringComparison.OrdinalIgnoreCase))
                await TryDeleteBlobByUrlAsync(BankBookContainer, vendor.BankBookImageUrl);
            vendor.BankBookImageUrl = newUrl;
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

        // 刪除廠商前一併移除存摺封面 blob
        await TryDeleteBlobByUrlAsync(BankBookContainer, vendor.BankBookImageUrl);

        db.Vendors.Remove(vendor);
        await db.SaveChangesAsync();

        return new OkObjectResult(ApiResponse.Ok($"Vendor '{id}' deleted."));
    }

    // ────────────────────────────────────────────────────────────────
    // 私有輔助
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 讀取 multipart：text part `payload` 反序列化為 <typeparamref name="T"/>，
    /// 加上 file part `bankBookImage` 與 `removeBankBookImage` 旗標（optional）。
    /// 也支援舊版 JSON body（沒上傳檔案、純文字 PATCH）以維持相容性。
    /// </summary>
    private static async Task<(T? Body, IFormFile? BankBookFile, bool RemoveBankBook)> ReadMultipartAsync<T>(HttpRequest req) where T : class
    {
        // 兼容純 JSON（無 multipart）的呼叫
        if (!req.HasFormContentType)
        {
            var body = await req.ReadFromJsonAsync<T>();
            return (body, null, false);
        }

        var form        = await req.ReadFormAsync();
        var payloadJson = form["payload"].ToString();
        if (string.IsNullOrWhiteSpace(payloadJson))
            return (null, null, false);

        T? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<T>(payloadJson, PayloadJsonOptions);
        }
        catch (JsonException)
        {
            return (null, null, false);
        }

        var file = form.Files.GetFile("bankBookImage");
        if (file is not null && file.Length == 0) file = null;

        var remove = form["removeBankBookImage"].ToString() == "true";

        return (parsed, file, remove);
    }

    /// <summary>
    /// 驗證並上傳存摺封面到 Blob，回傳前端可用的 proxy 路徑。
    /// 命名規則：{vendorId}{ext}，同 Id 上傳會自動覆蓋舊檔。
    /// </summary>
    private async Task<string> UploadBankBookAsync(int vendorId, IFormFile file)
    {
        if (file.Length > 1 * 1024 * 1024)
            throw AppException.BadRequest("上傳照片勿超過1MB");

        string? actualType;
        using (var peek = file.OpenReadStream())
            actualType = await FileSignatureValidator.DetectAsync(peek);

        if (actualType is null || !AllowedBankBookTypes.Contains(actualType))
            throw AppException.BadRequest("存摺封面僅支援 PNG、JPEG 圖片或 PDF 格式。");

        var ext      = Path.GetExtension(file.FileName);
        var blobName = $"{vendorId}{ext}";

        using (var stream = file.OpenReadStream())
            await blob.UploadAsync(BankBookContainer, blobName, stream, actualType);

        return $"files/{BankBookContainer}/{blobName}";
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
