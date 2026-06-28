using Jabez.Api.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Jabez.Api.Handlers;

/// <summary>
/// 報價單 OCR Handler：接收圖片並透過 Google Gemini API 辨識報價單 / 估價單行項目。
/// 回傳結果為陣列（每個行項目一筆），包含品項名稱、金額、備註。
/// </summary>
public sealed class QuoteOcrHandler(IConfiguration config, ILogger<QuoteOcrHandler> logger)
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
    /// POST /quote-ocr
    /// 接收 multipart/form-data（欄位 "file"），呼叫 Gemini API 辨識報價單行項目。
    /// 回傳 ApiResponse&lt;OcrQuoteItemResult[]&gt;（一張圖可辨識出多筆行項目）。
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
            請辨識這張圖片或 PDF，這是一份**報價單 / 估價單**，請找出其中**所有**的報價行項目（同一份文件可能包含多個品項，請逐一辨識，不要遺漏）。

            請為每一個行項目各提取以下欄位，組成一個 JSON 物件，所有物件放入一個 JSON 陣列回覆（不要加任何其他文字、不要 markdown）：

            - itemName：品項名稱 / 服務項目名稱（完整描述，不要截斷）
            - amount：此行項目的金額（純整數，無則填 0）
              * 優先序：含稅金額 > 未稅金額 > 小計
              * 若有折扣，以折扣後金額為準
            - note：備註（規格、數量、單位、單價等補充說明，組合成一個字串；沒有則填 ""）

            注意事項：
            - 不要包含合計列 / 總金額列 / 稅金列（只要明細行項目）
            - 若文件非報價單 / 估價單，或完全找不到任何行項目，回傳空陣列 []

            回覆格式（僅此一行 JSON 陣列，無任何多餘文字）：
            [{"itemName": "...", "amount": 0, "note": ""}]
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
        logger.LogDebug("QuoteOcr request JSON: {Body}", logJson);

        // ── 6. 呼叫 Gemini API ──────────────────────────────────────────────
        GeminiResponse? geminiResp;
        var apiUrl = $"{ApiBase}/{model}:generateContent?key={apiKey}";
        try
        {
            using var httpReq = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            httpReq.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            logger.LogInformation("QuoteOcr calling Gemini API: model={Model}", model);
            using var httpResp = await _http.SendAsync(httpReq);
            var respBody = await httpResp.Content.ReadAsStringAsync();

            if (!httpResp.IsSuccessStatusCode)
            {
                logger.LogError("QuoteOcr Gemini API error {Status}: {Body}", (int)httpResp.StatusCode, respBody);
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
            logger.LogError(ex, "QuoteOcr HTTP error");
            return new ObjectResult(ApiResponse.Fail("呼叫 AI 服務時發生錯誤，請稍後再試。"))
                { StatusCode = 500 };
        }

        // ── 7. 解析 Gemini 回傳文字 ──────────────────────────────────────────
        var rawText = geminiResp?.Candidates?.FirstOrDefault()
                          ?.Content?.Parts?.FirstOrDefault(p => !string.IsNullOrEmpty(p.Text))
                          ?.Text ?? string.Empty;

        var cleanedText = CleanJsonText(rawText);

        OcrQuoteItemResult[] ocrResults;
        try
        {
            ocrResults = JsonSerializer.Deserialize<OcrQuoteItemResult[]>(cleanedText, CamelOpts)
                        ?? [];
        }
        catch
        {
            ocrResults = FallbackExtract(rawText);
        }

        return new OkObjectResult(ApiResponse.Ok(ocrResults, "報價單辨識成功。"));
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
    /// JSON 解析失敗時的備援萃取：嘗試從文字中切出每一個 JSON 物件 {...} 各別萃取；
    /// 切不出物件時，退而對整段文字做單筆萃取。完全抽不到內容則回傳空陣列。
    /// </summary>
    private static OcrQuoteItemResult[] FallbackExtract(string text)
    {
        var blocks = Regex.Matches(text, @"\{[^{}]*\}")
                          .Select(m => m.Value)
                          .ToList();

        var sources = blocks.Count > 0 ? blocks : [text];

        var results = sources
            .Select(ExtractOne)
            .Where(r => r is not null)
            .Select(r => r!)
            .ToArray();

        return results;
    }

    /// <summary>從單一文字片段萃取一筆 OcrQuoteItemResult；完全無有效內容時回傳 null。</summary>
    private static OcrQuoteItemResult? ExtractOne(string text)
    {
        var itemName = string.Empty;
        decimal amount = 0;
        var note = string.Empty;

        var itemNameMatch = Regex.Match(text, @"""itemName""\s*:\s*""([^""]*)""");
        if (itemNameMatch.Success)
            itemName = itemNameMatch.Groups[1].Value;

        var amountMatch = Regex.Match(text, @"""amount""\s*:\s*(\d+(?:\.\d+)?)");
        if (amountMatch.Success)
            decimal.TryParse(amountMatch.Groups[1].Value, out amount);

        var noteMatch = Regex.Match(text, @"""note""\s*:\s*""([^""]*)""");
        if (noteMatch.Success)
            note = noteMatch.Groups[1].Value;

        if (string.IsNullOrEmpty(itemName) && amount == 0)
            return null;

        return new OcrQuoteItemResult(itemName, amount, note);
    }

    // ── 內部 DTO ──────────────────────────────────────────────────────────────

    private sealed record OcrQuoteItemResult(
        [property: JsonPropertyName("itemName")] string  ItemName,
        [property: JsonPropertyName("amount")]   decimal Amount,
        [property: JsonPropertyName("note")]     string  Note = "");

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
