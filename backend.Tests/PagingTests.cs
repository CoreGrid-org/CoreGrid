using CoreGrid.Api.Data;
using CoreGrid.Api.Domain;
using CoreGrid.Api.Features.Shared.Paging;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.Features.Shared;

// Phase 6 (§8): pins QueryableExtensions.ToPagedResultAsync's clamping
// (NFR-07, §4.3) — the one implementation every list endpoint's paging
// now goes through — rather than trusting each controller's own query
// parameters to already be well-formed.
public class PagingTests
{
    private static async Task<CoreGridDbContext> SeedAsync(int rowCount)
    {
        var options = new DbContextOptionsBuilder<CoreGridDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var db = new CoreGridDbContext(options, new NullCurrentOrganizationProvider());

        for (var i = 0; i < rowCount; i++)
        {
            db.Organizations.Add(new Organization { Id = Guid.NewGuid(), Name = $"Org {i:D3}" });
        }
        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task TotalCount_ReflectsTheFullRowCount_RegardlessOfPageSize()
    {
        await using var db = await SeedAsync(45);

        var result = await db.Organizations.ToPagedResultAsync(new PagedQuery { Page = 1, PageSize = 10 }, o => o.Id, CancellationToken.None);

        Assert.Equal(45, result.TotalCount);
        Assert.Equal(10, result.Items.Count);
        Assert.Equal(5, result.TotalPages);
    }

    [Fact]
    public async Task Page_BelowOne_ClampsToOne()
    {
        await using var db = await SeedAsync(5);

        var result = await db.Organizations.ToPagedResultAsync(new PagedQuery { Page = 0, PageSize = 10 }, o => o.Id, CancellationToken.None);

        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task PageSize_BelowOne_ClampsToTheDefault()
    {
        await using var db = await SeedAsync(30);

        var result = await db.Organizations.ToPagedResultAsync(new PagedQuery { Page = 1, PageSize = 0 }, o => o.Id, CancellationToken.None);

        Assert.Equal(PagedQuery.DefaultPageSize, result.PageSize);
    }

    [Fact]
    public async Task PageSize_AboveMax_ClampsToMax()
    {
        await using var db = await SeedAsync(150);

        var result = await db.Organizations.ToPagedResultAsync(new PagedQuery { Page = 1, PageSize = 1000 }, o => o.Id, CancellationToken.None);

        Assert.Equal(PagedQuery.MaxPageSize, result.PageSize);
        Assert.Equal(PagedQuery.MaxPageSize, result.Items.Count);
    }

    [Fact]
    public async Task PageSize_AboveExportMax_ClampsToTheExportOverride()
    {
        await using var db = await SeedAsync(600);

        var result = await db.Organizations.ToPagedResultAsync(
            new PagedQuery { Page = 1, PageSize = 10000 }, o => o.Id, CancellationToken.None, maxPageSize: PagedQuery.MaxExportPageSize);

        Assert.Equal(PagedQuery.MaxExportPageSize, result.PageSize);
        Assert.Equal(PagedQuery.MaxExportPageSize, result.Items.Count);
    }

    [Fact]
    public async Task LastPage_ReturnsOnlyTheRemainingRows()
    {
        await using var db = await SeedAsync(25);

        var result = await db.Organizations.ToPagedResultAsync(new PagedQuery { Page = 3, PageSize = 10 }, o => o.Id, CancellationToken.None);

        Assert.Equal(5, result.Items.Count);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task PageBeyondTheLastPage_ReturnsNoRowsButStillReportsTheRealTotal()
    {
        await using var db = await SeedAsync(10);

        var result = await db.Organizations.ToPagedResultAsync(new PagedQuery { Page = 99, PageSize = 10 }, o => o.Id, CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(10, result.TotalCount);
    }
}
