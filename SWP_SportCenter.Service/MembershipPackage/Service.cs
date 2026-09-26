using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;

namespace SWP_SportCenter.Service.MembershipPackage;

public class Service: IService
{
    private readonly AppDbContext _dbContext;
    public Service(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
     // GET: api/membership-packages
    // Lấy danh sách gói tập có phân trang và tìm kiếm
    public async Task<Base.Response.PageResult<Response.MembershipPackageResponse>>
        GetAllAsync(string? searchTerm, int pageSize, int pageIndex)
    {
        var query = _dbContext.MembershipPackages
            .Where(x => x.IsDeleted == false);

        // Tìm kiếm theo tên gói hoặc mô tả
        if (searchTerm != null)
        {
            query = query.Where(x =>
                x.PackageName.Contains(searchTerm) ||
                x.Description.Contains(searchTerm));
        }

        // Đếm tổng số gói trước khi phân trang
        var totalItems = await query.CountAsync();

        // Sắp xếp và phân trang
        query = query
            .OrderBy(x => x.PackageName)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize);

        // Chuyển Entity sang Response
        var selected = query.Select(x =>
            new Response.MembershipPackageResponse
            {
                MembershipPackageId = x.Id,
                PackageName = x.PackageName,
                Description = x.Description,
                DurationDays = x.DurationDays,
                Price = x.Price,
                Status = x.Status,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            });

        var listResult = await selected.ToListAsync();

        var result =
            new Base.Response.PageResult<Response.MembershipPackageResponse>
            {
                Items = listResult,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalItems = totalItems
            };

        return result;
    }

    // GET: api/membership-packages/{id}
    // Lấy chi tiết gói tập
    public async Task<Response.MembershipPackageResponse?> GetByIdAsync(
        Guid id)
    {
        var selected = _dbContext.MembershipPackages
            .Where(x => x.Id == id && x.IsDeleted == false)
            .Select(x => new Response.MembershipPackageResponse
            {
                MembershipPackageId = x.Id,
                PackageName = x.PackageName,
                Description = x.Description,
                DurationDays = x.DurationDays,
                Price = x.Price,
                Status = x.Status,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            });

        var result = await selected.FirstOrDefaultAsync();

        return result;
    }

    // POST: api/membership-packages
    // Tạo gói tập
    public async Task<Response.MembershipPackageResponse> CreateAsync(
        Request.MembershipPackageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PackageName))
        {
            throw new ArgumentException("Package name is required.");
        }

        if (request.DurationDays <= 0)
        {
            throw new ArgumentException(
                "Duration must be greater than 0.");
        }

        if (request.Price < 0)
        {
            throw new ArgumentException(
                "Price cannot be negative.");
        }

        var package = new Repository.Entity.MembershipPackage
        {
            Id = Guid.NewGuid(),
            PackageName = request.PackageName.Trim(),
            Description = request.Description,
            DurationDays = request.DurationDays,
            Price = request.Price,
            Status = request.Status,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.MembershipPackages.Add(package);
        await _dbContext.SaveChangesAsync();

        var result = new Response.MembershipPackageResponse
        {
            MembershipPackageId = package.Id,
            PackageName = package.PackageName,
            Description = package.Description,
            DurationDays = package.DurationDays,
            Price = package.Price,
            Status = package.Status,
            CreatedAt = package.CreatedAt,
            UpdatedAt = package.UpdatedAt
        };

        return result;
    }

    // PUT: api/membership-packages/{id}
    // Cập nhật gói tập
    public async Task<bool> UpdateAsync(
        Guid id,
        Request.MembershipPackageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PackageName))
        {
            throw new ArgumentException("Package name is required.");
        }

        if (request.DurationDays <= 0)
        {
            throw new ArgumentException(
                "Duration must be greater than 0.");
        }

        if (request.Price < 0)
        {
            throw new ArgumentException(
                "Price cannot be negative.");
        }

        var package = await _dbContext.MembershipPackages
            .FirstOrDefaultAsync(x =>
                x.Id == id && x.IsDeleted == false);

        if (package == null)
        {
            return false;
        }

        package.PackageName = request.PackageName.Trim();
        package.Description = request.Description;
        package.DurationDays = request.DurationDays;
        package.Price = request.Price;
        package.Status = request.Status;
        package.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return true;
    }

    // DELETE: api/membership-packages/{id}
    // Xóa mềm gói tập
    public async Task<bool> DeleteAsync(Guid id)
    {
        var package = await _dbContext.MembershipPackages
            .FirstOrDefaultAsync(x =>
                x.Id == id && x.IsDeleted == false);

        if (package == null)
        {
            return false;
        }

        package.IsDeleted = true;
        package.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return true;
    }
}