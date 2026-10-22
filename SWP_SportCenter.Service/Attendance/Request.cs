using System;
using System.ComponentModel.DataAnnotations;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.Attendance;

public class Request
{
    public class RecordAttendanceRequest
    {
        [Required(ErrorMessage = "Vui lòng chọn Buổi học (Session)")]
        public Guid SessionId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Học viên")]
        public Guid MemberId { get; set; }

        [Required(ErrorMessage = "Trạng thái điểm danh không được để trống")]
        // FIX B15: không nhận giá trị enum không tồn tại.
        [EnumDataType(typeof(AttendanceStatus))]
        public AttendanceStatus Status { get; set; }
        
        // CODE CŨ: [Required] RecordedBy từ client.
        // FIX B15: giữ field tương thích nhưng server bỏ qua, lấy từ token.
        public Guid RecordedBy { get; set; }
    }

    public class UpdateAttendanceRequest
    {
        [Required(ErrorMessage = "Trạng thái điểm danh không được để trống")]
        // FIX B15: giới hạn enum cho cập nhật.
        [EnumDataType(typeof(AttendanceStatus))]
        public AttendanceStatus Status { get; set; }
        
        // CODE CŨ: [Required] RecordedBy từ client.
        // FIX B15: server tự lấy tài khoản thực hiện cập nhật.
        public Guid RecordedBy { get; set; }
    }
}
