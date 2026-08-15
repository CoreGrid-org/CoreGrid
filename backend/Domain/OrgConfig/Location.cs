using System;
using System.Collections.Generic;

namespace CoreGrid.Api.Domain;

public class Location
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public Guid DepartmentId { get; set; }
    public Department? Department { get; set; }

    public required string Name { get; set; }
    public required string Type { get; set; } // e.g. store, workshop, office, ward
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
}
