using Jabez.Api.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Jabez.Api.Handlers;

/// <summary>
/// 發票 OCR Handler：接收圖片並透過 Google Gemini API 辨識台灣統一發票號碼與金額。
/// </summary>
public sealed class InvoiceOcrHandler(IConfiguration config)
{
    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private const string DefaultModel = "gemini-2.0-flash-lite-001";
    private const string ApiBase = "https://generativelanguage.googleapis.com/v1beta/models";

    // 允許的 MIME 類型（圖片 + PDF）
    private static readonly HashSet<string> AllowedMediaTypes =
        ["image/jpeg", "image/png", "image/gif", "image/webp", "application/pdf"];

    private static readonly JsonSerializerOptions CamelOpts = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// POST /invoice-ocr
    /// 接收 multipart/form-data（欄位 "file"），呼叫 Gemini API 辨識發票資訊。
    /// </summary>
    public async Task<IActionResult> RecognizeAsync(HttpRequest req)
    {
        // ── 1. 讀取設定 ────────────────────────────────────────────────────────
        var model = config["Google:Model"] ?? DefaultModel;
        var apiKey = config["Google:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return new ObjectResult(ApiResponse.Fail("Google API Key 未設定。"))
                { StatusCode = 503 };

        // ── 2. 解析 multipart/form-data ────────────────────────────────────────
        if (!req.HasFormContentType)
            return new BadRequestObjectResult(
                ApiResponse.Fail("Content-Type 必須為 multipart/form-data。"));

        IFormFile? file;
        try
        {
            var form = await req.ReadFormAsync();
            file = form.Files.GetFile("file");
        }
        catch (Exception ex)
        {
            return new BadRequestObjectResult(
                ApiResponse.Fail($"無法解析上傳表單：{ex.Message}"));
        }

        if (file is null || file.Length == 0)
            return new BadRequestObjectResult(
                ApiResponse.Fail("請上傳圖片或 PDF 檔案（欄位名稱：file）。"));

        // ── 3. 驗證檔案類型與大小 ───────────────────────────────────────────────
        var mediaType = file.ContentType?.ToLowerInvariant() ?? "image/jpeg";
        if (!AllowedMediaTypes.Contains(mediaType))
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            mediaType = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png"            => "image/png",
                ".gif"            => "image/gif",
                ".webp"           => "image/webp",
                ".pdf"            => "application/pdf",
                _                 => string.Empty
            };
            if (string.IsNullOrEmpty(mediaType))
                return new BadRequestObjectResult(
                    ApiResponse.Fail("不支援的檔案格式。請上傳 JPEG、PNG、GIF、WebP 圖片或 PDF。"));
        }

        const long maxBytes = 5 * 1024 * 1024;
        if (file.Length > maxBytes)
            return new BadRequestObjectResult(
                ApiResponse.Fail("圖片檔案大小不得超過 5 MB。"));

