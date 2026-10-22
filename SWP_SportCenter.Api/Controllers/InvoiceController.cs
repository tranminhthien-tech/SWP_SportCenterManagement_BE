using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Extensions;
using SWP_SportCenter.Service.Invoice;

namespace SWP_SportCenter.Controllers;

[ApiController]
[Route("api/invoices")]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceService _service;

    public InvoiceController(IInvoiceService service)
    {
        _service = service;
    }

    // GET: api/invoices
    // Lấy danh sách hóa đơn
    [Authorize(Policy = JwtExtensions.ManagerOrReceptionistPolicy)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();

        return Ok(result);
    }

    // GET: api/invoices/{id}
    // Lấy thông tin chi tiết hóa đơn
    [Authorize(Policy = JwtExtensions.ManagerOrReceptionistPolicy)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);

        if (result == null)
            return NotFound("Không tìm thấy hóa đơn.");

        return Ok(result);
    }

    // POST: api/invoices
    // Tạo hóa đơn mới
    [Authorize(Policy = JwtExtensions.ManagerOrReceptionistPolicy)]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] Request.CreateInvoiceRequest request)
    {
        var result = await _service.CreateAsync(request);

        return Ok(result);
    }

    // PUT: api/invoices/{id}
    // Cập nhật hóa đơn
    [Authorize(Policy = JwtExtensions.ManagerOrReceptionistPolicy)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] Request.UpdateInvoiceRequest request)
    {
        var result = await _service.UpdateAsync(id, request);

        if (!result)
            return NotFound("Không tìm thấy hóa đơn.");

        return Ok("Cập nhật hóa đơn thành công.");
    }

    // DELETE: api/invoices/{id}
    // Xóa hóa đơn
    [Authorize(Policy = JwtExtensions.ManagerOrReceptionistPolicy)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);

        if (!result)
            return NotFound("Không tìm thấy hóa đơn.");

        return Ok("Xóa hóa đơn thành công.");
    }
    
    // GET: api/members/me/invoices
// Lấy danh sách hóa đơn của Member đang đăng nhập
    [Authorize(Policy = JwtExtensions.MemberPolicy)]
    [HttpGet("members/me")]
    // FIX B11: giữ alias cũ để FE cũ chạy, thêm route chuẩn.
    [HttpGet("~/api/members/me/invoices")]
    public async Task<IActionResult> GetMyInvoices()
    {
        var result = await _service.GetMyInvoicesAsync();

        return Ok(result);
    }

// GET: api/invoices/{id}/export
// Lấy thông tin hóa đơn để xem / xuất hóa đơn
    [Authorize(Policy = JwtExtensions.ManagerOrReceptionistPolicy)]
    [HttpGet("invoices/{id:guid}/export")]
    // FIX B11: route export đúng tiền tố, giữ route cũ tương thích.
    [HttpGet("{id:guid}/export")]
    public async Task<IActionResult> ExportInvoice(Guid id)
    {
        var result = await _service.ExportAsync(id);

        if (result == null)
        {
            return NotFound(new
            {
                message = "Invoice not found"
            });
        }

        return Ok(result);
    }
}
