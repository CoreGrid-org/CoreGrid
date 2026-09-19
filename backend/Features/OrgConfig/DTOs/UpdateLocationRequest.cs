using System.ComponentModel.DataAnnotations;

namespace CoreGrid.Api.Features.OrgConfig.DTOs;

public class UpdateLocationRequest
{
    [Required, MaxLength(200)]
    public required string Name { get; set; }

    [Required, MaxLength(50)]
    public required string Type { get; set; }

    [Required]
    public Guid? DepartmentId { get; set; }
}