        // ── 4. 讀取圖片並轉換為 Base64 ──────────────────────────────────────────
        string base64Data;
        try
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            base64Data = Convert.ToBase64String(ms.ToArray());
        }
        catch (Exception ex)
        {
            return new ObjectResult(ApiResponse.Fail($"讀取圖片失敗：{ex.Message}"))
                { StatusCode = 500 };
        }

        // ── 5. 組建 Gemini API 請求 ──────────────────────────────────────────
        var prompt = """
            請辨識這張圖片或 PDF，它可能是以下其中一種：
            A. 台灣統一發票 / 收據
            B. 交通票根（高鐵、台鐵、捷運、客運、機票 / 登機證、計程車收據、停車費收據、ETC 通行費等）

            請依下列規則提取 4 個欄位，並以 JSON 格式回覆（不要加任何其他文字、不要 markdown）：

            - docType：文件類型
              * 若為 A 類（統一發票 / 收據）：填 "invoice"
              * 若為 B 類（交通票根）：填 "ticket"

            【若為台灣統一發票 / 收據】
            - invoiceNo：發票號碼（格式為 2 個英文大寫字母 + 8 個數字，例如 AB12345678）
            - amount：總金額 / 合計 / 應付金額（純數字，無則填 0）
            - invoiceDate：發票日期（西元 YYYY-MM-DD；若為民國年如「113 年 01 月 15 日」或「113/01/15」請轉為西元）

            【若為交通票根】
            - invoiceNo：票號 / 車票號碼 / 訂位代號 / 序號（保留完整英數字，不做格式限制）
              * 特別規則：若為「高鐵票（台灣高鐵 THSR）」，票號請**移除所有 dash（「-」）符號**，僅保留 13 碼純數字
            - amount：票價 / 金額（純數字，票券未印金額則填 0）
            - invoiceDate：搭乘日期 / 乘車日期 / 航班日期（西元 YYYY-MM-DD；民國年請轉為西元；去回程票以去程日期為準）

            找不到的欄位：字串欄位填空字串 ""、金額填 0。
            無法判別文件類型時：docType 填 "invoice"。

            回覆格式（僅此一行 JSON，無任何多餘文字）：
            {"docType": "invoice|ticket", "invoiceNo": "...", "amount": 0, "invoiceDate": "YYYY-MM-DD 或空字串"}
            """;

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new
                        {
                            inline_data = new
                            {
                                mime_type = mediaType,
                                data = base64Data
                            }
                        },
                        new
                        {
                            text = prompt
                        }
                    }
                }
            }
        };

        var jsonBody = JsonSerializer.Serialize(requestBody);

        // Log 送出的 JSON（去掉 base64 data 避免 log 爆量）
        var logJson = Regex.Replace(jsonBody, @"""data""\s*:\s*""[^""]+""", @"""data"":""[BASE64_TRUNCATED]""");
        Console.WriteLine($"[InvoiceOcr] Request JSON: {logJson}");

        // ── 6. 呼叫 Gemini API ──────────────────────────────────────────────
        GeminiResponse? geminiResp;
        var apiUrl = $"{ApiBase}/{model}:generateContent?key={apiKey}";
        try
        {
            using var httpReq = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            httpReq.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            Console.WriteLine($"[InvoiceOcr] Calling Gemini API: model={model}");
            using var httpResp = await _http.SendAsync(httpReq);
            var respBody = await httpResp.Content.ReadAsStringAsync();

            if (!httpResp.IsSuccessStatusCode)
            {
                Console.Error.WriteLine($"[InvoiceOcr] Gemini API error {(int)httpResp.StatusCode}: {respBody}");
                return new ObjectResult(
                    ApiResponse.Fail($"AI 服務暫時無法使用（{(int)httpResp.StatusCode}），請稍後再試。"))
                    { StatusCode = 502 };
            }

            geminiResp = JsonSerializer.Deserialize<GeminiResponse>(respBody, CamelOpts);
        }
        catch (TaskCanceledException)
        {
            return new ObjectResult(ApiResponse.Fail("AI 服務請求逾時，請稍後再試。"))
                { StatusCode = 504 };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[InvoiceOcr] HTTP error: {ex.Message}");
            return new ObjectResult(ApiResponse.Fail("呼叫 AI 服務時發生錯誤，請稍後再試。"))
                { StatusCode = 500 };
        }

        // ── 7. 解析 Gemini 回傳文字 ──────────────────────────────────────────
        var rawText = geminiResp?.Candidates?.FirstOrDefault()
                          ?.Content?.Parts?.FirstOrDefault(p => !string.IsNullOrEmpty(p.Text))
                          ?.Text ?? string.Empty;

        var cleanedText = CleanJsonText(rawText);

        OcrResult ocrResult;
        try
        {
            ocrResult = JsonSerializer.Deserialize<OcrResult>(cleanedText, CamelOpts)
                        ?? new OcrResult(string.Empty, 0, string.Empty);
        }
        catch
        {
            ocrResult = FallbackExtract(rawText);
        }

        ocrResult = NormalizeInvoiceNo(ocrResult);

        return new OkObjectResult(ApiResponse.Ok(ocrResult, "發票辨識成功。"));
    }

    // ── 私有輔助方法 ──────────────────────────────────────────────────────────

    /// <summary>移除 LLM 有時包裹的 markdown code fence（```json ... ```）</summary>
    private static string CleanJsonText(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var start = trimmed.IndexOf('\n');
            var end   = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (start >= 0 && end > start)
                return trimmed[(start + 1)..end].Trim();
        }
        return trimmed;
    }

    /// <summary>
    /// 後處理：高鐵票號若為「含 dash 的 13 碼純數字」，移除所有 dash（含全形「－」）。
    /// 對統一發票（AB12345678）等其他格式無副作用。
    /// </summary>
    private static OcrResult NormalizeInvoiceNo(OcrResult r)
    {
        if (string.IsNullOrEmpty(r.InvoiceNo)) return r;
        var noDash = r.InvoiceNo.Replace("-", string.Empty).Replace("－", string.Empty);
        if (noDash.Length == 13 && noDash.All(char.IsDigit) && noDash != r.InvoiceNo)
            return r with { InvoiceNo = noDash };
        return r;
    }

    /// <summary>JSON 解析失敗時的備援萃取</summary>
    private static OcrResult FallbackExtract(string text)
    {
        var invoiceNo = string.Empty;
        decimal amount = 0;
        var invoiceDate = string.Empty;
        var docType = "invoice";

        // 嘗試從 JSON 文字中萃取 docType
        var docTypeMatch = Regex.Match(text, @"""docType""\s*:\s*""(invoice|ticket)""");
        if (docTypeMatch.Success)
            docType = docTypeMatch.Groups[1].Value;

        // 優先：直接從 JSON 字串萃取 invoiceNo 欄位（支援任意格式的票號，例如交通票根）
        var invoiceNoJsonMatch = Regex.Match(text, @"""invoiceNo""\s*:\s*""([^""]*)""");
        if (invoiceNoJsonMatch.Success)
        {
            invoiceNo = invoiceNoJsonMatch.Groups[1].Value;
        }
        else
        {
            // Fallback：嘗試統一發票格式
            var invoiceMatch = Regex.Match(text, @"[A-Z]{2}\d{8}");
            if (invoiceMatch.Success)
                invoiceNo = invoiceMatch.Value;
        }

        var amountMatch = Regex.Match(text, @"""amount""\s*:\s*(\d+(?:\.\d+)?)");
        if (amountMatch.Success)
            decimal.TryParse(amountMatch.Groups[1].Value, out amount);

        // 嘗試萃取日期（西元 YYYY-MM-DD）
        var dateMatch = Regex.Match(text, @"\d{4}-\d{2}-\d{2}");
        if (dateMatch.Success)
        {
            invoiceDate = dateMatch.Value;
        }
        else
        {
            // 嘗試民國年格式：111年01月15日 或 111/01/15
            var rocMatch = Regex.Match(text, @"(\d{2,3})\s*[年/]\s*(\d{1,2})\s*[月/]\s*(\d{1,2})");
            if (rocMatch.Success &&
                int.TryParse(rocMatch.Groups[1].Value, out var rocYear) &&
                int.TryParse(rocMatch.Groups[2].Value, out var month) &&
                int.TryParse(rocMatch.Groups[3].Value, out var day))
            {
                var adYear = rocYear + 1911;
                invoiceDate = $"{adYear:D4}-{month:D2}-{day:D2}";
            }
        }

        return new OcrResult(invoiceNo, amount, invoiceDate, docType);
    }

    // ── 內部 DTO ──────────────────────────────────────────────────────────────

    private sealed record OcrResult(
        [property: JsonPropertyName("invoiceNo")]    string  InvoiceNo,
        [property: JsonPropertyName("amount")]       decimal Amount,
        [property: JsonPropertyName("invoiceDate")]  string  InvoiceDate = "",
        [property: JsonPropertyName("docType")]      string  DocType = "invoice");

    /// <summary>Gemini API 回應結構</summary>
    private sealed class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    private sealed class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; set; }
    }

    private sealed class GeminiContent
    {
        [JsonPropertyName("parts")]
        public List<GeminiPart>? Parts { get; set; }
    }

    private sealed class GeminiPart
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
