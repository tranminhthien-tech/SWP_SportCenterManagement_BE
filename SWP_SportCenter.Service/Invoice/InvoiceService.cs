using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using SWP_SportCenter.Repository.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SWP_SportCenter.Service.Invoice;

public class InvoiceService : IInvoiceService
{
    private readonly AppDbContext _context;

    public InvoiceService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Response.InvoiceResponse>> GetAllAsync()
    {
        return await _context.Invoices
            .AsNoTracking()
            .Include(i => i.Receptionist)
            .Include(i => i.MemberMembership).ThenInclude(mm => mm.Member)
            .Include(i => i.MemberMembership).ThenInclude(mm => mm.Package)
            .Select(i => new Response.InvoiceResponse
            {
                Id = i.Id,
                ReceptionistId = i.ReceptionistId,
                ReceptionistName = i.Receptionist != null ? i.Receptionist.FullName : string.Empty,
                MemberMembershipId = i.MemberMembershipId,
                MemberName = i.MemberMembership != null && i.MemberMembership.Member != null ? i.MemberMembership.Member.FullName : string.Empty,
                PackageName = i.MemberMembership != null && i.MemberMembership.Package != null ? i.MemberMembership.Package.PackageName : string.Empty,
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
            .Include(i => i.MemberMembership).ThenInclude(mm => mm.Member)
            .Include(i => i.MemberMembership).ThenInclude(mm => mm.Package)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null) return null;

        return new Response.InvoiceResponse
        {
            Id = invoice.Id,
            ReceptionistId = invoice.ReceptionistId,
            ReceptionistName = invoice.Receptionist != null ? invoice.Receptionist.FullName : string.Empty,
            MemberMembershipId = invoice.MemberMembershipId,
            MemberName = invoice.MemberMembership != null && invoice.MemberMembership.Member != null ? invoice.MemberMembership.Member.FullName : string.Empty,
            PackageName = invoice.MemberMembership != null && invoice.MemberMembership.Package != null ? invoice.MemberMembership.Package.PackageName : string.Empty,
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

        var memberMembershipExists = await _context.MemberMemberships.AnyAsync(mm => mm.Id == request.MemberMembershipId);
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
}