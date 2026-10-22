using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.Membership;

public class Request
{
    // FIX B20: chỉ sửa kỳ hạn/trạng thái, không đổi Member/Package làm lệch hóa đơn cũ.
    public class UpdateMembershipRequest : System.ComponentModel.DataAnnotations.IValidatableObject
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        [System.ComponentModel.DataAnnotations.EnumDataType(typeof(MembershipStatus))]
        public MembershipStatus Status { get; set; }
        public IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> Validate(
            System.ComponentModel.DataAnnotations.ValidationContext context)
        {
            if (StartDate == default || EndDate == default || StartDate.Date > EndDate.Date)
                yield return new("Ngày bắt đầu và kết thúc bắt buộc, ngày kết thúc không trước ngày bắt đầu.", new[] { nameof(StartDate), nameof(EndDate) });
        }
    }
    public class MembershipRequest
    {
        public Guid MemberId { get; set; }

        public Guid PackageId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public MembershipStatus Status { get; set; }
    }
}
