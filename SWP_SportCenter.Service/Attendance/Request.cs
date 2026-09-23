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
        public AttendanceStatus Status { get; set; }
        
        [Required(ErrorMessage = "Vui lòng cung cấp ID người điểm danh")]
        public Guid RecordedBy { get; set; }
    }

    public class UpdateAttendanceRequest
    {
        [Required(ErrorMessage = "Trạng thái điểm danh không được để trống")]
        public AttendanceStatus Status { get; set; }
        
        [Required(ErrorMessage = "Vui lòng cung cấp ID người cập nhật")]
        public Guid RecordedBy { get; set; }
    }
}