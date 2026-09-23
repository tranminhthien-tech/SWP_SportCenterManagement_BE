using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SWP_SportCenter.Service.Coach;

public interface ICoachService
{
    Task<IEnumerable<Response.CoachResponse>> GetAllAsync();
    Task<Response.CoachResponse?> GetByIdAsync(Guid id);
    Task<Response.CoachResponse?> GetByAccountIdAsync(Guid accountId); // Phục vụ API /me
    
    Task<Response.CoachResponse> CreateAsync(Request.CreateCoachRequest request);
    Task<bool> UpdateAsync(Guid id, Request.UpdateCoachRequest request);
    Task<bool> DeleteAsync(Guid id);

    // Các nghiệp vụ đặc thù cho Flow 2
    Task<IEnumerable<Response.CoachClassResponse>> GetCoachClassesAsync(Guid coachId);
    Task<IEnumerable<Response.CoachMemberResponse>> GetCoachMembersAsync(Guid coachId);
}