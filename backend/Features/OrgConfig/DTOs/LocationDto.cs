namespace CoreGrid.Api.Features.OrgConfig.DTOs;

public class LocationDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public Guid DepartmentId { get; set; }

    public string DepartmentName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
