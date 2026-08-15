namespace CoreGrid.Api.Features.OrgConfig.DTOs;

public class CreateLocationRequest
{
    public required string Name { get; set; }

    public required string Type { get; set; }

    public Guid DepartmentId { get; set; }
}
