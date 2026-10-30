using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Service.MediaService;
namespace SWP_SportCenter.Controllers;

// FIX B22: FE có thể upload trước rồi dùng URL trả về với API JSON cũ.
[ApiController, Route("api/media"), Authorize(Roles = "Member,Coach,Receptionist,CenterManager")]
public sealed class MediaController(IService media) : ControllerBase
{
    public sealed class ImageRequest { public IFormFile File { get; set; } = null!; }
    // CODE CŨ: RequestSizeLimit(6 * 1024 * 1024), file 6 MiB cộng multipart vượt ngưỡng trước validation.
    [HttpPost("images"), Consumes("multipart/form-data"), RequestSizeLimit(8 * 1024 * 1024), RequestFormLimits(MultipartBodyLengthLimit = 8 * 1024 * 1024)] // FIX B64: envelope khác giới hạn file 5 MiB.
    public async Task<IActionResult> Upload([FromForm] ImageRequest request)
    {
        if (request.File == null) return BadRequest(new { message = "Vui lòng chọn file ảnh." });
        // FIX B64: kiểm tra trước provider, kể cả implementation upload thay đổi.
        if (request.File.Length > 5 * 1024 * 1024) return StatusCode(413, new { message = "Ảnh không được vượt quá 5 MiB." });
        return Ok(new { url = await media.UploadAsync(request.File) });
    }
}
