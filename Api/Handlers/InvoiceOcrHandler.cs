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
/// 發票 OCR Handler：接收圖片並透過 Gemini 2.0 Flash API 辨識台灣統一發票號碼與金額。
/// </summary>
public sealed class InvoiceOcrHandler(IConfiguration config)
{
    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    private const string Model = "gemini-2.5-flash";

    // 允許的圖片 MIME 類型
    private static readonly HashSet<string> AllowedMediaTypes =
        ["image/jpeg", "image/png", "image/gif", "image/webp"];

    private static readonly JsonSerializerOptions CamelOpts = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// POST /invoice-ocr
    /// 接收 multipart/form-data（欄位 "file"），呼叫 Gemini 2.0 Flash 辨識發票資訊。
    /// </summary>
    public async Task<IActionResult> RecognizeAsync(HttpRequest req)
    {
        // ── 1. 讀取設定 ────────────────────────────────────────────────────────
        var apiKey = config["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return new ObjectResult(ApiResponse.Fail("Gemini API Key 未設定。"))
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
                ApiResponse.Fail("請上傳圖片檔案（欄位名稱：file）。"));

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
                _                 => string.Empty
            };
            if (string.IsNullOrEmpty(mediaType))
                return new BadRequestObjectResult(
                    ApiResponse.Fail("不支援的圖片格式。請上傳 JPEG、PNG、GIF 或 WebP 圖片。"));
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

        // ── 5. 組建 Gemini API 請求 ───────────────────────────────────────────
        var prompt = """
            請辨識這張台灣統一發票/收據圖片，提取以下資訊：
            1. 發票號碼（格式：2個英文大寫字母 + 8個數字，如 AB12345678）
            2. 總金額（合計/總計/應付金額的數字）

            請以 JSON 格式回覆，不要加任何其他文字：
            {"invoiceNo": "發票號碼或空字串", "amount": 金額數字或0}
            """;

        // Gemini generateContent 請求格式
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
                            inlineData = new
                            {
                                mimeType = mediaType,
                                data     = base64Data
                            }
                        },
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                maxOutputTokens = 1024,
                temperature     = 0.1,
                responseMimeType = "application/json",
                responseSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        invoiceNo = new { type = "string", description = "發票號碼（2英文大寫+8數字）" },
                        amount    = new { type = "number", description = "總金額" }
                    },
                    required = new[] { "invoiceNo", "amount" }
                }
            }
        };

        var jsonBody = JsonSerializer.Serialize(requestBody, CamelOpts);

        // ── 6. 呼叫 Gemini API ────────────────────────────────────────────────
        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent?key={apiKey}";

        GeminiResponse? geminiResp;
        try
        {
            using var httpReq = new HttpRequestMessage(HttpMethod.Post, endpoint);
            httpReq.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

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

        // ── 7. 解析 Gemini 回傳文字 ───────────────────────────────────────────
        var rawText = geminiResp?.Candidates?.FirstOrDefault()
                          ?.Content?.Parts?.FirstOrDefault()?.Text ?? string.Empty;

        var cleanedText = CleanJsonText(rawText);

        OcrResult ocrResult;
        try
        {
            ocrResult = JsonSerializer.Deserialize<OcrResult>(cleanedText, CamelOpts)
                        ?? new OcrResult(string.Empty, 0);
        }
        catch
        {
            ocrResult = FallbackExtract(rawText);
        }

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

    /// <summary>JSON 解析失敗時的備援萃取</summary>
    private static OcrResult FallbackExtract(string text)
    {
        var invoiceNo = string.Empty;
        decimal amount = 0;

        var invoiceMatch = Regex.Match(text, @"[A-Z]{2}\d{8}");
        if (invoiceMatch.Success)
            invoiceNo = invoiceMatch.Value;

        var amountMatch = Regex.Match(text, @"""amount""\s*:\s*(\d+(?:\.\d+)?)");
        if (amountMatch.Success)
            decimal.TryParse(amountMatch.Groups[1].Value, out amount);

        return new OcrResult(invoiceNo, amount);
    }

    // ── 內部 DTO ──────────────────────────────────────────────────────────────

    private sealed record OcrResult(
        [property: JsonPropertyName("invoiceNo")] string  InvoiceNo,
        [property: JsonPropertyName("amount")]    decimal Amount);

    /// <summary>Gemini generateContent 回應結構</summary>
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
