using CoreGrid.Api.Data;
using CoreGrid.Api.Features.Audit;
using CoreGrid.Api.Features.Shared.Exceptions;
using CoreGrid.Api.Features.Verification.DTOs;
using CoreGrid.Api.Features.Verification.Services;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.Features.Audit;

// An inverted from/to range on the audit report or audit log is a 400, not
// a silently empty result that looks like "nothing happened in this period".
public class AuditDateRangeValidationTests
{
    private static CoreGridDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CoreGridDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CoreGridDbContext(options, new NullCurrentOrganizationProvider());
    }

    [Fact]
    public async Task AuditReport_FromAfterTo_ThrowsValidationException()
    {
        await using var db = CreateInMemoryDbContext();
        var service = new AuditReportService(db);

        var filter = new AuditReportFilter { From = new DateOnly(2026, 9, 10), To = new DateOnly(2026, 9, 1) };

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.GetReportAsync(Guid.NewGuid(), filter, CancellationToken.None));
    }

    [Fact]
    public async Task AuditReport_SameDayRange_IsAllowed()
    {
        await using var db = CreateInMemoryDbContext();
        var service = new AuditReportService(db);

        var day = new DateOnly(2026, 9, 10);
        var report = await service.GetReportAsync(Guid.NewGuid(), new AuditReportFilter { From = day, To = day }, CancellationToken.None);

        Assert.Equal(0, report.CampaignsInPeriod);
    }

    [Fact]
    public async Task AuditLog_FromAfterTo_ThrowsValidationException()
    {
        await using var db = CreateInMemoryDbContext();
        var service = new AuditLogService(db);

        var parameters = new AuditLogQueryParameters
        {
            From = new DateTimeOffset(2026, 9, 10, 0, 0, 0, TimeSpan.Zero),
            To = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
        };

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.GetEntriesAsync(Guid.NewGuid(), parameters, CancellationToken.None));
    }
}
