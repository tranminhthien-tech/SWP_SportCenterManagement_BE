using Microsoft.AspNetCore.Http;
namespace SWP_SportCenter.Service.MediaService;

// FIX B22: giới hạn 5 MiB, kiểm tra chữ ký file, không tin tên/MIME của client.
public static class ImageValidation
{
    public static async Task ValidateAsync(IFormFile file)
    {
        if (file == null) throw new ArgumentException("Vui lòng chọn file ảnh."); // FIX B22
        if (file.Length <= 0 || file.Length > 5 * 1024 * 1024)
            throw new ArgumentException("Ảnh phải có dữ liệu và không quá 5 MiB.");
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        await using var stream = file.OpenReadStream();
        var header = new byte[12];
        var length = await stream.ReadAtLeastAsync(header, 12, throwOnEndOfStream: false);
        bool valid = ext switch {
            ".jpg" or ".jpeg" => length >= 3 && header[0] == 255 && header[1] == 216 && header[2] == 255,
            ".png" => length >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}),
            ".gif" => length >= 6 && (System.Text.Encoding.ASCII.GetString(header, 0, 6) is "GIF87a" or "GIF89a"),
            ".webp" => length >= 12 && System.Text.Encoding.ASCII.GetString(header,0,4) == "RIFF" &&
                System.Text.Encoding.ASCII.GetString(header,8,4) == "WEBP",
            _ => false
        };
        if (!valid) throw new ArgumentException("Chỉ chấp nhận ảnh JPEG, PNG, GIF hoặc WebP đúng định dạng.");
    }
}
