using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SWP_SportCenter.Service.Attendance;

public interface IAttendanceService
{
    // Lấy danh sách điểm danh của 1 buổi học (dành cho HLV/Quản lý)
    Task<IEnumerable<Response.AttendanceResponse>> GetBySessionIdAsync(Guid sessionId);
    
    // Lấy lịch sử điểm danh của 1 học viên (dành cho Member)
    Task<IEnumerable<Response.AttendanceResponse>> GetByMemberIdAsync(Guid memberId);
    
    // Thực hiện điểm danh
    Task<Response.AttendanceResponse> RecordAttendanceAsync(Request.RecordAttendanceRequest request);
    
    // Cập nhật trạng thái điểm danh (nếu điểm danh nhầm)
    Task<bool> UpdateAttendanceAsync(Guid id, Request.UpdateAttendanceRequest request);
}