using System.ComponentModel.DataAnnotations;
namespace SWP_SportCenter.Service.Validation;
// FIX B3 B17: ngày sinh thiếu hoặc ngoài khoảng hợp lệ là lỗi 400.
public sealed class DateOfBirthAttribute : ValidationAttribute
{
    public DateOfBirthAttribute() : base("Ngày sinh bắt buộc, từ năm 1900 đến ngày hiện tại.") { }
    public override bool IsValid(object? value) => value is DateTime date &&
        date.Date >= new DateTime(1900, 1, 1) && date.Date <= DateTime.UtcNow.Date;
}
