using System;
using System.ComponentModel.DataAnnotations;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.Classes;

public class Request
{
    public class CreateClassRequest
    {
        [Required(ErrorMessage = "Tên lớp học không được để trống")]
        [MaxLength(150)]
        public string ClassName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn môn học")]
        public Guid CategoryId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn huấn luyện viên")]
        public Guid CoachId { get; set; }

        [Range(1, 1000, ErrorMessage = "Sức chứa tối đa phải lớn hơn 0")]
        public int MaxCapacity { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu không được để trống")]
        public DateTime StartDate { get; set; } // Trả về DateTime

        [Required(ErrorMessage = "Ngày kết thúc không được để trống")]
        public DateTime EndDate { get; set; }   // Trả về DateTime

        public ClassStatus Status { get; set; } = ClassStatus.Scheduled;
    }

    public class UpdateClassRequest
    {
        [Required(ErrorMessage = "Tên lớp học không được để trống")]
        [MaxLength(150)]
        public string ClassName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn môn học")]
        public Guid CategoryId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn huấn luyện viên")]
        public Guid CoachId { get; set; }

        [Range(1, 1000, ErrorMessage = "Sức chứa tối đa phải lớn hơn 0")]
        public int MaxCapacity { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu không được để trống")]
        public DateTime StartDate { get; set; } // Trả về DateTime

        [Required(ErrorMessage = "Ngày kết thúc không được để trống")]
        public DateTime EndDate { get; set; }   // Trả về DateTime

        [Required(ErrorMessage = "Trạng thái lớp không được để trống")]
        public ClassStatus Status { get; set; }
    }
}