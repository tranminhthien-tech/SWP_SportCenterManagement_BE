using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using SWP_SportCenter.Repository.Entity;
namespace SWP_SportCenter.Service.Payment;
// FIX B53 B55: không giả định mọi hóa đơn đều có Membership.
public static class PaymentQueries
{
    public static IQueryable<Repository.Entity.Invoice> Graph(AppDbContext db) => db.Invoices
        .Include(i => i.Membership!).ThenInclude(m => m.Package)
        .Include(i => i.Membership!).ThenInclude(m => m.Member).ThenInclude(m => m.Account)
        .Include(i => i.UpgradeFromMembership)
        .Include(i => i.ClassBooking!).ThenInclude(b => b.Class).ThenInclude(c => c.ClassSessions)
        .Include(i => i.ClassBooking!).ThenInclude(b => b.Member).ThenInclude(m => m.Account)
        .Include(i => i.ClassBooking!).ThenInclude(b => b.Membership);
    public static Guid MemberId(Repository.Entity.Invoice invoice) => invoice.Membership?.MemberId ?? invoice.ClassBooking?.MemberId
        ?? throw new InvalidOperationException("Hóa đơn thiếu chủ sở hữu.");
}
