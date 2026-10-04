using Microsoft.AspNetCore.Mvc;
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

    // GET: api/accounts/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var account = await _accountService.GetByIdAsync(id);
        return account == null
            ? NotFound("Không tìm thấy tài khoản.")
            : Ok(account);
    }
}
