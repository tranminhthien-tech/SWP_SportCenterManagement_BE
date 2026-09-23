using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
                Email = cm.Email
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
        var isAccountExist = await _context.Accounts.AnyAsync(a => a.Id == request.AccountId);
        if (!isAccountExist) throw new Exception("Tài khoản (Account) không tồn tại.");

        var isProfileExist = await _context.CenterManagers.AnyAsync(cm => cm.AccountId == request.AccountId);
        if (isProfileExist) throw new Exception("Tài khoản này đã có hồ sơ Quản lý trung tâm.");

        var isPhoneExist = await _context.CenterManagers.AnyAsync(cm => cm.Phone == request.Phone);
        if (isPhoneExist) throw new Exception("Số điện thoại này đã được sử dụng.");

        var isEmailExist = await _context.CenterManagers.AnyAsync(cm => cm.Email == request.Email);
        if (isEmailExist) throw new Exception("Email này đã được sử dụng.");

        var newManager = new Repository.Entity.CenterManager
        {
            AccountId = request.AccountId,
            FullName = request.FullName,
            Phone = request.Phone,
            Email = request.Email
        };

        _context.CenterManagers.Add(newManager);
        await _context.SaveChangesAsync();

        return MapToResponse(newManager);
    }

    public async Task<bool> UpdateAsync(Guid id, Request.UpdateCenterManagerRequest request)
    {
        var manager = await _context.CenterManagers.FindAsync(id);
        if (manager == null) return false;

        // Kiểm tra trùng lặp thông tin với người khác
        if (manager.Phone != request.Phone && await _context.CenterManagers.AnyAsync(cm => cm.Phone == request.Phone))
            throw new Exception("Số điện thoại này đã được sử dụng bởi người khác.");

        if (manager.Email != request.Email && await _context.CenterManagers.AnyAsync(cm => cm.Email == request.Email))
            throw new Exception("Email này đã được sử dụng bởi người khác.");

        manager.FullName = request.FullName;
        manager.Phone = request.Phone;
        manager.Email = request.Email;

        _context.CenterManagers.Update(manager);
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
            Email = manager.Email
        };
    }
}