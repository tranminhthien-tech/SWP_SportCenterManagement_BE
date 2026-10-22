using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Response.RevenueReportResponse> GetRevenueReportAsync(
        DateTimeOffset from,
        DateTimeOffset to)
    {
        var invoices = await _db.Invoices
            .AsNoTracking()
            .Where(i =>
                i.Status == InvoiceStatus.Paid &&
                i.PaymentDate >= from &&
                i.PaymentDate <= to)
            .ToListAsync();

        var monthly = invoices
            .GroupBy(i => new { i.PaymentDate.Year, i.PaymentDate.Month })
            .Select(g => new Response.MonthlyRevenueItem
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Revenue = g.Sum(i => i.Amount),
                InvoiceCount = g.Count()
            })
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .ToList();

        return new Response.RevenueReportResponse
        {
            From = from,
            To = to,
            TotalRevenue = invoices.Sum(i => i.Amount),
            PaidInvoiceCount = invoices.Count,
            MonthlyRevenue = monthly
        };
    }

    public async Task<Response.MemberSummaryResponse> GetMemberSummaryAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);

        var totalMembers = await _db.Members
            .AsNoTracking()
            .CountAsync(m => !m.Account.IsDeleted);

        var newMembers = await _db.Members
            .AsNoTracking()
            .CountAsync(m =>
                !m.Account.IsDeleted &&
                m.CreatedAt >= thirtyDaysAgo);

        // CODE CŨ: đếm Member có Membership còn hạn. Số này không khớp với trang Accounts.
        // var activeMembers = await _db.Memberships
        //     .AsNoTracking()
        //     .Where(ms =>
        //         ms.Status == MembershipStatus.Active &&
        //         ms.EndDate >= now.Date)
        //     .Select(ms => ms.MemberId)
        //     .Distinct()
        //     .CountAsync();

        // FIX 2026-10-22: đếm đúng Account Member đang Active để Dashboard đồng bộ trang Accounts.
        var activeMembers = await _db.Accounts
            .AsNoTracking()
            .CountAsync(account =>
                account.Role == AccountRole.Member &&
                account.Status == AccountStatus.Active &&
                !account.IsDeleted);

        return new Response.MemberSummaryResponse
        {
            TotalMembers = totalMembers,
            NewMembersLast30Days = newMembers,
            ActiveMembers = activeMembers
        };
    }

    public async Task<Response.MembershipSummaryResponse> GetMembershipSummaryAsync()
    {
        var now = DateTime.UtcNow.Date;
        var sevenDaysLater = now.AddDays(7);

        var active = await _db.Memberships.AsNoTracking()
            .CountAsync(ms => ms.Status == MembershipStatus.Active && ms.EndDate >= now);

        var expiringSoon = await _db.Memberships.AsNoTracking()
            .CountAsync(ms =>
                ms.Status == MembershipStatus.Active &&
                ms.EndDate >= now &&
                ms.EndDate <= sevenDaysLater);

        var expired = await _db.Memberships.AsNoTracking()
            .CountAsync(ms => ms.Status == MembershipStatus.Expired);

        var inactive = await _db.Memberships.AsNoTracking()
            .CountAsync(ms => ms.Status == MembershipStatus.Inactive);

        return new Response.MembershipSummaryResponse
        {
            ActiveMemberships = active,
            ExpiringSoon = expiringSoon,
            ExpiredMemberships = expired,
            InactiveMemberships = inactive
        };
    }

    public async Task<Response.ClassSummaryResponse> GetClassSummaryAsync()
    {
        var totalClasses = await _db.Classes.AsNoTracking().CountAsync();
        var totalSessions = await _db.ClassSessions.AsNoTracking().CountAsync();
        var totalBookings = await _db.ClassBookings.AsNoTracking().CountAsync();

        return new Response.ClassSummaryResponse
        {
            TotalClasses = totalClasses,
            TotalSessions = totalSessions,
            TotalBookings = totalBookings
        };
    }
}
