using System;
using System.ComponentModel.DataAnnotations;

namespace SWP_SportCenter.Service.ClassSession;

public class Request
{
    public class CreateClassSessionRequest
    {
        [Required(ErrorMessage = "Vui lòng chọn Lớp học")]
        public Guid ClassId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Phòng tập")]
        public Guid RoomId { get; set; }

        [Required(ErrorMessage = "Ngày học không được để trống")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Thời gian bắt đầu không được để trống")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "Thời gian kết thúc không được để trống")]
        public TimeSpan EndTime { get; set; }
    }

    public class UpdateClassSessionRequest
    {
        [Required(ErrorMessage = "Vui lòng chọn Lớp học")]
        public Guid ClassId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Phòng tập")]
        public Guid RoomId { get; set; }

        [Required(ErrorMessage = "Ngày học không được để trống")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Thời gian bắt đầu không được để trống")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "Thời gian kết thúc không được để trống")]
        public TimeSpan EndTime { get; set; }
    }
}