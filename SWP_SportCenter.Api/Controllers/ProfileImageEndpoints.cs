using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Infrastructure;
using Media = SWP_SportCenter.Service.MediaService.IService;
namespace SWP_SportCenter.Controllers;
// FIX B22: bổ sung multipart, giữ action JSON cũ để tương thích.
public partial class CoachController
{
    [HttpPost, Consumes("multipart/form-data"), Authorize(Policy = "CenterManagerPolicy"), RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> CreateForm([FromForm] SWP_SportCenter.Service.Coach.Request.CreateCoachRequest request, [FromServices] Media media)
    {
        request.Avatar = await ProfileImageForm.ResolveAsync(Request, media, request.Avatar);
        return await Create(request);
    }
    [HttpPut("{id:guid}"), Consumes("multipart/form-data"), Authorize(Policy = "CenterManagerPolicy"), RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UpdateForm(Guid id, [FromForm] SWP_SportCenter.Service.Coach.Request.UpdateCoachRequest request, [FromServices] Media media)
    {
        // FIX B22: không chọn ảnh mới thì giữ URL đang có.
        request.Avatar ??= (await _service.GetByIdAsync(id))?.Avatar;
        request.Avatar = await ProfileImageForm.ResolveAsync(Request, media, request.Avatar);
        return await Update(id, request);
    }
}
public partial class ReceptionistController
{
    [HttpPost, Consumes("multipart/form-data"), Authorize(Policy = "CenterManagerPolicy"), RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> CreateForm([FromForm] SWP_SportCenter.Service.Receptionist.Request.CreateReceptionistRequest request, [FromServices] Media media)
    {
        request.Avatar = await ProfileImageForm.ResolveAsync(Request, media, request.Avatar);
        return await Create(request);
    }
    [HttpPut("{id:guid}"), Consumes("multipart/form-data"), Authorize(Policy = "CenterManagerPolicy"), RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UpdateForm(Guid id, [FromForm] SWP_SportCenter.Service.Receptionist.Request.UpdateReceptionistRequest request, [FromServices] Media media)
    {
        request.Avatar ??= (await _receptionistService.GetByIdAsync(id))?.Avatar; // FIX B22
        request.Avatar = await ProfileImageForm.ResolveAsync(Request, media, request.Avatar);
        return await Update(id, request);
    }
}
public partial class CenterManagerController
{
    [HttpPost, Consumes("multipart/form-data"), Authorize(Policy = "CenterManagerPolicy"), RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> CreateForm([FromForm] SWP_SportCenter.Service.CenterManager.Request.CreateCenterManagerRequest request, [FromServices] Media media)
    {
        request.Avatar = await ProfileImageForm.ResolveAsync(Request, media, request.Avatar);
        return await Create(request);
    }
    [HttpPut("{id:guid}"), Consumes("multipart/form-data"), Authorize(Policy = "CenterManagerPolicy"), RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UpdateForm(Guid id, [FromForm] SWP_SportCenter.Service.CenterManager.Request.UpdateCenterManagerRequest request, [FromServices] Media media)
    {
        request.Avatar ??= (await _centerManagerService.GetByIdAsync(id))?.Avatar; // FIX B22
        request.Avatar = await ProfileImageForm.ResolveAsync(Request, media, request.Avatar);
        return await Update(id, request);
    }
}
public partial class MemberController
{
    [HttpPost, Consumes("multipart/form-data"), Authorize(Policy = "ManagerOrReceptionistPolicy"), RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> CreateForm([FromForm] SWP_SportCenter.Service.Member.Request.MemberRequest request, [FromServices] Media media)
    {
        request.Avatar = await ProfileImageForm.ResolveAsync(Request, media, request.Avatar);
        return await Create(request);
    }
    [HttpPut("{id:guid}"), Consumes("multipart/form-data"), Authorize(Policy = "ManagerOrReceptionistPolicy"), RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UpdateForm(Guid id, [FromForm] SWP_SportCenter.Service.Member.Request.MemberRequest request, [FromServices] Media media)
    {
        request.Avatar ??= (await _service.GetByIdAsync(id))?.Avatar; // FIX B22
        request.Avatar = await ProfileImageForm.ResolveAsync(Request, media, request.Avatar);
        return await Update(id, request);
    }
}
public partial class MemberController
{
    [HttpPut("me"), Consumes("multipart/form-data"), Authorize(Policy = "MemberPolicy"), RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UpdateMyProfileForm([FromForm] SWP_SportCenter.Service.Member.Request.MemberRequest request, [FromServices] Media media)
    {
        request.Avatar ??= (await _service.GetMyProfileAsync())?.Avatar; // FIX B22
        request.Avatar = await ProfileImageForm.ResolveAsync(Request, media, request.Avatar);
        return await UpdateMyProfile(request);
    }
}
