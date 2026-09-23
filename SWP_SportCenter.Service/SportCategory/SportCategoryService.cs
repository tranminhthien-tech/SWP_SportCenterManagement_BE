using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using static SWP_SportCenter.Service.SportCategory.Request;
using static SWP_SportCenter.Service.SportCategory.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SWP_SportCenter.Service.SportCategory;

public class SportCategoryService : ISportCategoryService
{
    private readonly AppDbContext _context;

    public SportCategoryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SportCategoryResponse>> GetAllAsync()
    {
        return await _context.SportCategories
            .AsNoTracking()
            .Select(c => new SportCategoryResponse
            {
                Id = c.Id,
                CategoryName = c.CategoryName,
                Description = c.Description,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<SportCategoryResponse?> GetByIdAsync(Guid id)
    {
        var category = await _context.SportCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null) return null;

        return new SportCategoryResponse
        {
            Id = category.Id,
            CategoryName = category.CategoryName,
            Description = category.Description,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }

    public async Task<SportCategoryResponse> CreateAsync(CreateSportCategoryRequest request)
    {
        // Kiểm tra xem tên bộ môn đã tồn tại chưa (vì DB có cấu hình Unique)
        var isExist = await _context.SportCategories.AnyAsync(c => c.CategoryName.ToLower() == request.CategoryName.ToLower());
        if (isExist)
        {
            throw new Exception("Tên bộ môn này đã tồn tại trong hệ thống.");
        }

        var newCategory = new Repository.Entity.SportCategory
        {
            CategoryName = request.CategoryName,
            Description = request.Description
        };

        _context.SportCategories.Add(newCategory);
        await _context.SaveChangesAsync();

        return new SportCategoryResponse
        {
            Id = newCategory.Id,
            CategoryName = newCategory.CategoryName,
            Description = newCategory.Description,
            CreatedAt = newCategory.CreatedAt,
            UpdatedAt = newCategory.UpdatedAt
        };
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateSportCategoryRequest request)
    {
        var category = await _context.SportCategories.FindAsync(id);
        if (category == null) return false;

        // Kiểm tra trùng tên nếu người dùng đổi sang tên khác
        if (category.CategoryName != request.CategoryName)
        {
            var isExist = await _context.SportCategories.AnyAsync(c => c.CategoryName.ToLower() == request.CategoryName.ToLower());
            if (isExist) throw new Exception("Tên bộ môn này đã tồn tại trong hệ thống.");
        }

        category.CategoryName = request.CategoryName;
        category.Description = request.Description;
        category.UpdatedAt = DateTimeOffset.UtcNow; // Cập nhật thời gian sửa

        _context.SportCategories.Update(category);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var category = await _context.SportCategories.FindAsync(id);
        if (category == null) return false;

        // Kiểm tra xem bộ môn này đã có Lớp học nào chưa (tránh lỗi Restrict)
        var hasClasses = await _context.Classes.AnyAsync(c => c.CategoryId == id);
        if (hasClasses)
        {
            throw new Exception("Không thể xóa bộ môn này vì đang có lớp học thuộc bộ môn này.");
        }

        _context.SportCategories.Remove(category);
        await _context.SaveChangesAsync();

        return true;
    }
}