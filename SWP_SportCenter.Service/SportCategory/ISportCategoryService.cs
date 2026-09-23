using static SWP_SportCenter.Service.SportCategory.Request;
using static SWP_SportCenter.Service.SportCategory.Response;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SWP_SportCenter.Service.SportCategory;

public interface ISportCategoryService
{
    Task<IEnumerable<SportCategoryResponse>> GetAllAsync();
    Task<SportCategoryResponse?> GetByIdAsync(Guid id);
    Task<SportCategoryResponse> CreateAsync(CreateSportCategoryRequest request);
    Task<bool> UpdateAsync(Guid id, UpdateSportCategoryRequest request);
    Task<bool> DeleteAsync(Guid id);
}