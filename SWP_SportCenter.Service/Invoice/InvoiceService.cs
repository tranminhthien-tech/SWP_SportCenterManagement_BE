using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using SWP_SportCenter.Repository.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SWP_SportCenter.Service.Invoice;

public class InvoiceService : IInvoiceService
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public InvoiceService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IEnumerable<Response.InvoiceResponse>> GetAllAsync()
    {
        return await _context.Invoices
            .AsNoTracking()
            .Include(i => i.Receptionist)
            .Include(i => i.Membership).ThenInclude(mm => mm.Member)
            .Include(i => i.Membership).ThenInclude(mm => mm.Package)
            .Select(i => new Response.InvoiceResponse
            {
                Id = i.Id,
                ReceptionistId = i.ReceptionistId,
                ReceptionistName = i.Receptionist != null ? i.Receptionist.FullName : string.Empty,
                MemberMembershipId = i.MemberMembershipId,
                MemberName = i.Membership != null && i.Membership.Member != null ? i.Membership.Member.FullName : string.Empty,
                PackageName = i.Membership != null && i.Membership.Package != null ? i.Membership.Package.PackageName : string.Empty,
                Amount = i.Amount,
                PaymentMethod = i.PaymentMethod,
                PaymentDate = i.PaymentDate,
                Status = i.Status
            })
            .ToListAsync();
    }

    public async Task<Response.InvoiceResponse?> GetByIdAsync(Guid id)
    {
        var invoice = await _context.Invoices
            .AsNoTracking()
            .Include(i => i.Receptionist)
            .Include(i => i.Membership).ThenInclude(mm => mm.Member)
            .Include(i => i.Membership).ThenInclude(mm => mm.Package)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null) return null;

        return new Response.InvoiceResponse
        {
            Id = invoice.Id,
            ReceptionistId = invoice.ReceptionistId,
            ReceptionistName = invoice.Receptionist != null ? invoice.Receptionist.FullName : string.Empty,
            MemberMembershipId = invoice.MemberMembershipId,
            MemberName = invoice.Membership != null && invoice.Membership.Member != null ? invoice.Membership.Member.FullName : string.Empty,
            PackageName = invoice.Membership != null && invoice.Membership.Package != null ? invoice.Membership.Package.PackageName : string.Empty,
            Amount = invoice.Amount,
            PaymentMethod = invoice.PaymentMethod,
            PaymentDate = invoice.PaymentDate,
            Status = invoice.Status
        };
    }

    public async Task<Response.InvoiceResponse> CreateAsync(Request.CreateInvoiceRequest request)
    {
        var receptionistExists = await _context.Receptionists.AnyAsync(r => r.Id == request.ReceptionistId);
        if (!receptionistExists) throw new Exception("Lễ tân (Receptionist) không tồn tại.");

        var memberMembershipExists = await _context.Memberships.AnyAsync(mm => mm.Id == request.MemberMembershipId);
        if (!memberMembershipExists) throw new Exception("Lượt đăng ký gói tập không tồn tại.");

        // Bảng Invoice và MemberMembership có quan hệ 1-1 (1 lượt đăng ký chỉ có 1 hóa đơn)
        var existingInvoice = await _context.Invoices.AnyAsync(i => i.MemberMembershipId == request.MemberMembershipId);
        if (existingInvoice) throw new Exception("Gói đăng ký này đã được xuất hóa đơn.");

        var newInvoice = new Repository.Entity.Invoice
        {
            ReceptionistId = request.ReceptionistId,
            MemberMembershipId = request.MemberMembershipId,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            PaymentDate = DateTimeOffset.UtcNow,
            Status = request.Status
        };

        _context.Invoices.Add(newInvoice);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(newInvoice.Id) ?? throw new Exception("Lỗi khi tạo hóa đơn.");
    }

    public async Task<bool> UpdateAsync(Guid id, Request.UpdateInvoiceRequest request)
    {
        var invoice = await _context.Invoices.FindAsync(id);
        if (invoice == null) return false;

        invoice.PaymentMethod = request.PaymentMethod;
        
        // Tự động cập nhật lại ngày thanh toán nếu trạng thái đổi thành Paid
        if (invoice.Status != InvoiceStatus.Paid && request.Status == InvoiceStatus.Paid)
        {
            invoice.PaymentDate = DateTimeOffset.UtcNow;
        }
        
        invoice.Status = request.Status;

        _context.Invoices.Update(invoice);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var invoice = await _context.Invoices.FindAsync(id);
        if (invoice == null) return false;

        // Chặn xóa nếu đã thanh toán
        if (invoice.Status == InvoiceStatus.Paid)
            throw new Exception("Không thể xóa hóa đơn đã được thanh toán.");

        _context.Invoices.Remove(invoice);
        await _context.SaveChangesAsync();
        
        return true;
    }
    // Lấy danh sách hóa đơn của Member đang đăng nhập
    public async Task<IEnumerable<Response.InvoiceResponse>>
        GetMyInvoicesAsync()
    {
        // Lấy AccountId từ JWT Token
        var accountIdClaim = _httpContextAccessor.HttpContext?
            .User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(accountIdClaim, out var accountId))
        {
            throw new UnauthorizedAccessException(
                "Không tìm thấy AccountId trong token.");
        }

        // Lấy hóa đơn thuộc về Member hiện tại
        return await _context.Invoices
            .AsNoTracking()
            .Where(i =>
                i.Membership != null &&
                i.Membership.Member != null &&
                i.Membership.Member.AccountId == accountId)
            .Select(i => new Response.InvoiceResponse
            {
                Id = i.Id,
                ReceptionistId = i.ReceptionistId,
                ReceptionistName = i.Receptionist != null
                    ? i.Receptionist.FullName
                    : string.Empty,

                MemberMembershipId = i.MemberMembershipId,

                MemberName = i.Membership != null &&
                             i.Membership.Member != null
                    ? i.Membership.Member.FullName
                    : string.Empty,

                PackageName = i.Membership != null &&
                              i.Membership.Package != null
                    ? i.Membership.Package.PackageName
                    : string.Empty,

                Amount = i.Amount,
                PaymentMethod = i.PaymentMethod,
                PaymentDate = i.PaymentDate,
                Status = i.Status
            })
            .ToListAsync();
    }


// Lấy dữ liệu hóa đơn để phục vụ xuất/in
    public async Task<Response.InvoiceResponse?> ExportAsync(Guid id)
    {
        var invoice = await GetByIdAsync(id);

        if (invoice == null)
            return null;

        return invoice;
    }
}