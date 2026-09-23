using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SWP_SportCenter.Service.Classes;

public interface IClassService
{
    Task<IEnumerable<Response.ClassResponse>> GetAllAsync();
    Task<Response.ClassResponse?> GetByIdAsync(Guid id);
    Task<Response.ClassResponse> CreateAsync(Request.CreateClassRequest request);
    Task<bool> UpdateAsync(Guid id, Request.UpdateClassRequest request);
    Task<bool> DeleteAsync(Guid id);
    
    // Flow 2: Xem danh sách học viên của lớp
    Task<IEnumerable<Response.ClassMemberResponse>> GetMembersByClassIdAsync(Guid classId);
}