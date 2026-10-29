using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using SWP_SportCenter.Repository.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SWP_SportCenter.Service.Classes;

public class ClassService : IClassService
{
    private readonly AppDbContext _context;
    private readonly TimeProvider _clock; // FIX B58

    // CODE CŨ: ClassService(AppDbContext context)
    public ClassService(AppDbContext context, TimeProvider? clock = null)
    {
        _context = context;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<IEnumerable<Response.ClassResponse>> GetAllAsync()
    {
        // Include Category và Coach để lấy được Tên hiển thị lên giao diện
        return await _context.Classes
            .AsNoTracking()
            .Where(c => !c.IsDeleted) // FIX B58
            .Include(c => c.Category)
            .Include(c => c.Coach)
            .Select(c => new Response.ClassResponse
            {
                Id = c.Id,
                Price = c.Price, // FIX B53
                ClassName = c.ClassName,
                CategoryId = c.CategoryId,
                CategoryName = c.Category != null ? c.Category.CategoryName : string.Empty,
                CoachId = c.CoachId,
                CoachName = c.Coach != null ? c.Coach.FullName : string.Empty,
                MaxCapacity = c.MaxCapacity,
                // FIX B14: không tính booking hủy/xóa mềm.
                // CODE CŨ: Status != Cancelled đếm cả hold đã hết hạn.
                CurrentEnrolled = c.ClassBookings.Count(b => !b.IsDeleted && (b.Status == Repository.Enum.BookingStatus.Booked || b.Status == Repository.Enum.BookingStatus.Attended || (b.Status == Repository.Enum.BookingStatus.PendingPayment && b.PaymentExpiresAt > _clock.GetUtcNow()))),
                SpotsLeft = Math.Max(0, c.MaxCapacity - c.ClassBookings.Count(b => !b.IsDeleted && (b.Status == Repository.Enum.BookingStatus.Booked || b.Status == Repository.Enum.BookingStatus.Attended || (b.Status == Repository.Enum.BookingStatus.PendingPayment && b.PaymentExpiresAt > _clock.GetUtcNow())))), // FIX B53
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Status = c.Status,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<Response.ClassResponse?> GetByIdAsync(Guid id)
    {
        var classEntity = await _context.Classes
            .AsNoTracking()
            .Include(c => c.Category)
            .Include(c => c.Coach)
            // CODE CŨ: chỉ lọc ID.
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted); // FIX B58

        if (classEntity == null) return null;

        // FIX B14: tính trực tiếp database, không phụ thuộc navigation đã load.
        var enrolled = await _context.ClassBookings.CountAsync(b => b.ClassId == id &&
            // CODE CŨ: Status != Cancelled.
            !b.IsDeleted && (b.Status == Repository.Enum.BookingStatus.Booked || b.Status == Repository.Enum.BookingStatus.Attended || (b.Status == Repository.Enum.BookingStatus.PendingPayment && b.PaymentExpiresAt > _clock.GetUtcNow()))); // FIX B53

        return new Response.ClassResponse
        {
            Id = classEntity.Id,
            Price = classEntity.Price, // FIX B53
            ClassName = classEntity.ClassName,
            CategoryId = classEntity.CategoryId,
            CategoryName = classEntity.Category != null ? classEntity.Category.CategoryName : string.Empty,
            CoachId = classEntity.CoachId,
            CoachName = classEntity.Coach != null ? classEntity.Coach.FullName : string.Empty,
            MaxCapacity = classEntity.MaxCapacity,
            CurrentEnrolled = enrolled,
            SpotsLeft = Math.Max(0, classEntity.MaxCapacity - enrolled),
            StartDate = classEntity.StartDate,
            EndDate = classEntity.EndDate,
            Status = classEntity.Status,
            CreatedAt = classEntity.CreatedAt,
            UpdatedAt = classEntity.UpdatedAt
        };
    }

    public async Task<Response.ClassResponse> CreateAsync(Request.CreateClassRequest request)
    {
        // Validate xem StartDate có trước EndDate không
        if (request.StartDate >= request.EndDate)
        {
            // CODE CŨ: throw new Exception("Ngày kết thúc phải lớn hơn ngày bắt đầu.");
            // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
            throw new SWP_SportCenter.Service.Exceptions.BusinessException("Ngày kết thúc phải lớn hơn ngày bắt đầu.");
        }

        var newClass = new Class
        {
            ClassName = request.ClassName,
            Price = request.Price, // FIX B53
            CategoryId = request.CategoryId,
            CoachId = request.CoachId,
            MaxCapacity = request.MaxCapacity,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status
        };
        await ValidateReferencesAsync(
            request.CategoryId,
            request.CoachId);

        _context.Classes.Add(newClass);
        await _context.SaveChangesAsync();

        // Query lại để lấy được thông tin CategoryName và CoachName
        // CODE CŨ: return await GetByIdAsync(newClass.Id) ?? throw new Exception("Lỗi khi tạo lớp học.");
        // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
        return await GetByIdAsync(newClass.Id) ?? throw new SWP_SportCenter.Service.Exceptions.BusinessException("Lỗi khi tạo lớp học.");
    }

    public async Task<bool> UpdateAsync(Guid id, Request.UpdateClassRequest request)
    {
        if (request.StartDate >= request.EndDate)
        {
            // CODE CŨ: throw new Exception("Ngày kết thúc phải lớn hơn ngày bắt đầu.");
            // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
            throw new SWP_SportCenter.Service.Exceptions.BusinessException("Ngày kết thúc phải lớn hơn ngày bắt đầu.");
        }

        var classEntity = await _context.Classes.FindAsync(id);
        if (classEntity == null) return false;

        await ValidateReferencesAsync(
            request.CategoryId,
            request.CoachId);
        
        classEntity.ClassName = request.ClassName;
        classEntity.Price = request.Price; // FIX B53
        classEntity.CategoryId = request.CategoryId;
        classEntity.CoachId = request.CoachId;
        classEntity.MaxCapacity = request.MaxCapacity;
        classEntity.StartDate = request.StartDate;
        classEntity.EndDate = request.EndDate;
        classEntity.Status = request.Status;
        classEntity.UpdatedAt = DateTimeOffset.UtcNow;

        
        _context.Classes.Update(classEntity);
        await _context.SaveChangesAsync();
        
        return true;
    }

    /* CODE CŨ B58: chặn mọi lớp có lịch/booking và hard-delete.
    public async Task<bool> DeleteAsync(Guid id)
    {
        var classEntity = await _context.Classes.FindAsync(id);
        if (classEntity == null) return false;

        // Kiểm tra xem Lớp này đã được xếp lịch hoặc có học viên đăng ký chưa
        var hasSessions = await _context.ClassSessions.AnyAsync(cs => cs.ClassId == id);
        var hasBookings = await _context.ClassBookings.AnyAsync(cb => cb.ClassId == id);

        if (hasSessions || hasBookings)
        {
            // CODE CŨ: throw new Exception("Không thể xóa lớp học này vì đã có lịch học được xếp hoặc đã có học viên đăng ký.");
            // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
            throw new SWP_SportCenter.Service.Exceptions.BusinessException("Không thể xóa lớp học này vì đã có lịch học được xếp hoặc đã có học viên đăng ký.");
        }

        _context.Classes.Remove(classEntity);
        await _context.SaveChangesAsync();

        return true;
    }
    */
    public async Task<bool> DeleteAsync(Guid id)
    {
        await using var tx = _context.Database.IsRelational() ? await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
        var cls = await _context.Classes.Include(c => c.ClassSessions).FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        if (cls == null) return false;
        if (ClassRules.IsRunning(cls, _clock.GetUtcNow())) throw new SWP_SportCenter.Service.Exceptions.BusinessException("Lớp đang diễn ra nên không xóa được.");
        // FIX B58: xóa lớp chưa mở thì trả hold và yêu cầu hoàn cho người đã trả, không mất dấu tiền.
        if (!ClassRules.HasStarted(cls, _clock.GetUtcNow())) {
            var bookings = await _context.ClassBookings.Include(b => b.Invoice).Where(b => b.ClassId == id && !b.IsDeleted && b.Status != Repository.Enum.BookingStatus.Cancelled).ToListAsync();
            foreach (var b in bookings) {
                b.Status = Repository.Enum.BookingStatus.Cancelled; b.CancelledAt = _clock.GetUtcNow();
                if (b.Invoice?.Status == Repository.Enum.InvoiceStatus.Pending) b.Invoice.Status = Repository.Enum.InvoiceStatus.Cancelled;
                if (b.Invoice?.Status == Repository.Enum.InvoiceStatus.Paid && b.Invoice.Amount > 0) b.Invoice.RefundRequestedAt ??= _clock.GetUtcNow();
            }
        }
        cls.IsDeleted = true; // FIX B58: giữ lịch sử tài chính.
        await _context.SaveChangesAsync(); if (tx != null) await tx.CommitAsync(); return true;
    }

    public async Task<IEnumerable<Response.ClassMemberResponse>> GetMembersByClassIdAsync(Guid classId)
    {
        // Join với bảng Member thông qua ClassBooking
        // Email được lấy từ Account của Member
        return await _context.ClassBookings
            .AsNoTracking()
            // CODE CŨ: mọi trạng thái booking, gồm cả chưa trả tiền.
            .Where(cb => cb.ClassId == classId && !cb.IsDeleted && (cb.Status == Repository.Enum.BookingStatus.Booked || cb.Status == Repository.Enum.BookingStatus.Attended)) // FIX B53
            .Select(cb => new Response.ClassMemberResponse
            {
                MemberId = cb.MemberId,
                FullName = cb.Member.FullName,
                // CODE CŨ: Phone = cb.Member.Phone,
                // FIX B18: response cũ vẫn là string.
                Phone = cb.Member.Phone ?? string.Empty,
                Email = cb.Member.Account.Email,
                BookingStatus = cb.Status,
                BookingDate = cb.BookingDate
            })
            .ToListAsync();
    }
    private async Task ValidateReferencesAsync(
        Guid categoryId,
        Guid coachId)
    {
        if (!await _context.SportCategories
                .AnyAsync(c => c.Id == categoryId && !c.IsDeleted)) // FIX B58
        {
            // CODE CŨ: throw new Exception("Bộ môn không tồn tại.");
            // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
            throw new SWP_SportCenter.Service.Exceptions.BusinessException("Bộ môn không tồn tại.");
        }

        if (!await _context.Coaches
                .AnyAsync(c => c.Id == coachId && !c.IsDeleted && !c.Account.IsDeleted && c.Account.Role == Repository.Enum.AccountRole.Coach && c.Account.Status == Repository.Enum.AccountStatus.Active)) // FIX B58
        {
            // CODE CŨ: throw new Exception("Huấn luyện viên không tồn tại.");
            // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
            throw new SWP_SportCenter.Service.Exceptions.BusinessException("Huấn luyện viên không tồn tại.");
        }
    }
}
