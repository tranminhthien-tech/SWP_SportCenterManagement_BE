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
            .Include(i => i.Membership!).ThenInclude(mm => mm.Member)
            .Include(i => i.Membership!).ThenInclude(mm => mm.Package)
            .Select(i => new Response.InvoiceResponse
            {
                Id = i.Id,
                ReceptionistId = i.ReceptionistId,
                ReceptionistName = i.Receptionist != null ? i.Receptionist.FullName : string.Empty,
                MemberMembershipId = i.MemberMembershipId,
                ClassBookingId = i.ClassBookingId, ClassId = i.ClassBooking != null ? i.ClassBooking.ClassId : null,
                ClassName = i.ClassBooking != null ? i.ClassBooking.Class.ClassName : null, Kind = i.Kind,
                OriginalAmount = i.OriginalAmount, DiscountPercent = i.DiscountPercent, CreditAmount = i.CreditAmount,
                RefundPending = i.RefundRequestedAt != null && i.RefundedAt == null, // FIX B53 B55 B56
                // CODE CŨ: hóa đơn lớp trả rỗng tên học viên.
                MemberName = i.Membership != null ? i.Membership.Member.FullName : i.ClassBooking != null ? i.ClassBooking.Member.FullName : string.Empty, // FIX B53
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
            .Include(i => i.ClassBooking!).ThenInclude(b => b.Class) // FIX B53
            .Include(i => i.ClassBooking!).ThenInclude(b => b.Member)
            .Include(i => i.Receptionist)
            .Include(i => i.Membership!).ThenInclude(mm => mm.Member)
            .Include(i => i.Membership!).ThenInclude(mm => mm.Package)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null) return null;

        return new Response.InvoiceResponse
        {
            Id = invoice.Id,
            ReceptionistId = invoice.ReceptionistId,
            ReceptionistName = invoice.Receptionist != null ? invoice.Receptionist.FullName : string.Empty,
            MemberMembershipId = invoice.MemberMembershipId,
            ClassBookingId = invoice.ClassBookingId, ClassId = invoice.ClassBooking != null ? invoice.ClassBooking.ClassId : null,
            ClassName = invoice.ClassBooking != null ? invoice.ClassBooking.Class.ClassName : null, Kind = invoice.Kind,
            OriginalAmount = invoice.OriginalAmount, DiscountPercent = invoice.DiscountPercent, CreditAmount = invoice.CreditAmount,
            RefundPending = invoice.RefundRequestedAt != null && invoice.RefundedAt == null, // FIX B53 B55 B56
            MemberName = invoice.Membership != null ? invoice.Membership.Member.FullName : invoice.ClassBooking != null ? invoice.ClassBooking.Member.FullName : string.Empty, // FIX B53
            PackageName = invoice.Membership != null && invoice.Membership.Package != null ? invoice.Membership.Package.PackageName : string.Empty,
            Amount = invoice.Amount,
            PaymentMethod = invoice.PaymentMethod,
            PaymentDate = invoice.PaymentDate,
            Status = invoice.Status
        };
    }

    public async Task<Response.InvoiceResponse> CreateAsync(Request.CreateInvoiceRequest request)
    {
        // CODE CŨ: tin ReceptionistId từ request, có thể đứng tên nhân viên khác.
        var accountId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (_httpContextAccessor.HttpContext?.User.IsInRole("Receptionist") != true || !Guid.TryParse(accountId, out var actorId))
            throw new UnauthorizedAccessException("Chỉ lễ tân được lập hóa đơn.");
        var receptionistId = await _context.Receptionists.Where(r => r.AccountId == actorId && !r.IsDeleted).Select(r => (Guid?)r.Id).SingleOrDefaultAsync();
        var receptionistExists = receptionistId.HasValue; // FIX B58
        // CODE CŨ: if (!receptionistExists) throw new Exception("Lễ tân (Receptionist) không tồn tại.");
        // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
        if (!receptionistExists) throw new SWP_SportCenter.Service.Exceptions.BusinessException("Lễ tân (Receptionist) không tồn tại.");

        var memberMembershipExists = await _context.Memberships.AnyAsync(mm => mm.Id == request.MemberMembershipId);
        // CODE CŨ: if (!memberMembershipExists) throw new Exception("Lượt đăng ký gói tập không tồn tại.");
        // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
        if (!memberMembershipExists) throw new SWP_SportCenter.Service.Exceptions.BusinessException("Lượt đăng ký gói tập không tồn tại.");

        // Bảng Invoice và MemberMembership có quan hệ 1-1 (1 lượt đăng ký chỉ có 1 hóa đơn)
        var existingInvoice = await _context.Invoices.AnyAsync(i => i.MemberMembershipId == request.MemberMembershipId);
        // CODE CŨ: if (existingInvoice) throw new Exception("Gói đăng ký này đã được xuất hóa đơn.");
        // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
        if (existingInvoice) throw new SWP_SportCenter.Service.Exceptions.BusinessException("Gói đăng ký này đã được xuất hóa đơn.");

        var newInvoice = new Repository.Entity.Invoice
        {
            // CODE CŨ: request.ReceptionistId.
            ReceptionistId = receptionistId, // FIX B58
            MemberMembershipId = request.MemberMembershipId,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            // CODE CŨ: PaymentDate = DateTimeOffset.UtcNow,
            // FIX B39: hóa đơn chưa trả tiền không có PaymentDate.
            PaymentDate = request.Status == InvoiceStatus.Paid ? DateTimeOffset.UtcNow : null,
            Status = request.Status
        };

        _context.Invoices.Add(newInvoice);
        await _context.SaveChangesAsync();

        // CODE CŨ: return await GetByIdAsync(newInvoice.Id) ?? throw new Exception("Lỗi khi tạo hóa đơn.");
        // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
        return await GetByIdAsync(newInvoice.Id) ?? throw new SWP_SportCenter.Service.Exceptions.BusinessException("Lỗi khi tạo hóa đơn.");
    }

    public async Task<bool> UpdateAsync(Guid id, Request.UpdateInvoiceRequest request)
    {
        var invoice = await _context.Invoices.FindAsync(id);
        if (invoice == null) return false;

        // FIX B43: đơn online chỉ do webhook/confirm/cancel chính thức xử lý, không đổi trạng thái bằng PUT.
        if (invoice.PaymentExpiresAt != null || invoice.PaymentReference != null || invoice.Kind is "Class" or "Upgrade")
            throw new SWP_SportCenter.Service.Exceptions.ConflictException("Không được sửa tay hóa đơn online. Dùng luồng thanh toán hoặc hủy đơn.");

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
        // CODE CŨ: chỉ chặn Paid và hard-delete hóa đơn còn lại.
        if (invoice.Status is InvoiceStatus.Paid or InvoiceStatus.Refunded || invoice.PaymentExpiresAt != null || invoice.ClassBookingId != null)
            // CODE CŨ: throw new Exception("Không thể xóa hóa đơn đã được thanh toán.");
            // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
            throw new SWP_SportCenter.Service.Exceptions.BusinessException("Không thể xóa hóa đơn đã được thanh toán.");

        // CODE CŨ: _context.Invoices.Remove(invoice);
        invoice.IsDeleted = true; // FIX B56: không xóa sổ tài chính.
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
                // CODE CŨ: chỉ hóa đơn Membership.
                (i.Membership != null && i.Membership.Member.AccountId == accountId) ||
                (i.ClassBooking != null && i.ClassBooking.Member.AccountId == accountId)) // FIX B53
            // FIX B11: hóa đơn mới nhất trước.
            .OrderByDescending(i => i.PaymentDate).ThenByDescending(i => i.CreatedAt)
            .Select(i => new Response.InvoiceResponse
            {
                Id = i.Id,
                ReceptionistId = i.ReceptionistId,
                ReceptionistName = i.Receptionist != null
                    ? i.Receptionist.FullName
                    : string.Empty,

                MemberMembershipId = i.MemberMembershipId,
                ClassBookingId = i.ClassBookingId, ClassId = i.ClassBooking != null ? i.ClassBooking.ClassId : null,
                ClassName = i.ClassBooking != null ? i.ClassBooking.Class.ClassName : null, Kind = i.Kind,
                OriginalAmount = i.OriginalAmount, DiscountPercent = i.DiscountPercent, CreditAmount = i.CreditAmount,
                RefundPending = i.RefundRequestedAt != null && i.RefundedAt == null, // FIX B53 B55 B56

                MemberName = i.Membership != null &&
                             i.Membership.Member != null
                    ? i.Membership.Member.FullName
                    : i.ClassBooking != null ? i.ClassBooking.Member.FullName : string.Empty, // FIX B53

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
