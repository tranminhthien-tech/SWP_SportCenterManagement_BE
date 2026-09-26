using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SWP_SportCenter.Service.Invoice;

public interface IInvoiceService
{
    Task<IEnumerable<Response.InvoiceResponse>> GetAllAsync();
    Task<Response.InvoiceResponse?> GetByIdAsync(Guid id);
    Task<Response.InvoiceResponse> CreateAsync(Request.CreateInvoiceRequest request);
    Task<bool> UpdateAsync(Guid id, Request.UpdateInvoiceRequest request);
    Task<bool> DeleteAsync(Guid id);
    // Lấy hóa đơn của Member đang đăng nhập
    Task<IEnumerable<Response.InvoiceResponse>> GetMyInvoicesAsync();

    // Lấy dữ liệu hóa đơn phục vụ xuất/in
    Task<Response.InvoiceResponse?> ExportAsync(Guid id);
}