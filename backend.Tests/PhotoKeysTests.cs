using CoreGrid.Api.Features.Shared.Exceptions;
using CoreGrid.Api.Features.Shared.Storage;

namespace backend.Tests.Features.Storage;

public class PhotoKeysTests
{
    private static readonly Guid Org = Guid.Parse("762c5395-8a26-4cf5-b092-8c92bfc73e0b");

    [Fact]
    public void AcceptsThisOrganisationsOwnUpload()
    {
        var key = $"{PhotoKeys.MaintenanceFolder(Org)}/0f1e2d3c4b5a69788796a5b4c3d2e1f0-photo.jpg";
        Assert.Equal(key, PhotoKeys.RequireOwnMaintenancePhoto(key, Org, "PhotoUrl"));
    }

    [Fact]
    public void NoPhotoStaysNull()
    {
        Assert.Null(PhotoKeys.RequireOwnMaintenancePhoto(null, Org, "PhotoUrl"));
        Assert.Null(PhotoKeys.RequireOwnMaintenancePhoto("  ", Org, "PhotoUrl"));
    }

    [Theory]
    [InlineData("maintenance/0000000000000000000000000000abcd/x-photo.jpg")] // another organisation
    [InlineData("maintenance/legacy-photo.jpg")]                                 // old, unscoped layout
    [InlineData("https://evil.example/photo.jpg")]                              // not a key at all
    public void RejectsKeysOutsideThisOrganisationsFolder(string key)
    {
        Assert.Throws<ValidationException>(() => PhotoKeys.RequireOwnMaintenancePhoto(key, Org, "PhotoUrl"));
    }

    [Fact]
    public void RejectsPathTraversal()
    {
        var key = $"{PhotoKeys.MaintenanceFolder(Org)}/../0000000000000000000000000000abcd/photo.jpg";
        Assert.Throws<ValidationException>(() => PhotoKeys.RequireOwnMaintenancePhoto(key, Org, "PhotoUrl"));
    }

    [Theory]
    [InlineData("image/png", "photo.png")]
    [InlineData("image/webp", "photo.webp")]
    [InlineData("image/jpeg", "photo.jpg")]
    public void FileNameComesFromTheContentTypeNotTheUpload(string contentType, string expected)
    {
        Assert.Equal(expected, PhotoKeys.FileNameFor(contentType));
    }
}
