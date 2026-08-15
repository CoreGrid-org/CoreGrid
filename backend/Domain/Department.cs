using System;
using System.Collections.Generic;

namespace CoreGrid.Api.Domain;

public class Department
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Location> Locations { get; set; } = new List<Location>();
    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
}
