namespace SWP_SportCenter.Service.SportCategory;
using System.ComponentModel.DataAnnotations;
public class Request
{
    public class CreateSportCategoryRequest
    {
        [Required(ErrorMessage = "Tên bộ môn không được để trống")]
        [MaxLength(150)]
        public string CategoryName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }

    public class UpdateSportCategoryRequest
    {
        [Required(ErrorMessage = "Tên bộ môn không được để trống")]
        [MaxLength(150)]
        public string CategoryName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }
}