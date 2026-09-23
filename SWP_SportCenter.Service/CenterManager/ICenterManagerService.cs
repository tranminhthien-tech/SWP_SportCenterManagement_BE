using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SWP_SportCenter.Service.CenterManager;

public interface ICenterManagerService
{
    Task<IEnumerable<Response.CenterManagerResponse>> GetAllAsync();
    Task<Response.CenterManagerResponse?> GetByIdAsync(Guid id);
    Task<Response.CenterManagerResponse?> GetByAccountIdAsync(Guid accountId); // Hỗ trợ lấy profile qua token
    
    Task<Response.CenterManagerResponse> CreateAsync(Request.CreateCenterManagerRequest request);
    Task<bool> UpdateAsync(Guid id, Request.UpdateCenterManagerRequest request);
    Task<bool> DeleteAsync(Guid id);
}