using SWP_SportCenter.Service.MediaService;
namespace SWP_SportCenter.Infrastructure;

// FIX B22: giữ URL trong DTO/DB; file chỉ có trên multipart, không lưu byte ảnh trong DB.
public static class ProfileImageForm
{
    public static async Task<string?> ResolveAsync(HttpRequest request, IService media, string? existing)
    {
        var form = await request.ReadFormAsync();
        if (form.Files.Count == 0) return existing;
        if (form.Files.Count != 1) throw new ArgumentException("Chỉ chọn một ảnh đại diện.");
        return await media.UploadAsync(form.Files[0]);
    }
}
