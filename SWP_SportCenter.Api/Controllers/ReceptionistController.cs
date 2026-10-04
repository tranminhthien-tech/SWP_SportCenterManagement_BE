using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Service.Receptionist;

namespace SWP_SportCenter.Controllers;

[ApiController]
[Route("api/receptionists")]
public class ReceptionistController : ControllerBase
{
    private readonly IReceptionistService _receptionistService;

    public ReceptionistController(IReceptionistService receptionistService)
    {
        _receptionistService = receptionistService;
    }

    // GET: api/receptionists
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _receptionistService.GetAllAsync());
    }

    // GET: api/receptionists/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var receptionist = await _receptionistService.GetByIdAsync(id);
        return receptionist == null
            ? NotFound("Không tìm thấy lễ tân.")
            : Ok(receptionist);
    }

    // GET: api/receptionists/account/{accountId}
    [HttpGet("account/{accountId:guid}")]
    public async Task<IActionResult> GetByAccountId(Guid accountId)
    {
        var receptionist = await _receptionistService.GetByAccountIdAsync(accountId);
        return receptionist == null
            ? NotFound("Không tìm thấy lễ tân.")
            : Ok(receptionist);
    }

    // GET: api/receptionists/me
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var accountId = GetCurrentAccountId();
        if (accountId == null)
        {
            return Unauthorized("Không tìm thấy AccountId trong token.");
        }

        var receptionist = await _receptionistService.GetByAccountIdAsync(accountId.Value);
        return receptionist == null
            ? NotFound("Không tìm thấy hồ sơ lễ tân.")
            : Ok(receptionist);
    }

    // POST: api/receptionists
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] Request.CreateReceptionistRequest request)
    {
        var receptionist = await _receptionistService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = receptionist.Id }, receptionist);
    }

    // PUT: api/receptionists/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] Request.UpdateReceptionistRequest request)
    {
        var updated = await _receptionistService.UpdateAsync(id, request);
        return updated
            ? NoContent()
            : NotFound("Không tìm thấy lễ tân.");
    }

    // DELETE: api/receptionists/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _receptionistService.DeleteAsync(id);
        return deleted
            ? NoContent()
            : NotFound("Không tìm thấy lễ tân.");
    }

    private Guid? GetCurrentAccountId()
    {
        var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(accountIdClaim, out var accountId) ? accountId : null;
    }
}
