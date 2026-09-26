using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Service.Room;

namespace SWP_SportCenter.Controllers;

[ApiController]
[Route("api/rooms")]
public class RoomController : ControllerBase
{
    private readonly IRoomService _service;

    public RoomController(IRoomService service)
    {
        _service = service;
    }

    // GET: api/rooms
    // Lấy danh sách phòng tập
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();

        return Ok(result);
    }

    // GET: api/rooms/{id}
    // Lấy thông tin chi tiết phòng tập
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);

        if (result == null)
            return NotFound("Không tìm thấy phòng tập.");

        return Ok(result);
    }

    // POST: api/rooms
    // Tạo phòng tập mới
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] Request.CreateRoomRequest request)
    {
        var result = await _service.CreateAsync(request);

        return Ok(result);
    }

    // PUT: api/rooms/{id}
    // Cập nhật thông tin phòng tập
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] Request.UpdateRoomRequest request)
    {
        var result = await _service.UpdateAsync(id, request);

        if (!result)
            return NotFound("Không tìm thấy phòng tập.");

        return Ok("Cập nhật phòng tập thành công.");
    }

    // DELETE: api/rooms/{id}
    // Xóa phòng tập
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);

        if (!result)
            return NotFound("Không tìm thấy phòng tập.");

        return Ok("Xóa phòng tập thành công.");
    }
}