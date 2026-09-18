using System.ComponentModel.DataAnnotations;

namespace CoreGrid.Api.Features.OrgConfig.DTOs;

public class UpdateLocationRequest
{
    public required string Name { get; set; }

    public required string Type { get; set; }

    [Required]
    public Guid? DepartmentId { get; set; }
}
