using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWP_SportCenter.Repository.Enum;

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
            .Select(cm => new Response.CenterManagerResponse
            {
                Id = cm.Id,
                AccountId = cm.AccountId,
                FullName = cm.FullName,
                Phone = cm.Phone,
                Email = cm.Account.Email
            })
            .ToListAsync();
    }

    public async Task<Response.CenterManagerResponse?> GetByIdAsync(Guid id)
    {
        var manager = await _context.CenterManagers.AsNoTracking().FirstOrDefaultAsync(cm => cm.Id == id);
        if (manager == null) return null;

        return MapToResponse(manager);
    }

    public async Task<Response.CenterManagerResponse?> GetByAccountIdAsync(Guid accountId)
    {
        var manager = await _context.CenterManagers.AsNoTracking().FirstOrDefaultAsync(cm => cm.AccountId == accountId);
        if (manager == null) return null;

        return MapToResponse(manager);
    }

    public async Task<Response.CenterManagerResponse> CreateAsync(Request.CreateCenterManagerRequest request)
    {
        // Kiểm tra ràng buộc
        var account = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AccountId);

        if (account == null)
            throw new Exception(
                "Tài khoản (Account) không tồn tại.");

        if (account.Role != AccountRole.CenterManager)
            throw new Exception(
                "Tài khoản này không có vai trò Quản lý trung tâm (Center Manager).");

        var isProfileExist = await _context.CenterManagers.AnyAsync(cm => cm.AccountId == request.AccountId);
        if (isProfileExist) throw new Exception("Tài khoản này đã có hồ sơ Quản lý trung tâm.");

        var isPhoneExist = await _context.CenterManagers.AnyAsync(cm => cm.Phone == request.Phone);
        if (isPhoneExist) throw new Exception("Số điện thoại này đã được sử dụng.");

        var isEmailExist = await _context.CenterManagers.AnyAsync(cm => cm.Account.Email == request.Email);
        if (isEmailExist) throw new Exception("Email này đã được sử dụng.");

        var newManager = new Repository.Entity.CenterManager
        {
            AccountId = request.AccountId,
            FullName = request.FullName,
            Phone = request.Phone,
        };

        _context.CenterManagers.Add(newManager);
        await _context.SaveChangesAsync();

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
            throw new Exception("Số điện thoại này đã được sử dụng bởi người khác.");
        }

        // Kiểm tra Email trùng với Account khác
        if (manager.Account.Email != request.Email &&
            await _context.Accounts.AnyAsync(a =>
                a.Email == request.Email &&
                a.Id != manager.AccountId))
        {
            throw new Exception("Email này đã được sử dụng bởi người khác.");
        }

        // Cập nhật thông tin CenterManager
        manager.FullName = request.FullName;
        manager.Phone = request.Phone;

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
            Phone = manager.Phone,
            Email = manager.Account.Email
        };
    }
}