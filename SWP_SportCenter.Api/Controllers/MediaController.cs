using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Service.MediaService;
namespace SWP_SportCenter.Controllers;

// FIX B22: FE có thể upload trước rồi dùng URL trả về với API JSON cũ.
[ApiController, Route("api/media"), Authorize(Roles = "Member,Coach,Receptionist,CenterManager")]
public sealed class MediaController(IService media) : ControllerBase
{
    public sealed class ImageRequest { public IFormFile File { get; set; } = null!; }
    [HttpPost("images"), Consumes("multipart/form-data"), RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] ImageRequest request)
    {
        if (request.File == null) return BadRequest(new { message = "Vui lòng chọn file ảnh." });
        return Ok(new { url = await media.UploadAsync(request.File) });
    }
}
