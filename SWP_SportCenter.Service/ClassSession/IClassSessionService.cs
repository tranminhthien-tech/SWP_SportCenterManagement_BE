using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SWP_SportCenter.Service.ClassSession;

public interface IClassSessionService
{
    Task<IEnumerable<Response.ClassSessionResponse>> GetAllAsync();
    Task<IEnumerable<Response.ClassSessionResponse>> GetByClassIdAsync(Guid classId); // Hàm hỗ trợ lấy lịch của 1 lớp cụ thể
    Task<Response.ClassSessionResponse?> GetByIdAsync(Guid id);
    Task<Response.ClassSessionResponse> CreateAsync(Request.CreateClassSessionRequest request);
    Task<bool> UpdateAsync(Guid id, Request.UpdateClassSessionRequest request);
    Task<bool> DeleteAsync(Guid id);
}