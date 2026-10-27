using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWP_SportCenter.Repository.Enum;
using SWP_SportCenter.Service.Exceptions;

namespace SWP_SportCenter.Service.CenterManager;

public class CenterManagerService : ICenterManagerService
{
    private readonly AppDbContext _context;

    public CenterManagerService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Response.CenterManagerResponse>> GetAllAsync()
    {
        return await _context.CenterManagers
            .AsNoTracking()
            .Where(cm => !cm.IsDeleted && !cm.Account.IsDeleted && cm.Account.Role == AccountRole.CenterManager) // FIX B50
            .Select(cm => new Response.CenterManagerResponse
            {
                Id = cm.Id,
                AccountId = cm.AccountId,
                FullName = cm.FullName,
                Avatar = cm.Avatar, // FIX B22
                Phone = cm.Phone,
                Email = cm.Account.Email
            })
            .ToListAsync();
    }

    public async Task<Response.CenterManagerResponse?> GetByIdAsync(Guid id)
    {
        // CODE CŨ: không Include Account trước mapping Email.
        // CODE CŨ: FirstOrDefaultAsync(cm => cm.Id == id)
        var manager = await _context.CenterManagers.AsNoTracking().Include(m => m.Account)
            .FirstOrDefaultAsync(cm => cm.Id == id && !cm.IsDeleted && cm.Account.Role == AccountRole.CenterManager); // FIX B50
        if (manager == null) return null;

        return MapToResponse(manager);
    }

    public async Task<Response.CenterManagerResponse?> GetByAccountIdAsync(Guid accountId)
    {
        // CODE CŨ: không Include Account.
        // CODE CŨ: FirstOrDefaultAsync(cm => cm.AccountId == accountId)
        var manager = await _context.CenterManagers.AsNoTracking().Include(m => m.Account)
            .FirstOrDefaultAsync(cm => cm.AccountId == accountId && !cm.IsDeleted && cm.Account.Role == AccountRole.CenterManager); // FIX B50
        if (manager == null) return null;

        return MapToResponse(manager);
    }

    public async Task<Response.CenterManagerResponse> CreateAsync(Request.CreateCenterManagerRequest request)
    {
        // Kiểm tra ràng buộc
        var account = await _context.Accounts
            // CODE CŨ: .AsNoTracking().FirstOrDefaultAsync(a => a.Id == request.AccountId);
            // FIX: track Account để lưu email và gắn vào profile; chỉ dùng Account Active chưa xóa.
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && !a.IsDeleted && a.Status == AccountStatus.Active);

        if (account == null)
            // CODE CŨ: throw new Exception(...);
            // FIX: lỗi nghiệp vụ trả 400 JSON.
            throw new BusinessException(
                "Tài khoản (Account) không tồn tại.");

        if (account.Role != AccountRole.CenterManager)
            // CODE CŨ: throw new Exception(...);
            // FIX: lỗi sai role trả 400 JSON.
            throw new BusinessException(
                "Tài khoản này không có vai trò Quản lý trung tâm (Center Manager).");

        var isProfileExist = await _context.CenterManagers.AnyAsync(cm => cm.AccountId == request.AccountId);
        // CODE CŨ: if (isProfileExist) throw new Exception("Tài khoản này đã có hồ sơ Quản lý trung tâm.");
        // FIX: phân biệt lỗi tạo trùng hồ sơ.
        if (isProfileExist) throw new BusinessException("Tài khoản này đã có hồ sơ Quản lý trung tâm.");

        var isPhoneExist = await _context.CenterManagers.AnyAsync(cm => cm.Phone == request.Phone);
        // CODE CŨ: if (isPhoneExist) throw new Exception("Số điện thoại này đã được sử dụng.");
        // FIX: lỗi trùng phone trả 400 JSON.
        if (isPhoneExist) throw new BusinessException("Số điện thoại này đã được sử dụng.");

        // CODE CŨ: kiểm tra email qua CenterManagers và throw new Exception.
        // FIX: email unique trên toàn bảng Account, loại trừ chính tài khoản được chọn.
        var isEmailExist = await _context.Accounts.AnyAsync(a => a.Email == request.Email && a.Id != request.AccountId);
        if (isEmailExist) throw new BusinessException("Email này đã được sử dụng.");
        account.Email = request.Email;

        var newManager = new Repository.Entity.CenterManager
        {
            AccountId = request.AccountId,
            FullName = request.FullName,
            Avatar = request.Avatar, // FIX B22
            Phone = request.Phone,
            // FIX: gắn Account để response lấy được Email ngay sau khi lưu.
            Account = account,
        };

        _context.CenterManagers.Add(newManager);
        // CODE CŨ: await _context.SaveChangesAsync();
        // FIX: xử lý lỗi unique constraint thành lỗi nghiệp vụ.
        await _context.SaveProfileChangesAsync();

        return MapToResponse(newManager);
    }

    public async Task<bool> UpdateAsync(Guid id, Request.UpdateCenterManagerRequest request)
    {
        // Lấy CenterManager kèm Account để cập nhật Email ở bảng Account
        var manager = await _context.CenterManagers
            .Include(cm => cm.Account)
            .FirstOrDefaultAsync(cm => cm.Id == id);

        if (manager == null)
            return false;

        // Kiểm tra số điện thoại trùng với CenterManager khác
        if (manager.Phone != request.Phone &&
            await _context.CenterManagers.AnyAsync(cm =>
                cm.Phone == request.Phone &&
                cm.Id != id))
        {
            // CODE CŨ: throw new Exception("Số điện thoại này đã được sử dụng bởi người khác.");
            // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
            throw new SWP_SportCenter.Service.Exceptions.BusinessException("Số điện thoại này đã được sử dụng bởi người khác.");
        }

        // Kiểm tra Email trùng với Account khác
        if (manager.Account.Email != request.Email &&
            await _context.Accounts.AnyAsync(a =>
                a.Email == request.Email &&
                a.Id != manager.AccountId))
        {
            // CODE CŨ: throw new Exception("Email này đã được sử dụng bởi người khác.");
            // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
            throw new SWP_SportCenter.Service.Exceptions.BusinessException("Email này đã được sử dụng bởi người khác.");
        }

        // Cập nhật thông tin CenterManager
        manager.FullName = request.FullName;
        manager.Phone = request.Phone;
        manager.Avatar = request.Avatar; // FIX B22

        // Email nằm ở Account, KHÔNG còn nằm ở CenterManager
        manager.Account.Email = request.Email;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var manager = await _context.CenterManagers.FindAsync(id);
        if (manager == null) return false;

        _context.CenterManagers.Remove(manager);
        await _context.SaveChangesAsync();
        
        return true;
    }

    private Response.CenterManagerResponse MapToResponse(Repository.Entity.CenterManager manager)
    {
        return new Response.CenterManagerResponse
        {
            Id = manager.Id,
            AccountId = manager.AccountId,
            FullName = manager.FullName,
            Avatar = manager.Avatar, // FIX B22
            Phone = manager.Phone,
            Email = manager.Account.Email
        };
    }
}
