namespace Jabez.Api.Common;

/// <summary>
/// 以檔案開頭的 magic bytes 偵測實際 MIME，避免攻擊者只竄改 Content-Type header
/// 就能上傳 .exe / .php / .svg（含 XSS）等危險檔案。
/// 支援的格式：PNG / JPEG / GIF / WebP / HEIC / AVIF / PDF。
/// </summary>
public static class FileSignatureValidator
{
    /// <summary>
    /// 從 stream 開頭嘗試辨識 MIME。回傳 null 代表不在白名單。
    /// 呼叫端應確認辨識結果是否與宣告的 Content-Type 一致、是否在允許清單。
    /// </summary>
    public static async Task<string?> DetectAsync(Stream stream, CancellationToken ct = default)
    {
        // HEIC / AVIF 的 ftyp box 在 offset 4-12，需至少 16 bytes
        var peek = new byte[16];
        var pos  = stream.CanSeek ? stream.Position : 0L;
        var read = await ReadFullyAsync(stream, peek, ct);
        if (stream.CanSeek) stream.Position = pos;

        return Detect(peek.AsSpan(0, read));
    }

    /// <summary>同步版本：對已讀入的 byte[] 進行偵測。</summary>
    public static string? Detect(ReadOnlySpan<byte> bytes)
    {
        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (bytes.Length >= 8 &&
            bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
            bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
            return "image/png";

        // JPEG: FF D8 FF
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            return "image/jpeg";

        // GIF: 47 49 46 38 (37|39) 61  → "GIF87a" or "GIF89a"
        if (bytes.Length >= 6 &&
            bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x38 &&
            (bytes[4] == 0x37 || bytes[4] == 0x39) && bytes[5] == 0x61)
            return "image/gif";

        // WebP: 52 49 46 46 ?? ?? ?? ?? 57 45 42 50  → "RIFF....WEBP"
        if (bytes.Length >= 12 &&
            bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
            bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
            return "image/webp";

        // HEIC / AVIF：offset 4-7 = "ftyp"，offset 8-11 = brand
        if (bytes.Length >= 12 &&
            bytes[4] == 0x66 && bytes[5] == 0x74 && bytes[6] == 0x79 && bytes[7] == 0x70)
        {
            var brand = System.Text.Encoding.ASCII.GetString(bytes.Slice(8, 4));
            return brand switch
            {
                "heic" or "heix" or "hevc" or "heim" or "heis" or "hevm" or "hevs" or "heiq" or "mif1" or "msf1" => "image/heic",
                "avif" or "avis" => "image/avif",
                _ => null,
            };
        }

        // PDF: 25 50 44 46 2D  → "%PDF-"
        if (bytes.Length >= 5 &&
            bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46 && bytes[4] == 0x2D)
            return "application/pdf";

        return null;
    }

    private static async Task<int> ReadFullyAsync(Stream stream, byte[] buffer, CancellationToken ct)
    {
        int total = 0;
        while (total < buffer.Length)
        {
            int n = await stream.ReadAsync(buffer.AsMemory(total, buffer.Length - total), ct);
            if (n == 0) break;
            total += n;
        }
        return total;
    }
}
