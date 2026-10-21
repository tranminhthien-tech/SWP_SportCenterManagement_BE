using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Extensions;
using SWP_SportCenter.Repository.Enum;
using SWP_SportCenter.Service.Accounts;

namespace SWP_SportCenter.Controllers;

[ApiController]
[Route("api/accounts")]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    // GET: api/accounts?searchTerm=&role=Receptionist&status=Active&pageSize=10&pageIndex=1
    // Không trả Password để dùng an toàn cho form chọn tài khoản/hồ sơ nhân viên.
    [Authorize(Policy = JwtExtensions.CenterManagerPolicy)]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? searchTerm,
        [FromQuery] AccountRole? role,
        [FromQuery] AccountStatus? status,
        [FromQuery] int pageSize = 10,
        [FromQuery] int pageIndex = 1)
    {
        var result = await _accountService.GetAllAsync(
            searchTerm,
            role,
            status,
            pageSize,
            pageIndex);

        return Ok(result);
    }
    
    [Authorize(Policy = JwtExtensions.CenterManagerPolicy)]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] Request.CreateAccountRequest request)
    {
        var account = await _accountService.CreateAsync(request);

        return Ok(account);
    }


    // GET: api/accounts/{id}
    [Authorize(Policy = JwtExtensions.CenterManagerPolicy)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var account = await _accountService.GetByIdAsync(id);
        return account == null
            ? NotFound("Không tìm thấy tài khoản.")
            : Ok(account);
    }

    // PUT: api/accounts/{id}
    // Cập nhật thông tin và trạng thái account. Password và role có API riêng để tránh ảnh hưởng hồ sơ liên quan.
    [Authorize(Policy = JwtExtensions.CenterManagerPolicy)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] Request.UpdateAccountRequest request)
    {
        var updated = await _accountService.UpdateAsync(id, request);
        return updated
            ? NoContent()
            : NotFound("Không tìm thấy tài khoản.");
    }

    // DELETE: api/accounts/{id}
    // Xóa mềm: IsDeleted=true và chuyển account sang Inactive.
    [Authorize(Policy = JwtExtensions.CenterManagerPolicy)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _accountService.SoftDeleteAsync(id);
        return deleted
            ? NoContent()
            : NotFound("Không tìm thấy tài khoản.");
    }
}
