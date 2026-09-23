using System.ComponentModel.DataAnnotations;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.Room;

public class Request
{
    public class CreateRoomRequest
    {
        [Required(ErrorMessage = "Tên phòng không được để trống")]
        [MaxLength(100)]
        public string RoomName { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Sức chứa phải lớn hơn 0")]
        public int Capacity { get; set; }

        [Required(ErrorMessage = "Trạng thái phòng không được để trống")]
        public RoomStatus Status { get; set; }
    }

    public class UpdateRoomRequest
    {
        [Required(ErrorMessage = "Tên phòng không được để trống")]
        [MaxLength(100)]
        public string RoomName { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Sức chứa phải lớn hơn 0")]
        public int Capacity { get; set; }

        [Required(ErrorMessage = "Trạng thái phòng không được để trống")]
        public RoomStatus Status { get; set; }
    }
}