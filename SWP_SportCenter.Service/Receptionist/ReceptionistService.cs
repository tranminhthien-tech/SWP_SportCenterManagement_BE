using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.Receptionist;

public class ReceptionistService : IReceptionistService
{
    private readonly AppDbContext _context;

    public ReceptionistService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Response.ReceptionistResponse>> GetAllAsync()
    {
        return await _context.Receptionists
            .AsNoTracking()
            .OrderBy(receptionist => receptionist.FullName)
            .Select(receptionist => MapToResponse(receptionist))
            .ToListAsync();
    }

    public async Task<Response.ReceptionistResponse?> GetByIdAsync(Guid id)
    {
        var receptionist = await _context.Receptionists
            .AsNoTracking()
            .FirstOrDefaultAsync(receptionist => receptionist.Id == id);

        return receptionist == null ? null : MapToResponse(receptionist);
    }

    public async Task<Response.ReceptionistResponse?> GetByAccountIdAsync(Guid accountId)
    {
        var receptionist = await _context.Receptionists
            .AsNoTracking()
            .FirstOrDefaultAsync(receptionist => receptionist.AccountId == accountId);

        return receptionist == null ? null : MapToResponse(receptionist);
    }

    public async Task<Response.ReceptionistResponse> CreateAsync(
        Request.CreateReceptionistRequest request)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(account => account.Id == request.AccountId);

        if (account == null)
        {
            throw new InvalidOperationException("Tài khoản không tồn tại.");
        }

        if (account.Role != AccountRole.Receptionist)
        {
            throw new InvalidOperationException(
                "Tài khoản này không có vai trò Lễ tân (Receptionist).");
        }

        if (await _context.Receptionists.AnyAsync(
                receptionist => receptionist.AccountId == request.AccountId))
        {
            throw new InvalidOperationException("Tài khoản này đã có hồ sơ lễ tân.");
        }

        // Chỉ kiểm tra Phone ở Receptionist.
        // Email được quản lý ở Account.
        await EnsureContactInformationIsAvailableAsync(request.Phone);

        var receptionist = new Repository.Entity.Receptionist
        {
            AccountId = request.AccountId,
            FullName = request.FullName.Trim(),
            Phone = request.Phone.Trim(),
            WorkingShift = request.WorkingShift?.Trim() ?? string.Empty,

            // Gắn Account để MapToResponse có thể lấy Email
            Account = account
        };

        _context.Receptionists.Add(receptionist);
        await _context.SaveChangesAsync();

        return MapToResponse(receptionist);
    }

    public async Task<bool> UpdateAsync(
        Guid id,
        Request.UpdateReceptionistRequest request)
    {
        var receptionist = await _context.Receptionists
            .Include(r => r.Account)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (receptionist == null)
        {
            return false;
        }

        await EnsureContactInformationIsAvailableAsync(
            request.Phone,
            receptionist.Id);

        receptionist.FullName = request.FullName.Trim();
        receptionist.Phone = request.Phone.Trim();
        receptionist.WorkingShift = request.WorkingShift?.Trim() ?? string.Empty;

        // Email nằm trong Account
        receptionist.Account.Email = request.Email.Trim();

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var receptionist = await _context.Receptionists.FindAsync(id);
        if (receptionist == null)
        {
            return false;
        }

        var hasInvoices = await _context.Invoices
            .AnyAsync(invoice => invoice.ReceptionistId == id);

        if (hasInvoices)
        {
            throw new InvalidOperationException(
                "Không thể xóa lễ tân vì đã có hóa đơn do lễ tân này lập.");
        }

        _context.Receptionists.Remove(receptionist);
        await _context.SaveChangesAsync();
        return true;
    }

    // private async Task EnsureContactInformationIsAvailableAsync(
    //     string phone,
    //     string email,
    //     Guid? currentReceptionistId = null)
    // {
    //     var normalizedPhone = phone.Trim();
    //     var normalizedEmail = email.Trim();
    //
    //     var isPhoneUsed = await _context.Receptionists.AnyAsync(receptionist =>
    //         receptionist.Phone == normalizedPhone && receptionist.Id != currentReceptionistId);
    //     if (isPhoneUsed)
    //     {
    //         throw new InvalidOperationException("Số điện thoại này đã được sử dụng.");
    //     }
    //
    //     var isEmailUsed = await _context.Receptionists.AnyAsync(receptionist =>
    //         receptionist.Email == normalizedEmail && receptionist.Id != currentReceptionistId);
    //     if (isEmailUsed)
    //     {
    //         throw new InvalidOperationException("Email này đã được sử dụng.");
    //     }
    // }
    private async Task EnsureContactInformationIsAvailableAsync(
        string phone,
        Guid? currentReceptionistId = null)
    {
        var normalizedPhone = phone.Trim();

        var isPhoneUsed = await _context.Receptionists.AnyAsync(receptionist =>
            receptionist.Phone == normalizedPhone &&
            receptionist.Id != currentReceptionistId);

        if (isPhoneUsed)
        {
            throw new InvalidOperationException(
                "Số điện thoại này đã được sử dụng.");
        }
    }

    private static Response.ReceptionistResponse MapToResponse(
        Repository.Entity.Receptionist receptionist)
    {
        return new Response.ReceptionistResponse
        {
            Id = receptionist.Id,
            AccountId = receptionist.AccountId,
            FullName = receptionist.FullName,
            Phone = receptionist.Phone,
            Email = receptionist.Account.Email,
            WorkingShift = receptionist.WorkingShift
        };
    }
}
