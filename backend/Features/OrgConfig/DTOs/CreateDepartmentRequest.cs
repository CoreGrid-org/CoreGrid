namespace CoreGrid.Api.Features.OrgConfig.DTOs;

public class CreateDepartmentRequest
{
    public required string Code { get; set; }

    public required string Name { get; set; }
}
